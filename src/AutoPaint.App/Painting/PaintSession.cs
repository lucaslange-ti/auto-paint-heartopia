using AutoPaint.App.Bridge;
using AutoPaint.App.Overlays;
using AutoPaint.Core.Bridge;
using AutoPaint.Core.Calibration;
using AutoPaint.Core.Config;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Painting;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AutoPaint.App.Painting;

/// <summary>
/// Coordena uma execução de pintura: guarda o plano atual, cria o motor, liga o
/// progresso aos eventos da ponte e trata as hotkeys de abortar/pausar.
/// </summary>
public sealed class PaintSession
{
    private readonly IServiceProvider _sp;
    private readonly IConfigStore _config;
    private readonly IHotkeyService _hotkeys;
    private readonly BridgeEventHub _events;
    private readonly PaintOverlayController _overlay;

    private IPaintEngine? _engine;
    private CancellationTokenSource? _cts;
    private Task? _run;
    private bool _paused;

    public PaintPlan? CurrentPlan { get; private set; }
    public ResumeState? LastResume { get; private set; }
    public bool IsRunning => _run is { IsCompleted: false };

    public PaintSession(IServiceProvider sp, IConfigStore config, IHotkeyService hotkeys, BridgeEventHub events,
        PaintOverlayController overlay)
    {
        _overlay = overlay;
        _sp = sp;
        _config = config;
        _hotkeys = hotkeys;
        _events = events;
        _hotkeys.HotkeyPressed += OnHotkey;
    }

    public void SetPlan(PaintPlan plan)
    {
        CurrentPlan = plan;
        LastResume = null;
    }

    public void Start(bool resume) => Launch(resume, repairOnly: false);

    /// <summary>
    /// Só reparo: sem passe de pintura, varre as células e repinta as que faltam
    /// ou divergem, pulando as já corretas. Precisa de plano convertido + calibração.
    /// </summary>
    public void StartRepair() => Launch(resume: false, repairOnly: true);

    private void Launch(bool resume, bool repairOnly)
    {
        if (IsRunning) throw new InvalidOperationException("Pintura já em andamento.");
        if (CurrentPlan is not { } plan) throw new InvalidOperationException("Nenhum plano convertido.");

        var calib = _config.Current.Calibration.ToDomain();
        if (!calib.IsCalibrated)
            throw new InvalidOperationException("Calibração incompleta (canvas ou paleta).");
        if (calib.Palette is not { ShadesPanelButton: not null, BackButton: not null, PaintToolButton: not null })
            throw new InvalidOperationException(
                "Botões do jogo não cadastrados (abrir painel de tons, voltar e ferramenta de pintura).");
        if (!calib.CoversGrid(plan.Size))
            throw new InvalidOperationException(
                "A calibração do canvas é de outra grade. Recalibre o canvas para a proporção/precisão atual.");

        var resumeState = !repairOnly && resume && LastResume is { } r
            && r.PlanHash == plan.Hash
            && r.CalibrationHash == CalibrationHash.Of(calib)
                ? LastResume
                : null;

        var options = BuildOptions();
        var request = new PaintRequest(plan, calib, options, resumeState) { RepairOnly = repairOnly };

        _cts = new CancellationTokenSource();
        _paused = false;
        _engine = _sp.GetRequiredService<IPaintEngine>();
        _hotkeys.Start(_config.Current.Hotkeys.Abort.ToBinding(), _config.Current.Hotkeys.PauseResume.ToBinding());

        var progress = new Progress<PaintProgress>(p =>
        {
            _events.Emit(BridgeEvent.Of(BridgeEvents.PaintProgress, p));
            _overlay.Report(p);
        });
        _overlay.EnterPainting(fresh: true);

        Log.Information("{Op} iniciada ({Cells} células, modo {Mode}, resume={Resume})",
            repairOnly ? "Reparo" : "Pintura", plan.Size.CellCount, options.Mode, resumeState is not null);

        _run = _engine.RunAsync(request, progress, _cts.Token).ContinueWith(OnRunFinished, TaskScheduler.Default);
    }

    public void Pause()
    {
        _engine?.Pause();
        _paused = true;
        _events.Emit(BridgeEvent.Of(BridgeEvents.PaintStatus, new { phase = "Paused" }));
        _overlay.LeavePainting(finished: false);
    }

    public void Resume()
    {
        _engine?.Resume();
        _paused = false;
        _events.Emit(BridgeEvent.Of(BridgeEvents.PaintStatus, new { phase = "Painting" }));
        _overlay.EnterPainting();
    }

    public void Abort() => _cts?.Cancel();

    /// <summary>
    /// Move o progresso da pintura para a célula <paramref name="cell"/> (0..total).
    /// Só com a pintura pausada: para frente pula, para trás repinta a partir dali.
    /// </summary>
    public void Seek(int cell)
    {
        if (!IsRunning) throw new InvalidOperationException("Nenhuma pintura em andamento.");
        if (!_paused) throw new InvalidOperationException("Pause a pintura antes de ajustar o progresso.");
        _engine?.SeekTo(cell);
        Log.Information("Progresso reposicionado para a célula {Cell}", cell);
    }

    private void OnRunFinished(Task t)
    {
        _hotkeys.Stop();
        _overlay.LeavePainting(finished: true);
        LastResume = _engine?.LastResumeState;

        var phase = t.IsCanceled ? "Aborted" : t.IsFaulted ? "Failed" : "Finished";
        if (t.IsFaulted)
            Log.Error(t.Exception, "Pintura falhou");

        _events.Emit(BridgeEvent.Of(BridgeEvents.PaintStatus, new
        {
            phase,
            canResume = LastResume is not null,
            message = t.Exception?.GetBaseException().Message
        }));
    }

    private void OnHotkey(object? sender, HotkeyPressedEventArgs e)
    {
        switch (e.Hotkey)
        {
            case PaintHotkey.Abort:
                Abort();
                break;
            case PaintHotkey.PauseResume:
                if (_paused) Resume(); else Pause();
                break;
        }
    }

    // Tela crua do canvas de pintura do Heartopia: branco da superfície + creme do
    // painel. Uma célula amostrada assim (com alvo diferente) conta como não pintada.
    private static readonly Rgb[] BareCanvas =
    {
        new(0xF7, 0xF5, 0xF0),
        new(0xEF, 0xE3, 0xC9),
    };

    private PaintOptions BuildOptions()
    {
        var c = _config.Current;
        return new PaintOptions
        {
            Mode = c.PaintMode,
            Timing = c.Timing.ToTiming(),
            EnableDragStrokes = c.EnableDragStrokes,
            VerifyAndRepair = c.VerifyAndRepair,
            VerifyToleranceRgb = c.VerifyToleranceRgb,
            VerifyMaxPasses = c.VerifyMaxPasses,
            BareCanvasColors = BareCanvas,
            SkipBackground = null
        };
    }
}

using System.Diagnostics;
using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Painting;

/// <summary>
/// Implementação base do motor de pintura. Faz a ordem, a seleção de tom e os
/// cliques célula a célula, reportando progresso. O passe de verificação/reparo
/// ainda é um stub (<see cref="VerifyAndRepair"/>).
/// </summary>
public sealed class PaintEngine : IPaintEngine
{
    private readonly IInputSender _input;
    private readonly IDelayService _delay;
    private readonly IScreenGeometry _geometry;
    private readonly IScreenReader _screen;

    private readonly ManualResetEventSlim _pauseGate = new(initialState: true);

    /// <summary>Índice de célula global pedido pela UI; -1 = nada pendente.</summary>
    private int _seekRequest = -1;

    public PaintEngine(IInputSender input, IDelayService delay, IScreenGeometry geometry, IScreenReader screen)
    {
        _input = input;
        _delay = delay;
        _geometry = geometry;
        _screen = screen;
    }

    public ResumeState? LastResumeState { get; private set; }

    public void Pause() => _pauseGate.Reset();
    public void Resume() => _pauseGate.Set();

    public void SeekTo(int globalCellIndex)
        => Interlocked.Exchange(ref _seekRequest, Math.Max(0, globalCellIndex));

    public Task RunAsync(PaintRequest request, IProgress<PaintProgress> progress, CancellationToken ct)
        // Cria a task sem o ct para o corpo sempre rodar e reportar o encerramento;
        // o cancelamento é cooperativo dentro de Run.
        => Task.Factory.StartNew(
            () => Run(request, progress, ct),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private void Run(PaintRequest request, IProgress<PaintProgress> progress, CancellationToken ct)
    {
        var (plan, calib, options, resume) = (request.Plan, request.Calibration, request.Options, request.Resume);
        var sw = Stopwatch.StartNew();

        var steps = new PaintOrderPlanner().Plan(plan, options);
        int total = steps.Sum(s => s.Cells.Count);

        // Offset acumulado de cada passo: converte um índice de célula global (o
        // valor da barra de progresso) em (passo, célula dentro do passo).
        var stepStart = new int[steps.Count + 1];
        for (int i = 0; i < steps.Count; i++)
            stepStart[i + 1] = stepStart[i] + steps[i].Cells.Count;

        int done = Math.Clamp(resume?.CellsDone ?? 0, 0, total);
        Interlocked.Exchange(ref _seekRequest, -1);

        progress.Report(new PaintProgress { Phase = PaintPhase.Preparing, CellsTotal = total, CellsDone = done, Elapsed = sw.Elapsed });

        try
        {
            if (request.RepairOnly)
            {
                progress.Report(new PaintProgress { Phase = PaintPhase.Repairing, CellsTotal = total, CellsDone = 0, Elapsed = sw.Elapsed });
                int fixedCells = VerifyAndRepair(plan, calib, options, progress, sw, PaintPhase.Repairing, ct);
                LastResumeState = null;
                progress.Report(Progress(PaintPhase.Finished, total, total, null, sw.Elapsed) with { CellsRepaired = fixedCells });
                return;
            }

            int si = LocateStep(stepStart, done);
            int selectedStep = -1; // -1 força re-selecionar o tom (início ou após seek)

            while (si < steps.Count || Volatile.Read(ref _seekRequest) >= 0)
            {
                // Grava o ponto de retomada antes de qualquer coisa que possa lançar
                // (pausa cancelada, seleção de tom): um abort aqui ainda retoma certo.
                if (si < steps.Count)
                    SaveResume(plan, calib, options, si, done - stepStart[si], done);

                _pauseGate.Wait(ct);

                int seek = Interlocked.Exchange(ref _seekRequest, -1);
                if (seek >= 0)
                {
                    done = Math.Clamp(seek, 0, total);
                    si = LocateStep(stepStart, done);
                    selectedStep = -1;
                    progress.Report(Progress(PaintPhase.Painting, done, total, null, sw.Elapsed));
                    if (si >= steps.Count) break; // arrastou até o fim
                }

                var step = steps[si];
                var shadeName = ShadeName(plan.PaletteSnapshot, step.Shade);

                int ci = done - stepStart[si];
                SaveResume(plan, calib, options, si, ci, done);

                if (si != selectedStep)
                {
                    SelectShade(plan.PaletteSnapshot, step.Shade, options.Timing, ct);
                    selectedStep = si;
                }

                bool seekInterrupted = false;
                for (; ci < step.Cells.Count; ci++)
                {
                    GuardFailsafe(ct);
                    _pauseGate.Wait(ct);

                    if (Volatile.Read(ref _seekRequest) >= 0) { seekInterrupted = true; break; }

                    SaveResume(plan, calib, options, si, ci, done);

                    var cell = step.Cells[ci];
                    var pt = calib.CellCenter(cell.Column, cell.Row, plan.Size);
                    _input.Tap(pt, options.Timing, ct);
                    done++;

                    if (done % 16 == 0 || ci == step.Cells.Count - 1)
                        progress.Report(Progress(PaintPhase.Painting, done, total, shadeName, sw.Elapsed));
                }

                if (seekInterrupted || Volatile.Read(ref _seekRequest) >= 0)
                    continue; // volta ao topo e consome o seek

                si++;
                _delay.Wait(TimeSpan.FromMilliseconds(options.Timing.RowDelayMs), ct, _pauseGate);
            }

            if (options.VerifyAndRepair)
            {
                progress.Report(Progress(PaintPhase.Verifying, done, total, null, sw.Elapsed));
                int repaired = VerifyAndRepair(plan, calib, options, progress, sw, PaintPhase.Verifying, ct);
                progress.Report(Progress(PaintPhase.Verifying, done, total, null, sw.Elapsed) with { CellsRepaired = repaired });
            }

            LastResumeState = null;
            progress.Report(Progress(PaintPhase.Finished, done, total, null, sw.Elapsed));
        }
        catch (OperationCanceledException)
        {
            progress.Report(Progress(PaintPhase.Aborted, done, total, null, sw.Elapsed));
            throw;
        }
        catch (Exception ex)
        {
            progress.Report(Progress(PaintPhase.Failed, done, total, null, sw.Elapsed) with { Message = ex.Message });
            throw;
        }
    }

    private void SelectShade(PaletteConfig palette, ShadeRef shade, PaintTiming timing, CancellationToken ct)
    {
        var main = palette.MainColors[shade.MainColorIndex];
        var shadeBtn = main.Shades[shade.ShadeIndex];

        if (palette.BackButton is { } back)
            _input.Tap(back, timing, ct);

        _input.Tap(main.Pos, timing, ct);

        if (palette.ShadesPanelButton is { } panel)
            _input.Tap(panel, timing, ct);

        _delay.Wait(TimeSpan.FromMilliseconds(timing.PanelOpenMs), ct, _pauseGate);
        _input.DoubleTap(shadeBtn.Pos, timing, ct);
        _delay.Wait(TimeSpan.FromMilliseconds(timing.ShadeSelectMs), ct, _pauseGate);
    }

    /// <summary>
    /// Reamostra cada célula, compara com a cor-alvo do tom e repinta as que passam
    /// da tolerância (ou que leem como tela crua do canvas). Ignora as que já estão
    /// certas. Repete até estabilizar ou bater <see cref="PaintOptions.VerifyMaxPasses"/>.
    /// Reporta progresso em <paramref name="phase"/> (Verifying no fim da pintura,
    /// Repairing quando roda sozinho).
    /// </summary>
    private int VerifyAndRepair(PaintPlan plan, CalibrationProfile calib, PaintOptions options,
        IProgress<PaintProgress> progress, Stopwatch sw, PaintPhase phase, CancellationToken ct)
    {
        var palette = plan.PaletteSnapshot;
        int totalCells = plan.Size.CellCount;
        int totalRepaired = 0;

        for (int pass = 0; pass < options.VerifyMaxPasses; pass++)
        {
            ct.ThrowIfCancellationRequested();
            _pauseGate.Wait(ct);

            // Agrupa as células divergentes por tom para não re-selecionar toda hora.
            var mismatchesByShade = new Dictionary<ShadeRef, List<CellPos>>();
            int scanned = 0;

            for (int row = 0; row < plan.Size.Rows; row++)
            for (int col = 0; col < plan.Size.Columns; col++)
            {
                if (++scanned % 128 == 0)
                {
                    GuardFailsafe(ct);
                    _pauseGate.Wait(ct);
                    progress.Report(Progress(phase, scanned, totalCells, null, sw.Elapsed) with { CellsRepaired = totalRepaired });
                }

                var shade = plan.Get(col, row);
                if (options.SkipBackground is { } bg && shade == bg)
                    continue;

                var target = palette.MainColors[shade.MainColorIndex].Shades[shade.ShadeIndex].Rgb;
                var pt = calib.CellCenter(col, row, plan.Size);
                var actual = _screen.SamplePixel(pt);

                // Célula em branco: amostrou a tela crua do canvas mas o alvo não é
                // essa cor -> não foi pintada (calibração pulou a célula).
                bool bareMiss = !IsBareCanvas(target, options) && IsBareCanvas(actual, options);

                if (bareMiss || actual.DistanceTo(target) > options.VerifyToleranceRgb)
                {
                    if (!mismatchesByShade.TryGetValue(shade, out var list))
                        mismatchesByShade[shade] = list = new List<CellPos>();
                    list.Add(new CellPos(col, row));
                }
            }

            if (mismatchesByShade.Count == 0)
                break;

            foreach (var (shade, cells) in mismatchesByShade)
            {
                GuardFailsafe(ct);
                _pauseGate.Wait(ct);
                SelectShade(palette, shade, options.Timing, ct);
                foreach (var cell in cells)
                {
                    GuardFailsafe(ct);
                    _input.Tap(calib.CellCenter(cell.Column, cell.Row, plan.Size), options.Timing, ct);
                    totalRepaired++;

                    if (totalRepaired % 16 == 0)
                        progress.Report(Progress(phase, totalCells, totalCells, null, sw.Elapsed) with { CellsRepaired = totalRepaired });
                }
            }

            _delay.Wait(TimeSpan.FromMilliseconds(options.Timing.RowDelayMs), ct, _pauseGate);
        }

        return totalRepaired;
    }

    private static bool IsBareCanvas(Rgb c, PaintOptions options)
    {
        int tol2 = options.BareCanvasToleranceRgb * options.BareCanvasToleranceRgb;
        foreach (var bare in options.BareCanvasColors)
            if (c.DistanceSquaredTo(bare) <= tol2)
                return true;
        return false;
    }

    private void GuardFailsafe(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_geometry.IsNearAnyCorner(_geometry.GetCursorPosition()))
            throw new OperationCanceledException("Failsafe: cursor em um canto da tela.");
    }

    /// <summary>Passo que contém a célula global <paramref name="done"/> (ou steps.Count se já passou do fim).</summary>
    private static int LocateStep(int[] stepStart, int done)
    {
        int last = stepStart.Length - 1; // = número de passos
        if (done >= stepStart[last]) return last;
        int i = Array.BinarySearch(stepStart, 0, last, done);
        if (i < 0) i = ~i - 1;
        return Math.Max(0, i);
    }

    private void SaveResume(Imaging.PaintPlan plan, CalibrationProfile calib, PaintOptions options, int step, int cell, int done)
        => LastResumeState = new ResumeState
        {
            PlanHash = plan.Hash,
            CalibrationHash = CalibrationHash.Of(calib),
            Mode = options.Mode,
            StepIndex = step,
            CellIndexInStep = cell,
            CellsDone = done
        };

    private static PaintProgress Progress(PaintPhase phase, int done, int total, string? shade, TimeSpan elapsed)
    {
        TimeSpan? eta = done > 0 && done < total
            ? elapsed * (total - done) / done
            : null;

        return new PaintProgress
        {
            Phase = phase,
            CellsDone = done,
            CellsTotal = total,
            CurrentShadeName = shade,
            Elapsed = elapsed,
            Eta = eta
        };
    }

    private static string? ShadeName(PaletteConfig palette, ShadeRef shade)
        => palette.MainColors.Count > shade.MainColorIndex
           && palette.MainColors[shade.MainColorIndex].Shades.Count > shade.ShadeIndex
            ? palette.MainColors[shade.MainColorIndex].Shades[shade.ShadeIndex].Name
            : null;
}

using System.Windows;
using AutoPaint.Core.Config;
using AutoPaint.Core.Painting;

namespace AutoPaint.App.Overlays;

/// <summary>
/// Durante a pintura o app sai da frente do jogo e, se ativado em Configurações,
/// mostra o <see cref="PaintStatusOverlay"/>. Ao pausar ou terminar, o card some e
/// a janela do app volta. Tudo roda na thread de UI.
/// </summary>
public sealed class PaintOverlayController
{
    private readonly IConfigStore _config;
    private PaintStatusOverlay? _overlay;
    private PaintProgress? _last;

    public PaintOverlayController(IConfigStore config) => _config = config;

    /// <summary>Pintura começou ou foi retomada: esconde o app e mostra o card.</summary>
    public void EnterPainting(bool fresh = false) => OnUi(() =>
    {
        if (fresh) _last = null;
        MinimizeMainWindow();
        ShowOverlay();
    });

    /// <summary>Pausou ou terminou: tira o card e traz o app de volta.</summary>
    public void LeavePainting(bool finished) => OnUi(() =>
    {
        CloseOverlay();
        if (finished) _last = null;
        RestoreMainWindow();
    });

    public void Report(PaintProgress p)
    {
        _last = p;
        OnUi(() => _overlay?.Update(p));
    }

    private void ShowOverlay()
    {
        CloseOverlay();
        var c = _config.Current;
        if (!c.StatusOverlayEnabled) return;

        bool en = c.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var texts = en
            ? new PaintStatusOverlay.Texts("Painting", "Verifying", "Repairing", "Preparing",
                "cells", "Shade", "Left", "pause", "stop", "or move the mouse to a screen corner to stop")
            : new PaintStatusOverlay.Texts("Pintando", "Verificando", "Reparando", "Preparando",
                "células", "Tom", "Falta", "pausar", "parar", "ou leve o mouse a um canto da tela para parar");
        string space = en ? "Space" : "Espaço";

        _overlay = new PaintStatusOverlay(
            c.StatusOverlayCorner,
            texts,
            c.Hotkeys.PauseResume.ToBinding().Parts(space),
            c.Hotkeys.Abort.ToBinding().Parts(space));
        _overlay.Show();
        if (_last is { } p) _overlay.Update(p);
    }

    private void CloseOverlay()
    {
        _overlay?.Close();
        _overlay = null;
    }

    private static void MinimizeMainWindow()
    {
        if (Application.Current?.MainWindow is { } w)
            w.WindowState = WindowState.Minimized;
    }

    private static void RestoreMainWindow()
    {
        if (Application.Current?.MainWindow is not { } w) return;
        if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
        // O Windows pode negar o foco a um app em segundo plano; o Topmost
        // momentâneo garante que a janela pelo menos apareça na frente do jogo.
        w.Topmost = true;
        w.Topmost = false;
        w.Activate();
    }

    private static void OnUi(Action action)
    {
        var d = Application.Current?.Dispatcher;
        if (d is null) return;
        if (d.CheckAccess()) action();
        else d.BeginInvoke(action);
    }
}

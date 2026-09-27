using System.Windows;
using AutoPaint.Core.Calibration;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.App.Overlays;

/// <summary>
/// Abre os overlays de calibração na thread de UI e devolve o resultado
/// (retângulo do canvas ou ponto de um botão), em pixels físicos.
/// </summary>
public sealed class CalibrationOverlayService
{
    private readonly IScreenGeometry _geometry;
    private readonly IScreenReader _screen;

    public CalibrationOverlayService(IScreenGeometry geometry, IScreenReader screen)
    {
        _geometry = geometry;
        _screen = screen;
    }

    public Task<PhysicalRect?> SelectCanvasRectAsync()
        => OnUi(() => new RectSelectWindow(_geometry, _screen).PickAsync());

    /// <summary>
    /// Calibração precisa por dois centros de célula: pede o clique no centro da
    /// célula (0,0) e no da célula (colunas-1, linhas-1). Deriva origem + passo,
    /// que não acumula deriva. Null se o usuário cancelar qualquer um dos pontos.
    /// </summary>
    public async Task<CanvasGrid?> SelectCanvasGridAsync(int columns, int rows, string firstLabel, string lastLabel)
    {
        if (columns < 2 || rows < 2) return null;

        var a = await OnUi(() => new PointSelectWindow(_geometry, _screen, firstLabel).PickAsync());
        if (a is not { } first) return null;

        var b = await OnUi(() => new PointSelectWindow(_geometry, _screen, lastLabel).PickAsync());
        if (b is not { } last) return null;

        return CanvasGrid.FromCorners(first, last, columns, rows);
    }

    /// <summary>
    /// Acha o canvas por análise de imagem, sem clique. Minimiza a janela do app
    /// para não aparecer na captura, fotografa o monitor primário e roda o
    /// <see cref="CanvasAutoDetector"/>. v1: só o monitor primário.
    /// </summary>
    public async Task<CanvasDetection> DetectCanvasRectAsync()
    {
        Action restore = static () => { };
        await OnUiVoid(() => restore = MinimizeMainWindow());
        try
        {
            await Task.Delay(200); // deixa a janela sair da frente antes do BitBlt
            var primary = _geometry.Monitors.FirstOrDefault(m => m.IsPrimary)?.Bounds
                          ?? _geometry.VirtualDesktop;
            return CanvasAutoDetector.Detect(_screen.CaptureRegion(primary));
        }
        finally
        {
            await OnUiVoid(() => restore());
        }
    }

    private static Action MinimizeMainWindow()
    {
        if (Application.Current?.MainWindow is not { } w) return static () => { };
        var prev = w.WindowState;
        w.WindowState = WindowState.Minimized;
        return () =>
        {
            w.WindowState = prev == WindowState.Minimized ? WindowState.Normal : prev;
            w.Activate();
        };
    }

    private static Task OnUiVoid(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        return dispatcher.InvokeAsync(action).Task;
    }

    public async Task<(PhysicalPoint Point, Rgb Rgb)?> CapturePointAsync(string label)
    {
        var pt = await OnUi(() => new PointSelectWindow(_geometry, _screen, label).PickAsync());
        if (pt is not { } p) return null;
        return (p, _screen.SamplePixel(p));
    }

    private static Task<T> OnUi<T>(Func<Task<T>> action)
    {
        var dispatcher = Application.Current.Dispatcher;
        return dispatcher.CheckAccess()
            ? action()
            : dispatcher.InvokeAsync(action).Task.Unwrap();
    }
}

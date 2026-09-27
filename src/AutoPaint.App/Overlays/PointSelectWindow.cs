using System.Windows.Input;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.App.Overlays;

/// <summary>Capturar 1 ponto (botão de cor/tom/ferramenta). Retorna em pixels físicos.</summary>
public sealed class PointSelectWindow : OverlayWindowBase
{
    private readonly TaskCompletionSource<PhysicalPoint?> _tcs = new();

    public PointSelectWindow(IScreenGeometry geometry, IScreenReader screen, string label)
        : base(geometry, screen, $"{label} • clique para capturar • botão direito / ESC cancela")
    {
        MouseLeftButtonDown += (_, e) =>
        {
            _tcs.TrySetResult(ToPhysical(e.GetPosition(OverlayCanvas)));
            Close();
        };
        MouseRightButtonDown += (_, _) => CancelAndClose();
        Closed += (_, _) => _tcs.TrySetResult(null);
    }

    public Task<PhysicalPoint?> PickAsync()
    {
        Show();
        Activate();
        return _tcs.Task;
    }

    protected override void CancelAndClose()
    {
        _tcs.TrySetResult(null);
        Close();
    }
}

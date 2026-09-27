using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.App.Overlays;

/// <summary>Arrastar um retângulo sobre o canvas do jogo. Retorna em pixels físicos.</summary>
public sealed class RectSelectWindow : OverlayWindowBase
{
    private readonly TaskCompletionSource<PhysicalRect?> _tcs = new();
    private readonly Rectangle _band;
    private Point? _startDip;

    public RectSelectWindow(IScreenGeometry geometry, IScreenReader screen)
        : base(geometry, screen, "Arraste para selecionar o canvas • ESC cancela • roda = zoom")
    {
        _band = new Rectangle
        {
            Stroke = Brushes.Cyan,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Fill = new SolidColorBrush(Color.FromArgb(0x22, 0, 255, 255)),
            Visibility = Visibility.Collapsed
        };
        OverlayCanvas.Children.Add(_band);

        MouseLeftButtonDown += OnDown;
        MouseMove += OnMove;
        MouseLeftButtonUp += OnUp;
        Closed += (_, _) => _tcs.TrySetResult(null);
    }

    public Task<PhysicalRect?> PickAsync()
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

    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        _startDip = e.GetPosition(OverlayCanvas);
        _band.Visibility = Visibility.Visible;
        UpdateBand(_startDip.Value);
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (_startDip is not null && e.LeftButton == MouseButtonState.Pressed)
            UpdateBand(e.GetPosition(OverlayCanvas));
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (_startDip is not { } start) return;
        var end = e.GetPosition(OverlayCanvas);

        var a = ToPhysical(new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)));
        var b = ToPhysical(new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y)));
        var rect = new PhysicalRect(a.X, a.Y, b.X - a.X, b.Y - a.Y);

        if (rect.Width < 5 || rect.Height < 5)
        {
            _band.Visibility = Visibility.Collapsed;
            _startDip = null;
            return; // muito pequeno; continua esperando
        }

        _tcs.TrySetResult(rect);
        Close();
    }

    private void UpdateBand(Point current)
    {
        if (_startDip is not { } s) return;
        double x = Math.Min(s.X, current.X);
        double y = Math.Min(s.Y, current.Y);
        Canvas.SetLeft(_band, x);
        Canvas.SetTop(_band, y);
        _band.Width = Math.Abs(current.X - s.X);
        _band.Height = Math.Abs(current.Y - s.Y);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.App.Overlays;

/// <summary>
/// Base dos overlays de calibração: janela sem borda cobrindo o monitor primário,
/// levemente escurecida, com lupa seguindo o cursor. Trabalha em pixels físicos.
/// v1: só o monitor primário (escopo recomenda o jogo no primário). Multi-monitor
/// com DPIs diferentes fica para depois.
/// </summary>
public abstract class OverlayWindowBase : Window
{
    private const double MagnifierBoxSize = 170;
    private const int MinZoom = 2;
    private const int MaxZoom = 20;

    protected readonly IScreenReader Screen;
    private readonly IScreenGeometry _geometry;

    private readonly Image _magImage;
    private readonly Border _magBox;
    private readonly TextBlock _hint;
    protected readonly Canvas OverlayCanvas;

    private DpiScale _dpi = new(1, 1);
    private PhysicalRect _primary;
    private int _zoom = 10;

    protected OverlayWindowBase(IScreenGeometry geometry, IScreenReader screen, string hintText)
    {
        _geometry = geometry;
        Screen = screen;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;
        Background = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));

        _hint = new TextBlock
        {
            Text = hintText,
            Foreground = Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 24, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(12, 6, 12, 6),
        };
        _hint.Background = new SolidColorBrush(Color.FromArgb(0xB0, 0, 0, 0));

        _magImage = new Image
        {
            Width = MagnifierBoxSize,
            Height = MagnifierBoxSize,
            Stretch = Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(_magImage, BitmapScalingMode.NearestNeighbor);

        var crosshair = new Path
        {
            Stroke = Brushes.Cyan,
            StrokeThickness = 1,
            Data = Geometry.Parse(
                $"M {MagnifierBoxSize / 2},0 L {MagnifierBoxSize / 2},{MagnifierBoxSize} " +
                $"M 0,{MagnifierBoxSize / 2} L {MagnifierBoxSize},{MagnifierBoxSize / 2}")
        };

        _magBox = new Border
        {
            Width = MagnifierBoxSize,
            Height = MagnifierBoxSize,
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Background = Brushes.Black,
            Visibility = Visibility.Collapsed,
            Child = new Grid { Children = { _magImage, crosshair } }
        };

        OverlayCanvas = new Canvas();
        var root = new Grid();
        root.Children.Add(OverlayCanvas);
        root.Children.Add(_hint);
        Canvas.SetLeft(_magBox, 0);
        Canvas.SetTop(_magBox, 0);
        OverlayCanvas.Children.Add(_magBox);
        Content = root;

        SourceInitialized += OnSourceInitialized;
        MouseMove += OnMouseMoveBase;
        MouseWheel += (_, e) => _zoom = Math.Clamp(_zoom + Math.Sign(e.Delta), MinZoom, MaxZoom);
        KeyDown += (_, e) => { if (e.Key == Key.Escape) CancelAndClose(); };
        Deactivated += (_, _) => Activate();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _dpi = VisualTreeHelper.GetDpi(this);
        var primary = _geometry.Monitors.FirstOrDefault(m => m.IsPrimary)
                      ?? _geometry.Monitors.FirstOrDefault();
        _primary = primary?.Bounds ?? _geometry.VirtualDesktop;
        if (_primary.IsEmpty) _primary = new PhysicalRect(0, 0, 1920, 1080);

        Left = _primary.X / _dpi.DpiScaleX;
        Top = _primary.Y / _dpi.DpiScaleY;
        Width = _primary.Width / _dpi.DpiScaleX;
        Height = _primary.Height / _dpi.DpiScaleY;
    }

    protected PhysicalPoint ToPhysical(Point dip) => new(
        (int)Math.Round(_primary.X + dip.X * _dpi.DpiScaleX),
        (int)Math.Round(_primary.Y + dip.Y * _dpi.DpiScaleY));

    private void OnMouseMoveBase(object sender, MouseEventArgs e)
    {
        var dip = e.GetPosition(OverlayCanvas);
        var phys = ToPhysical(dip);

        // Lado da região de origem = caixa / fator de zoom -> ampliação real de _zoom vezes
        // (px físicos por px de origem; o Image estica com NearestNeighbor).
        int sourceSize = Math.Max(6, (int)Math.Round(MagnifierBoxSize / _zoom));
        var img = MagnifierRenderer.TryRender(Screen, phys, sourceSize);
        if (img is not null)
        {
            _magImage.Source = img;
            _magBox.Visibility = Visibility.Visible;

            double bx = dip.X + 24;
            double by = dip.Y + 24;
            if (bx + MagnifierBoxSize > ActualWidth) bx = dip.X - 24 - MagnifierBoxSize;
            if (by + MagnifierBoxSize > ActualHeight) by = dip.Y - 24 - MagnifierBoxSize;
            Canvas.SetLeft(_magBox, bx);
            Canvas.SetTop(_magBox, by);
        }
    }

    protected abstract void CancelAndClose();
}

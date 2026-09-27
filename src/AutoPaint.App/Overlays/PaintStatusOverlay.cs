using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using AutoPaint.Core.Painting;

namespace AutoPaint.App.Overlays;

/// <summary>
/// Card de progresso sobreposto ao jogo durante a pintura. Não rouba foco, deixa
/// os cliques passarem (WS_EX_TRANSPARENT) e fica fora de capturas de tela
/// (WDA_EXCLUDEFROMCAPTURE) para não sujar a verificação/reparo, que leem pixels.
/// </summary>
public sealed class PaintStatusOverlay : Window
{
    public sealed record Texts(
        string Painting, string Verifying, string Repairing, string Preparing,
        string Cells, string Shade, string Remaining, string Pause, string Stop, string CornerStop);

    private const double Margin_ = 18;
    private const double CardWidth = 300;

    private static readonly Color Accent = Color.FromRgb(0x87, 0x79, 0xFF);
    private static readonly Color Pink = Color.FromRgb(0xE0, 0x24, 0x5E);

    private readonly string _corner;
    private readonly Texts _t;

    private readonly TextBlock _phase;
    private readonly TextBlock _percent;
    private readonly ColumnDefinition _done;
    private readonly ColumnDefinition _left;
    private readonly TextBlock _details;

    public PaintStatusOverlay(string corner, Texts texts, IReadOnlyList<string> pauseKeys, IReadOnlyList<string> stopKeys)
    {
        _corner = corner;
        _t = texts;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        FontFamily = new FontFamily("Segoe UI");

        // --- Cabeçalho: bolinha pulsando + fase ............... 42% ---
        var dot = new Border
        {
            Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Accent), Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        dot.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.25, TimeSpan.FromSeconds(0.8))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever
        });

        _phase = new TextBlock
        {
            Text = texts.Preparing.ToUpperInvariant(), FontSize = 11, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(0xB8, 0xFF, 0xFF, 0xFF)), VerticalAlignment = VerticalAlignment.Center
        };
        _percent = new TextBlock
        {
            Text = "0%", FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center
        };

        var head = new Grid();
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(dot, 0); Grid.SetColumn(_phase, 1); Grid.SetColumn(_percent, 2);
        head.Children.Add(dot); head.Children.Add(_phase); head.Children.Add(_percent);

        // --- Barra de progresso em degradê ---
        _done = new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) };
        _left = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
        var fill = new Border
        {
            CornerRadius = new CornerRadius(3),
            Background = new LinearGradientBrush(Accent, Pink, 0)
        };
        var bar = new Grid { Height = 6, Margin = new Thickness(0, 8, 0, 8) };
        bar.ColumnDefinitions.Add(_done);
        bar.ColumnDefinitions.Add(_left);
        var track = new Border { CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)) };
        Grid.SetColumnSpan(track, 2);
        bar.Children.Add(track);
        bar.Children.Add(fill);

        _details = new TextBlock
        {
            FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(0xD0, 0xFF, 0xFF, 0xFF)),
            TextWrapping = TextWrapping.Wrap
        };

        // --- Atalhos ---
        var shortcuts = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        shortcuts.Children.Add(ShortcutRow(pauseKeys, texts.Pause));
        shortcuts.Children.Add(ShortcutRow(stopKeys, texts.Stop));
        shortcuts.Children.Add(new TextBlock
        {
            Text = texts.CornerStop, FontSize = 10.5, Margin = new Thickness(0, 4, 0, 0),
            Foreground = new SolidColorBrush(Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF))
        });

        var divider = new Border
        {
            Height = 1, Margin = new Thickness(0, 10, 0, 0),
            Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF))
        };

        var stack = new StackPanel();
        stack.Children.Add(head);
        stack.Children.Add(bar);
        stack.Children.Add(_details);
        stack.Children.Add(divider);
        stack.Children.Add(shortcuts);

        Content = new Border
        {
            Width = CardWidth,
            Margin = new Thickness(14), // espaço para a sombra
            Padding = new Thickness(16, 14, 16, 14),
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromArgb(0xEB, 0x16, 0x16, 0x1E)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, Accent.R, Accent.G, Accent.B)),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 4, Opacity = 0.45, Color = Colors.Black },
            Child = stack
        };

        SizeChanged += (_, _) => PlaceInCorner();
        SourceInitialized += (_, _) => MakeClickThrough();
    }

    public void Update(PaintProgress p)
    {
        double f = Math.Clamp(p.Fraction, 0, 1);
        _percent.Text = $"{Math.Round(f * 100)}%";
        _done.Width = new GridLength(f, GridUnitType.Star);
        _left.Width = new GridLength(1 - f, GridUnitType.Star);
        _phase.Text = (p.Phase switch
        {
            PaintPhase.Painting => _t.Painting,
            PaintPhase.Verifying => _t.Verifying,
            PaintPhase.Repairing => _t.Repairing,
            _ => _t.Preparing
        }).ToUpperInvariant();

        var parts = new List<string> { $"{p.CellsDone:N0} / {p.CellsTotal:N0} {_t.Cells}" };
        if (!string.IsNullOrEmpty(p.CurrentShadeName)) parts.Add($"{_t.Shade} {p.CurrentShadeName}");
        if (p.Eta is { } eta) parts.Add($"{_t.Remaining} {FormatEta(eta)}");
        _details.Text = string.Join("  ·  ", parts);
    }

    private static string FormatEta(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}min"
        : t.TotalMinutes >= 1 ? $"{t.Minutes}min {t.Seconds}s"
        : $"{t.Seconds}s";

    private static UIElement ShortcutRow(IReadOnlyList<string> keys, string label)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        for (int i = 0; i < keys.Count; i++)
        {
            if (i > 0)
                row.Children.Add(new TextBlock
                {
                    Text = "+", FontSize = 11, Margin = new Thickness(3, 0, 3, 0), VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF))
                });
            row.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(5), Padding = new Thickness(6, 1, 6, 2),
                Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)), BorderThickness = new Thickness(1, 1, 1, 2),
                Child = new TextBlock { Text = keys[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White }
            });
        }
        row.Children.Add(new TextBlock
        {
            Text = label, FontSize = 12, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromArgb(0xD0, 0xFF, 0xFF, 0xFF))
        });
        return row;
    }

    private void PlaceInCorner()
    {
        var area = SystemParameters.WorkArea; // monitor primário, sem a barra de tarefas (DIPs)
        bool right = _corner.EndsWith("Right", StringComparison.Ordinal);
        bool bottom = _corner.StartsWith("Bottom", StringComparison.Ordinal);
        Left = right ? area.Right - ActualWidth - Margin_ + 14 : area.Left + Margin_ - 14;
        Top = bottom ? area.Bottom - ActualHeight - Margin_ + 14 : area.Top + Margin_ - 14;
    }

    // ---------------- Win32 ----------------

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

    private void MakeClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE);
        // Windows 10 2004+; em versões antigas falha em silêncio e o card só aparece nas capturas.
        SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
}

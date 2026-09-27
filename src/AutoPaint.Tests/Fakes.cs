using AutoPaint.Core.Imaging;
using AutoPaint.Core.Painting;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

/// <summary>Grava as ações do motor de pintura sem tocar no mouse real (dry-run).</summary>
public sealed class RecordingInputSender : IInputSender
{
    public List<(string Kind, PhysicalPoint Point)> Actions { get; } = new();

    public void MoveTo(PhysicalPoint point) => Actions.Add(("move", point));
    public void LeftDown() => Actions.Add(("down", default));
    public void LeftUp() => Actions.Add(("up", default));
    public void Tap(PhysicalPoint point, PaintTiming timing, CancellationToken ct) => Actions.Add(("tap", point));
    public void DoubleTap(PhysicalPoint point, PaintTiming timing, CancellationToken ct) => Actions.Add(("doubletap", point));

    public IEnumerable<PhysicalPoint> Taps => Actions.Where(a => a.Kind == "tap").Select(a => a.Point);
    public int DoubleTapCount => Actions.Count(a => a.Kind == "doubletap");
}

/// <summary>Como o <see cref="RecordingInputSender"/>, mas chama um hook antes de
/// cada Tap — deixa o teste injetar um SeekTo no meio da pintura, de forma
/// determinística (tudo na thread do motor).</summary>
public sealed class HookingInputSender : IInputSender
{
    private readonly RecordingInputSender _inner = new();

    /// <summary>Chamado com o alvo de cada Tap, imediatamente antes de registrá-lo.</summary>
    public Action<PhysicalPoint>? BeforeTap { get; set; }

    public List<(string Kind, PhysicalPoint Point)> Actions => _inner.Actions;
    public IEnumerable<PhysicalPoint> Taps => _inner.Taps;
    public int DoubleTapCount => _inner.DoubleTapCount;

    public void MoveTo(PhysicalPoint point) => _inner.MoveTo(point);
    public void LeftDown() => _inner.LeftDown();
    public void LeftUp() => _inner.LeftUp();
    public void DoubleTap(PhysicalPoint point, PaintTiming timing, CancellationToken ct) => _inner.DoubleTap(point, timing, ct);

    public void Tap(PhysicalPoint point, PaintTiming timing, CancellationToken ct)
    {
        BeforeTap?.Invoke(point);
        _inner.Tap(point, timing, ct);
    }
}

public sealed class NoDelay : IDelayService
{
    public int SliceMs => 1;
    public void Wait(TimeSpan duration, CancellationToken ct, ManualResetEventSlim? pause = null)
        => ct.ThrowIfCancellationRequested();
}

public sealed class FakeGeometry : IScreenGeometry
{
    public IReadOnlyList<MonitorInfo> Monitors { get; } =
        new[] { new MonitorInfo(new PhysicalRect(0, 0, 3840, 2160), true) };
    public PhysicalRect VirtualDesktop => Monitors[0].Bounds;
    public PhysicalPoint GetCursorPosition() => new(1920, 1080);
    public bool IsNearAnyCorner(PhysicalPoint point, int threshold = 5) => false;
}

/// <summary>Tela toda de uma cor (default branco).</summary>
public sealed class SolidScreenReader : IScreenReader
{
    private readonly Rgb _color;
    public SolidScreenReader(Rgb? color = null) => _color = color ?? new Rgb(255, 255, 255);

    public Rgb SamplePixel(PhysicalPoint point) => _color;

    public ScreenCapture CaptureRegion(PhysicalRect region)
    {
        int w = Math.Max(1, region.Width), h = Math.Max(1, region.Height);
        var px = new Rgb[w * h];
        Array.Fill(px, _color);
        return new ScreenCapture(new PhysicalRect(region.X, region.Y, w, h), px);
    }
}

public sealed class ListProgress<T> : IProgress<T>
{
    public List<T> Items { get; } = new();
    public void Report(T value) => Items.Add(value);
}

using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Platform;

/// <summary>Uma região de tela capturada, em pixels físicos (row-major RGB).</summary>
public sealed record ScreenCapture(PhysicalRect Bounds, Rgb[] Pixels)
{
    public Rgb Get(int localX, int localY) => Pixels[localY * Bounds.Width + localX];
}

/// <summary>
/// Leitura de pixels da tela: amostra pontual (calibração da paleta, verificação)
/// e captura de região (lupa dos overlays). Implementação Windows usa GDI/BitBlt.
/// </summary>
public interface IScreenReader
{
    Rgb SamplePixel(PhysicalPoint point);
    ScreenCapture CaptureRegion(PhysicalRect region);
}

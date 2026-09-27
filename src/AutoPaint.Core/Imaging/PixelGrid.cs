using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Imaging;

/// <summary>
/// Grade de pixels RGB em row-major (índice = y * Width + x).
/// Resultado do redimensionamento da imagem de entrada para CxR.
/// </summary>
public sealed class PixelGrid
{
    private readonly Rgb[] _pixels;

    public int Width { get; }
    public int Height { get; }

    public PixelGrid(int width, int height, Rgb[] pixels)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Dimensões devem ser positivas.");
        if (pixels.Length != width * height)
            throw new ArgumentException("Tamanho do vetor de pixels não bate com width*height.", nameof(pixels));

        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public Rgb Get(int x, int y) => _pixels[y * Width + x];

    public ReadOnlySpan<Rgb> Row(int y) => _pixels.AsSpan(y * Width, Width);
}

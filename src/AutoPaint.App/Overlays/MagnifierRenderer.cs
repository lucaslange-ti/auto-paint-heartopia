using System.Windows.Media;
using System.Windows.Media.Imaging;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.App.Overlays;

/// <summary>
/// Constrói a imagem ampliada da região ao redor do cursor (lupa de calibração),
/// a partir de uma captura de tela em pixels físicos.
/// </summary>
public static class MagnifierRenderer
{
    /// <param name="sourceSize">lado da região capturada, em px físicos (~36).</param>
    public static BitmapSource? TryRender(IScreenReader screen, PhysicalPoint center, int sourceSize)
    {
        int half = sourceSize / 2;
        var region = new PhysicalRect(center.X - half, center.Y - half, sourceSize, sourceSize);

        ScreenCapture cap;
        try
        {
            cap = screen.CaptureRegion(region);
        }
        catch
        {
            return null;
        }

        int w = cap.Bounds.Width, h = cap.Bounds.Height;
        var wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        var buffer = new byte[w * h * 4];

        for (int i = 0; i < w * h; i++)
        {
            var px = cap.Pixels[i];
            buffer[i * 4 + 0] = px.B;
            buffer[i * 4 + 1] = px.G;
            buffer[i * 4 + 2] = px.R;
            buffer[i * 4 + 3] = 255;
        }

        wb.WritePixels(new System.Windows.Int32Rect(0, 0, w, h), buffer, w * 4, 0);
        wb.Freeze();
        return wb;
    }
}

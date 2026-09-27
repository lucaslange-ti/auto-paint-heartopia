using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;
using Rgb = AutoPaint.Core.Primitives.Rgb;

namespace AutoPaint.Platform.Windows;

/// <summary>
/// <see cref="IScreenReader"/> via GDI. Amostra pontual com GetPixel num DC de
/// tela; região via Graphics.CopyFromScreen (BitBlt). Requer processo DPI-aware.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GdiScreenReader : IScreenReader
{
    public Rgb SamplePixel(PhysicalPoint point)
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        try
        {
            uint c = GetPixel(hdc, point.X, point.Y);
            byte r = (byte)(c & 0xFF);
            byte g = (byte)((c >> 8) & 0xFF);
            byte b = (byte)((c >> 16) & 0xFF);
            return new Rgb(r, g, b);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }

    public ScreenCapture CaptureRegion(PhysicalRect region)
    {
        int w = Math.Max(1, region.Width);
        int h = Math.Max(1, region.Height);

        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(region.X, region.Y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);

        var pixels = new Rgb[w * h];
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            unsafe
            {
                byte* scan0 = (byte*)data.Scan0;
                for (int y = 0; y < h; y++)
                {
                    byte* row = scan0 + y * data.Stride;
                    for (int x = 0; x < w; x++)
                    {
                        byte b = row[x * 4 + 0];
                        byte gg = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];
                        pixels[y * w + x] = new Rgb(r, gg, b);
                    }
                }
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        return new ScreenCapture(new PhysicalRect(region.X, region.Y, w, h), pixels);
    }

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr hdc, int x, int y);
}

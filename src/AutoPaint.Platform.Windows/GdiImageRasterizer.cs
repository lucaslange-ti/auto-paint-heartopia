using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using AutoPaint.Core.Imaging;
using Rgb = AutoPaint.Core.Primitives.Rgb;

namespace AutoPaint.Platform.Windows;

/// <summary>
/// <see cref="IImageRasterizer"/> via System.Drawing. Alpha compositado sobre
/// branco. GDI+ não tem Lanczos — <see cref="ResampleMode.Lanczos"/> mapeia para
/// HighQualityBicubic (desvio conhecido do projeto de referência).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GdiImageRasterizer : IImageRasterizer
{
    public PixelGrid Rasterize(Stream imageData, CanvasSize target, FitMode fit, ResampleMode resample)
    {
        using var source = new Bitmap(imageData);
        using var canvas = new Bitmap(target.Columns, target.Rows, PixelFormat.Format24bppRgb);

        using (var g = Graphics.FromImage(canvas))
        {
            g.Clear(Color.White);
            g.InterpolationMode = resample == ResampleMode.NearestNeighbor
                ? InterpolationMode.NearestNeighbor
                : InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            var dest = ComputeDestRect(source.Width, source.Height, target, fit);
            g.DrawImage(source, dest, new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
        }

        return ToGrid(canvas);
    }

    private static Rectangle ComputeDestRect(int srcW, int srcH, CanvasSize target, FitMode fit)
    {
        double sx = (double)target.Columns / srcW;
        double sy = (double)target.Rows / srcH;
        double scale = fit == FitMode.Contain ? Math.Min(sx, sy) : Math.Max(sx, sy);

        int w = (int)Math.Round(srcW * scale);
        int h = (int)Math.Round(srcH * scale);
        int x = (target.Columns - w) / 2;
        int y = (target.Rows - h) / 2;
        return new Rectangle(x, y, w, h);
    }

    private static PixelGrid ToGrid(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        var pixels = new Rgb[w * h];
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
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
                        byte b = row[x * 3 + 0];
                        byte gg = row[x * 3 + 1];
                        byte r = row[x * 3 + 2];
                        pixels[y * w + x] = new Rgb(r, gg, b);
                    }
                }
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        return new PixelGrid(w, h, pixels);
    }
}

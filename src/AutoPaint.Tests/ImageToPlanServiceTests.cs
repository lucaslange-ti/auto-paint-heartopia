using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class ImageToPlanServiceTests
{
    // Rasterizador fake: metade preta, metade branca, no tamanho pedido.
    private sealed class HalfBlackHalfWhiteRasterizer : IImageRasterizer
    {
        public PixelGrid Rasterize(Stream imageData, CanvasSize target, FitMode fit, ResampleMode resample)
        {
            var px = new Rgb[target.CellCount];
            for (int y = 0; y < target.Rows; y++)
            for (int x = 0; x < target.Columns; x++)
                px[y * target.Columns + x] = x < target.Columns / 2 ? new Rgb(0, 0, 0) : new Rgb(255, 255, 255);
            return new PixelGrid(target.Columns, target.Rows, px);
        }
    }

    private static readonly PaletteConfig Pal = new(
        new[]
        {
            new MainColor("m", new PhysicalPoint(0, 0), new Rgb(0, 0, 0), new[]
            {
                new ShadeButton("preto", new PhysicalPoint(1, 0), new Rgb(0, 0, 0)),
                new ShadeButton("branco", new PhysicalPoint(2, 0), new Rgb(255, 255, 255)),
            })
        }, null, null, null);

    [Fact]
    public void Converts_1x1_small_to_30x30_and_matches_palette()
    {
        var svc = new ImageToPlanService(new HalfBlackHalfWhiteRasterizer(), new PlanBuilder());
        using var fakeImage = new MemoryStream(new byte[] { 1, 2, 3 });

        var plan = svc.Convert(fakeImage, CanvasAspect.OneToOne, CanvasPrecision.Small,
            FitMode.Contain, ResampleMode.Lanczos, Pal);

        Assert.Equal(30, plan.Size.Columns);
        Assert.Equal(30, plan.Size.Rows);
        Assert.Equal(new ShadeRef(0, 0), plan.Get(0, 0));   // metade esquerda = preto
        Assert.Equal(new ShadeRef(0, 1), plan.Get(29, 0));  // metade direita = branco
    }

    [Fact]
    public void Summary_counts_shades_and_orders_by_frequency()
    {
        var svc = new ImageToPlanService(new HalfBlackHalfWhiteRasterizer(), new PlanBuilder());
        using var fakeImage = new MemoryStream(new byte[] { 0 });

        var plan = svc.Convert(fakeImage, CanvasAspect.OneToOne, CanvasPrecision.Small,
            FitMode.Contain, ResampleMode.Lanczos, Pal);
        var summary = ImageToPlanService.Summarize(plan);

        Assert.Equal(900, summary.CellCount);
        Assert.Equal(2, summary.Usage.Count);
        Assert.Equal(900, summary.Usage.Sum(u => u.Count));
        Assert.True(summary.Usage[0].Count >= summary.Usage[1].Count);
    }
}

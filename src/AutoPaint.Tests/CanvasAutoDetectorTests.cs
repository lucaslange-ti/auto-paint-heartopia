using AutoPaint.Core.Calibration;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class CanvasAutoDetectorTests
{
    private static readonly Rgb Cream = new(239, 227, 201); // #EFE3C9, painel do jogo
    private static readonly Rgb White = new(247, 245, 240);  // faixa de desenho

    private static ScreenCapture Make(int w, int h, Func<int, int, Rgb> at, int ox = 0, int oy = 0)
    {
        var px = new Rgb[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = at(x, y);
        return new ScreenCapture(new PhysicalRect(ox, oy, w, h), px);
    }

    [Fact]
    public void Finds_the_white_surface_inside_the_cream_panel()
    {
        // Painel creme 400x300; superfície branca em [140,260) x [40,240).
        var cap = Make(400, 300, (x, y) =>
            x is >= 140 and < 260 && y is >= 40 and < 240 ? White : Cream, ox: 100, oy: 50);

        var d = CanvasAutoDetector.Detect(cap);

        Assert.True(d.Found);
        Assert.Null(d.Reason);
        // O offset da captura entra no resultado (pixels físicos absolutos).
        Assert.InRange(d.Rect.X, 238, 242);
        Assert.InRange(d.Rect.Y, 88, 92);
        Assert.InRange(d.Rect.Width, 116, 124);
        Assert.InRange(d.Rect.Height, 196, 204);
    }

    [Fact]
    public void Rejects_a_screen_that_is_all_white()
    {
        var d = CanvasAutoDetector.Detect(Make(400, 300, (_, _) => White));
        Assert.False(d.Found);
    }

    [Fact]
    public void Rejects_a_screen_with_no_white_area()
    {
        var d = CanvasAutoDetector.Detect(Make(400, 300, (_, _) => Cream));
        Assert.False(d.Found);
    }

    [Fact]
    public void Rejects_a_white_block_not_bordered_by_cream()
    {
        // Bloco branco sobre fundo cinza-escuro (nenhum creme dos lados) -> diálogo, não canvas.
        var gray = new Rgb(70, 70, 72);
        var cap = Make(400, 300, (x, y) =>
            x is >= 120 and < 280 && y is >= 60 and < 240 ? White : gray);

        var d = CanvasAutoDetector.Detect(cap);
        Assert.False(d.Found);
    }
}

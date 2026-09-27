using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class ColorMatcherTests
{
    private static PaletteConfig Palette(params (byte r, byte g, byte b)[] shades)
    {
        var list = shades
            .Select((c, i) => new ShadeButton($"s{i}", new PhysicalPoint(i, 0), new Rgb(c.r, c.g, c.b)))
            .ToArray();
        return new PaletteConfig(
            new[] { new MainColor("m0", new PhysicalPoint(0, 0), list[0].Rgb, list) },
            null, null, null);
    }

    [Fact]
    public void Picks_exact_color()
    {
        var matcher = new ColorMatcher(Palette((0, 0, 0), (255, 255, 255), (255, 0, 0)));
        Assert.Equal(new ShadeRef(0, 2), matcher.FindNearest(new Rgb(255, 0, 0)));
    }

    [Fact]
    public void Picks_nearest_when_no_exact()
    {
        var matcher = new ColorMatcher(Palette((0, 0, 0), (255, 255, 255)));
        Assert.Equal(new ShadeRef(0, 1), matcher.FindNearest(new Rgb(200, 200, 200)));
        Assert.Equal(new ShadeRef(0, 0), matcher.FindNearest(new Rgb(40, 40, 40)));
    }

    [Fact]
    public void Empty_palette_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ColorMatcher(new PaletteConfig(Array.Empty<MainColor>(), null, null, null)));
    }
}

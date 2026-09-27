using AutoPaint.Core.Config;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class CalibrationMappingTests
{
    private static ShadeDto Shade(string name, int x, bool enabled = true) =>
        new() { Name = name, Pos = new PointDto { X = x, Y = 10 }, Enabled = enabled };

    private static MainColorDto Color(string name, params ShadeDto[] shades) =>
        new() { Name = name, Shades = shades.ToList() };

    [Fact]
    public void Disabled_shades_are_left_out_and_keep_their_own_positions()
    {
        var dto = new CalibrationDto
        {
            MainColors = { Color("P2", Shade("T1", 100), Shade("T2", 200, enabled: false), Shade("T3", 300)) }
        };

        var shades = dto.ToDomain().Palette.MainColors.Single().Shades;

        Assert.Equal(new[] { "T1", "T3" }, shades.Select(s => s.Name));
        Assert.Equal(new PhysicalPoint(300, 10), shades[1].Pos);
    }

    [Fact]
    public void Color_with_every_shade_disabled_leaves_the_palette()
    {
        var dto = new CalibrationDto
        {
            MainColors =
            {
                Color("P1", Shade("T1", 100, enabled: false)),
                Color("P2", Shade("T1", 100))
            }
        };

        Assert.Equal(new[] { "P2" }, dto.ToDomain().Palette.MainColors.Select(c => c.Name));
    }
}

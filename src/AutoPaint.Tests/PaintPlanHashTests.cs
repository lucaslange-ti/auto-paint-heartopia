using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class PaintPlanHashTests
{
    private static readonly PaletteConfig Pal = new(
        new[]
        {
            new MainColor("m", new PhysicalPoint(0, 0), new Rgb(0, 0, 0), new[]
            {
                new ShadeButton("a", new PhysicalPoint(1, 0), new Rgb(0, 0, 0)),
                new ShadeButton("b", new PhysicalPoint(2, 0), new Rgb(9, 9, 9)),
            })
        }, null, null, null);

    [Fact]
    public void Hash_is_stable_for_same_cells()
    {
        var a = new PaintPlan(new CanvasSize(2, 1), new[] { new ShadeRef(0, 0), new ShadeRef(0, 1) }, Pal);
        var b = new PaintPlan(new CanvasSize(2, 1), new[] { new ShadeRef(0, 0), new ShadeRef(0, 1) }, Pal);
        Assert.Equal(a.Hash, b.Hash);
    }

    [Fact]
    public void Hash_changes_when_a_cell_changes()
    {
        var a = new PaintPlan(new CanvasSize(2, 1), new[] { new ShadeRef(0, 0), new ShadeRef(0, 1) }, Pal);
        var b = new PaintPlan(new CanvasSize(2, 1), new[] { new ShadeRef(0, 0), new ShadeRef(0, 0) }, Pal);
        Assert.NotEqual(a.Hash, b.Hash);
    }

    [Fact]
    public void Cell_count_must_match_grid()
    {
        Assert.Throws<ArgumentException>(() =>
            new PaintPlan(new CanvasSize(2, 2), new[] { new ShadeRef(0, 0) }, Pal));
    }
}

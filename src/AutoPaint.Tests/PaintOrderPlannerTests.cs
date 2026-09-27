using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Painting;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class PaintOrderPlannerTests
{
    // Paleta com 2 tons na mesma cor principal.
    private static readonly PaletteConfig Pal = new(
        new[]
        {
            new MainColor("m", new PhysicalPoint(0, 0), new Rgb(0, 0, 0), new[]
            {
                new ShadeButton("a", new PhysicalPoint(1, 0), new Rgb(0, 0, 0)),
                new ShadeButton("b", new PhysicalPoint(2, 0), new Rgb(255, 255, 255)),
            })
        }, null, null, null);

    private static readonly ShadeRef A = new(0, 0);
    private static readonly ShadeRef B = new(0, 1);

    private static PaintPlan Plan(int w, int h, params ShadeRef[] cells)
        => new(new CanvasSize(w, h), cells, Pal);

    [Fact]
    public void Row_mode_groups_horizontal_runs_and_serpentines()
    {
        // linha 0: A A B   | linha 1: B B B
        var plan = Plan(3, 2, A, A, B, B, B, B);
        var steps = new PaintOrderPlanner().Plan(plan, new PaintOptions { Mode = PaintMode.Row });

        Assert.Equal(3, steps.Count);
        Assert.Equal(new[] { new CellPos(0, 0), new CellPos(1, 0) }, steps[0].Cells);
        Assert.Equal(new[] { new CellPos(2, 0) }, steps[1].Cells);
        // linha 1 vai da direita para a esquerda (serpentina)
        Assert.Equal(new[] { new CellPos(2, 1), new CellPos(1, 1), new CellPos(0, 1) }, steps[2].Cells);
    }

    [Fact]
    public void Color_mode_orders_biggest_group_first()
    {
        var plan = Plan(3, 1, A, B, B);
        var steps = new PaintOrderPlanner().Plan(plan, new PaintOptions { Mode = PaintMode.Color });

        Assert.Equal(2, steps.Count);
        Assert.Equal(B, steps[0].Shade);
        Assert.Equal(2, steps[0].Cells.Count);
        Assert.Equal(A, steps[1].Shade);
    }

    [Fact]
    public void Skip_background_removes_those_cells()
    {
        var plan = Plan(3, 1, A, B, B);
        var steps = new PaintOrderPlanner().Plan(plan, new PaintOptions { Mode = PaintMode.Color, SkipBackground = B });

        Assert.Single(steps);
        Assert.Equal(A, steps[0].Shade);
    }
}

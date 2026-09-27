using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class CanvasGridTests
{
    [Fact]
    public void FromRect_places_the_first_center_half_a_step_in()
    {
        var g = CanvasGrid.FromRect(new PhysicalRect(100, 200, 560, 1000), 56, 100);

        Assert.Equal(10.0, g.PitchX);
        Assert.Equal(10.0, g.PitchY);
        Assert.Equal(new PhysicalPoint(105, 205), g.CellCenter(0, 0));
        Assert.Equal(new PhysicalPoint(655, 1195), g.CellCenter(55, 99));
    }

    [Fact]
    public void FromCorners_rejects_degenerate_input()
    {
        Assert.Null(CanvasGrid.FromCorners(new PhysicalPoint(0, 0), new PhysicalPoint(100, 100), 1, 10));
        Assert.Null(CanvasGrid.FromCorners(new PhysicalPoint(50, 50), new PhysicalPoint(10, 10), 4, 4)); // invertido
    }

    [Fact]
    public void FromCorners_spreads_endpoint_error_so_no_cell_drifts_a_full_step()
    {
        const int cols = 56, rows = 100;
        const double pitch = 5.64; // ~5,6 px/célula: aqui a deriva vira linha em branco
        const double ox = 300.0, oy = 200.0;

        PhysicalPoint TrueCenter(int c, int r) =>
            new((int)Math.Round(ox + c * pitch), (int)Math.Round(oy + r * pitch));

        // Canto final capturado com 3 px de erro (clique impreciso mesmo com lupa).
        var first = new PhysicalPoint((int)Math.Round(ox), (int)Math.Round(oy));
        var last = new PhysicalPoint(
            (int)Math.Round(ox + (cols - 1) * pitch) + 3,
            (int)Math.Round(oy + (rows - 1) * pitch) + 3);

        var grid = CanvasGrid.FromCorners(first, last, cols, rows)!;

        int maxDev = 0;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var g = grid.CellCenter(c, r);
            var tru = TrueCenter(c, r);
            maxDev = Math.Max(maxDev, Math.Max(Math.Abs(g.X - tru.X), Math.Abs(g.Y - tru.Y)));
        }

        // 3 px diluídos por 55/99 células: desvio máx ~3 px, bem abaixo de um passo (5,64).
        Assert.True(maxDev <= 3, $"desvio máx {maxDev} px");
    }

    [Fact]
    public void Profile_prefers_the_grid_when_it_matches_the_plan_size()
    {
        var grid = new CanvasGrid(10.0, 20.0, 5.0, 5.0, 4, 3);
        var profile = new CalibrationProfile(new PhysicalRect(0, 0, 999, 999), PaletteConfig.Empty, grid);

        // Grade bate (4x3) -> origem + passo, retângulo ignorado.
        Assert.Equal(new PhysicalPoint(10, 20), profile.CellCenter(0, 0, new CanvasSize(4, 3)));
        Assert.Equal(new PhysicalPoint(25, 30), profile.CellCenter(3, 2, new CanvasSize(4, 3)));

        // Grade diferente -> cai no retângulo (i+0.5)*passo.
        var viaRect = profile.CellCenter(0, 0, new CanvasSize(10, 10));
        Assert.Equal(new PhysicalPoint(50, 50), viaRect);
    }

    [Fact]
    public void CoversGrid_true_for_a_matching_grid_or_any_rect()
    {
        var withGrid = new CalibrationProfile(
            new PhysicalRect(0, 0, 0, 0), PaletteConfig.Empty, new CanvasGrid(0, 0, 1, 1, 4, 3));
        Assert.True(withGrid.CoversGrid(new CanvasSize(4, 3)));
        Assert.False(withGrid.CoversGrid(new CanvasSize(5, 5)));

        var withRect = new CalibrationProfile(new PhysicalRect(0, 0, 100, 100), PaletteConfig.Empty);
        Assert.True(withRect.CoversGrid(new CanvasSize(5, 5)));
    }
}

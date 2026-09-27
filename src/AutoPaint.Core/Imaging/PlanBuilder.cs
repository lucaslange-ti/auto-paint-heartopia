using AutoPaint.Core.Calibration;

namespace AutoPaint.Core.Imaging;

/// <summary>
/// Converte uma <see cref="PixelGrid"/> já redimensionada num <see cref="PaintPlan"/>,
/// casando cada pixel com o tom mais próximo da paleta. Puro e testável.
/// </summary>
public sealed class PlanBuilder
{
    public PaintPlan Build(PixelGrid grid, PaletteConfig palette)
    {
        var size = new CanvasSize(grid.Width, grid.Height);
        var matcher = new ColorMatcher(palette);
        var cells = new ShadeRef[size.CellCount];

        for (int y = 0; y < grid.Height; y++)
        for (int x = 0; x < grid.Width; x++)
            cells[y * grid.Width + x] = matcher.FindNearest(grid.Get(x, y));

        return new PaintPlan(size, cells, palette);
    }
}

using AutoPaint.Core.Calibration;
using AutoPaint.Core.Imaging;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Tests;

public class CellCenterTests
{
    [Fact]
    public void Center_uses_half_pixel_offset_over_the_rect()
    {
        // Canvas 100x100 px, grade 10x10 -> passo 10, centro da primeira célula = (5,5).
        var profile = new CalibrationProfile(
            new PhysicalRect(200, 300, 100, 100),
            PaletteConfig.Empty);
        var grid = new CanvasSize(10, 10);

        Assert.Equal(new PhysicalPoint(205, 305), profile.CellCenter(0, 0, grid));
        Assert.Equal(new PhysicalPoint(295, 395), profile.CellCenter(9, 9, grid));
        Assert.Equal(new PhysicalPoint(255, 355), profile.CellCenter(5, 5, grid));
    }
}

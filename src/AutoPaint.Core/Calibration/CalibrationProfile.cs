using AutoPaint.Core.Imaging;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Calibration;

/// <summary>
/// Tudo que a pintura precisa saber sobre a tela: onde fica o canvas no jogo (em
/// pixels físicos) e a paleta mapeada. Perfil único na v1.
///
/// A geometria vem de duas formas: <see cref="Grid"/> (origem + passo, preferido —
/// à prova de deriva) ou <see cref="CanvasRect"/> (retângulo de borda, legado).
/// Quando <see cref="Grid"/> serve para a grade pedida, ele ganha.
/// </summary>
public sealed record CalibrationProfile(PhysicalRect CanvasRect, PaletteConfig Palette, CanvasGrid? Grid = null)
{
    public static CalibrationProfile Empty { get; } =
        new(new PhysicalRect(0, 0, 0, 0), PaletteConfig.Empty);

    /// <summary>
    /// Centro da célula (col, row) em pixels físicos. Usa <see cref="Grid"/> se ele
    /// for da mesma grade; senão cai no retângulo de borda + (i+0.5)*passo.
    /// </summary>
    public PhysicalPoint CellCenter(int column, int row, CanvasSize grid)
    {
        if (Grid is { } g && g.Matches(grid))
            return g.CellCenter(column, row);

        double px = CanvasRect.X + (column + 0.5) * CanvasRect.Width / grid.Columns;
        double py = CanvasRect.Y + (row + 0.5) * CanvasRect.Height / grid.Rows;
        return new PhysicalPoint((int)Math.Round(px), (int)Math.Round(py));
    }

    public bool IsCalibrated => (Grid is not null || !CanvasRect.IsEmpty) && Palette.ShadeCount > 0;

    /// <summary>A geometria calibrada serve para pintar esta grade?</summary>
    public bool CoversGrid(CanvasSize size) =>
        (Grid is { } g && g.Matches(size)) || !CanvasRect.IsEmpty;
}

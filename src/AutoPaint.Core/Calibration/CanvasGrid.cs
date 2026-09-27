using AutoPaint.Core.Imaging;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Calibration;

/// <summary>
/// Geometria da grade em pixels físicos por <b>origem + passo</b>, em vez do
/// retângulo de borda. O centro da célula (0,0) é <see cref="OriginX"/>/<see cref="OriginY"/>
/// e cada célula seguinte fica a <see cref="PitchX"/>/<see cref="PitchY"/> de distância.
///
/// Vantagem sobre o retângulo: o passo vem do espaçamento real entre células, então
/// um erro de calibração se dilui por todas as células e nunca acumula uma célula
/// inteira de deriva (causa das "linhas em branco" quando o canvas tem ~5 px/célula).
/// </summary>
public sealed record CanvasGrid(double OriginX, double OriginY, double PitchX, double PitchY, int Columns, int Rows)
{
    public PhysicalPoint CellCenter(int column, int row) => new(
        (int)Math.Round(OriginX + column * PitchX),
        (int)Math.Round(OriginY + row * PitchY));

    /// <summary>Retângulo aproximado coberto pela grade (bordas a meio passo dos centros extremos).</summary>
    public PhysicalRect BoundingRect => new(
        (int)Math.Round(OriginX - PitchX / 2),
        (int)Math.Round(OriginY - PitchY / 2),
        (int)Math.Round(PitchX * Columns),
        (int)Math.Round(PitchY * Rows));

    /// <summary>
    /// Deriva origem + passo de um retângulo de borda + a contagem de células:
    /// passo = lado / contagem; origem = borda + meio passo.
    /// </summary>
    public static CanvasGrid FromRect(PhysicalRect rect, int columns, int rows)
    {
        double pitchX = rect.Width / (double)columns;
        double pitchY = rect.Height / (double)rows;
        return new CanvasGrid(rect.X + pitchX / 2, rect.Y + pitchY / 2, pitchX, pitchY, columns, rows);
    }

    /// <summary>
    /// Deriva origem + passo de dois <b>centros de célula</b> capturados: o da
    /// célula (0,0) e o da célula (columns-1, rows-1). Retorna null se os pontos
    /// estiverem invertidos/coincidentes ou a grade for pequena demais.
    /// </summary>
    public static CanvasGrid? FromCorners(PhysicalPoint firstCell, PhysicalPoint lastCell, int columns, int rows)
    {
        if (columns < 2 || rows < 2) return null;
        double pitchX = (lastCell.X - firstCell.X) / (double)(columns - 1);
        double pitchY = (lastCell.Y - firstCell.Y) / (double)(rows - 1);
        if (pitchX <= 0 || pitchY <= 0) return null;
        return new CanvasGrid(firstCell.X, firstCell.Y, pitchX, pitchY, columns, rows);
    }

    public bool Matches(CanvasSize size) => Columns == size.Columns && Rows == size.Rows;
}

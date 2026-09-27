using System.Security.Cryptography;
using System.Text;
using AutoPaint.Core.Calibration;

namespace AutoPaint.Core.Imaging;

/// <summary>
/// Resultado da conversão: para cada célula da grade, qual tom da paleta pintar.
/// Row-major. Carrega um snapshot da paleta e um hash para validar retomada.
/// </summary>
public sealed class PaintPlan
{
    private readonly ShadeRef[] _cells;

    public CanvasSize Size { get; }
    public PaletteConfig PaletteSnapshot { get; }
    public string Hash { get; }

    public PaintPlan(CanvasSize size, ShadeRef[] cells, PaletteConfig paletteSnapshot)
    {
        if (cells.Length != size.CellCount)
            throw new ArgumentException("Número de células não bate com a grade.", nameof(cells));

        Size = size;
        _cells = cells;
        PaletteSnapshot = paletteSnapshot;
        Hash = ComputeHash(size, cells);
    }

    public ShadeRef Get(int column, int row) => _cells[row * Size.Columns + column];

    public ReadOnlySpan<ShadeRef> Cells => _cells;

    private static string ComputeHash(CanvasSize size, ShadeRef[] cells)
    {
        var sb = new StringBuilder();
        sb.Append(size.Columns).Append('x').Append(size.Rows).Append(':');
        foreach (var c in cells)
            sb.Append(c.MainColorIndex).Append(',').Append(c.ShadeIndex).Append(';');

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(digest);
    }
}

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AutoPaint.Core.Calibration;

/// <summary>
/// Hash estável da calibração (retângulo do canvas + posições/cores da paleta).
/// Usado para só oferecer retomada quando a tela não mudou.
/// </summary>
public static class CalibrationHash
{
    public static string Of(CalibrationProfile profile)
    {
        var sb = new StringBuilder();
        var r = profile.CanvasRect;
        sb.Append(r.X).Append(',').Append(r.Y).Append(',').Append(r.Width).Append(',').Append(r.Height).Append('|');

        if (profile.Grid is { } g)
            sb.Append("G:")
              .Append(g.OriginX.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
              .Append(g.OriginY.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
              .Append(g.PitchX.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
              .Append(g.PitchY.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
              .Append(g.Columns).Append('x').Append(g.Rows).Append('|');

        var p = profile.Palette;
        Append(sb, p.ShadesPanelButton);
        Append(sb, p.BackButton);
        Append(sb, p.PaintToolButton);

        foreach (var main in p.MainColors)
        {
            sb.Append(main.Name).Append('@').Append(main.Pos.X).Append(',').Append(main.Pos.Y)
              .Append('#').Append(main.Rgb).Append('{');
            foreach (var s in main.Shades)
                sb.Append(s.Name).Append('@').Append(s.Pos.X).Append(',').Append(s.Pos.Y).Append('#').Append(s.Rgb).Append(';');
            sb.Append('}');
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(digest);
    }

    private static void Append(StringBuilder sb, Primitives.PhysicalPoint? point)
    {
        if (point is { } p) sb.Append(p.X).Append(',').Append(p.Y);
        sb.Append('|');
    }
}

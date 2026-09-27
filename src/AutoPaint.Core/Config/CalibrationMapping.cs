using AutoPaint.Core.Calibration;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Config;

/// <summary>Converte entre o DTO persistido e o domínio <see cref="CalibrationProfile"/>.</summary>
public static class CalibrationMapping
{
    public static CalibrationProfile ToDomain(this CalibrationDto dto)
    {
        var rect = dto.CanvasRect is { } r
            ? new PhysicalRect(r.X, r.Y, r.Width, r.Height)
            : new PhysicalRect(0, 0, 0, 0);

        var grid = dto.CanvasGrid is { } g
            ? new CanvasGrid(g.OriginX, g.OriginY, g.PitchX, g.PitchY, g.Columns, g.Rows)
            : null;

        var mains = dto.MainColors
            // Cada tom leva a própria posição, então filtrar os desativados não
            // desalinha nada; cor sem nenhum tom ativo sai da paleta.
            .Where(m => m.Enabled && m.Shades.Any(s => s.Enabled))
            .Select(m => new MainColor(
                m.Name,
                new PhysicalPoint(m.Pos.X, m.Pos.Y),
                new Rgb(m.Rgb.R, m.Rgb.G, m.Rgb.B),
                m.Shades
                    .Where(s => s.Enabled)
                    .Select(s => new ShadeButton(s.Name, new PhysicalPoint(s.Pos.X, s.Pos.Y), new Rgb(s.Rgb.R, s.Rgb.G, s.Rgb.B)))
                    .ToArray()))
            .ToArray();

        var palette = new PaletteConfig(
            mains,
            ToPoint(dto.ShadesPanelButtonPos),
            ToPoint(dto.BackButtonPos),
            ToPoint(dto.PaintToolButtonPos));

        return new CalibrationProfile(rect, palette, grid);
    }

    private static PhysicalPoint? ToPoint(PointDto? p) => p is null ? null : new PhysicalPoint(p.X, p.Y);
}

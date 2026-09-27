using AutoPaint.Core.Calibration;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Imaging;

/// <summary>
/// Casa uma cor arbitrária com o tom mais próximo da paleta configurada, por
/// distância euclidiana em RGB (igual ao projeto de referência). Cache de RGBs
/// repetidos. Trocar por Lab/CIEDE2000 depois é isolado a esta classe.
/// </summary>
public sealed class ColorMatcher
{
    private readonly (ShadeRef Ref, Rgb Rgb)[] _entries;
    private readonly Dictionary<Rgb, ShadeRef> _cache = new();

    public ColorMatcher(PaletteConfig palette)
    {
        var list = new List<(ShadeRef, Rgb)>();
        for (int c = 0; c < palette.MainColors.Count; c++)
        {
            var shades = palette.MainColors[c].Shades;
            for (int s = 0; s < shades.Count; s++)
                list.Add((new ShadeRef(c, s), shades[s].Rgb));
        }

        if (list.Count == 0)
            throw new ArgumentException("Paleta sem tons cadastrados.", nameof(palette));

        _entries = list.ToArray();
    }

    public ShadeRef FindNearest(Rgb color)
    {
        if (_cache.TryGetValue(color, out var cached))
            return cached;

        int bestDist = int.MaxValue;
        ShadeRef best = _entries[0].Ref;

        foreach (var (shadeRef, rgb) in _entries)
        {
            int d = color.DistanceSquaredTo(rgb);
            if (d < bestDist)
            {
                bestDist = d;
                best = shadeRef;
                if (d == 0) break;
            }
        }

        _cache[color] = best;
        return best;
    }
}

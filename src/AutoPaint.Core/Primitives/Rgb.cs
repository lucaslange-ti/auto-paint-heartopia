namespace AutoPaint.Core.Primitives;

/// <summary>Cor RGB de 8 bits por canal. Imutável.</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    /// <summary>Distância euclidiana ao quadrado em RGB (evita o sqrt).</summary>
    public int DistanceSquaredTo(Rgb other)
    {
        int dr = R - other.R;
        int dg = G - other.G;
        int db = B - other.B;
        return dr * dr + dg * dg + db * db;
    }

    /// <summary>Distância euclidiana em RGB.</summary>
    public double DistanceTo(Rgb other) => Math.Sqrt(DistanceSquaredTo(other));

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

namespace AutoPaint.Core.Primitives;

/// <summary>
/// Ponto em <b>pixels físicos</b> da tela (não DIPs do WPF).
/// Toda a automação e calibração trabalham nesta unidade.
/// </summary>
public readonly record struct PhysicalPoint(int X, int Y);

/// <summary>Retângulo em pixels físicos da tela.</summary>
public readonly record struct PhysicalRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(PhysicalPoint p) =>
        p.X >= X && p.X < Right && p.Y >= Y && p.Y < Bottom;
}

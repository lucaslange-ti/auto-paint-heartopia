using AutoPaint.Core.Primitives;

namespace AutoPaint.Core.Platform;

public sealed record MonitorInfo(PhysicalRect Bounds, bool IsPrimary);

/// <summary>
/// Geometria dos monitores em <b>pixels físicos</b>. Usada pelos overlays (que
/// cobrem o virtual desktop) e pelo failsafe (cursor perto de um canto).
/// </summary>
public interface IScreenGeometry
{
    IReadOnlyList<MonitorInfo> Monitors { get; }

    /// <summary>Retângulo que engloba todos os monitores.</summary>
    PhysicalRect VirtualDesktop { get; }

    PhysicalPoint GetCursorPosition();

    /// <summary>True se o ponto está a até <paramref name="threshold"/> px de um canto de monitor.</summary>
    bool IsNearAnyCorner(PhysicalPoint point, int threshold = 5);
}

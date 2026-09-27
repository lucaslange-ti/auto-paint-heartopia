using System.Runtime.InteropServices;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Platform.Windows;

/// <summary>
/// <see cref="IScreenGeometry"/> via EnumDisplayMonitors + GetCursorPos. Assume o
/// processo PerMonitorV2 (manifesto), então tudo já vem em pixels físicos.
/// </summary>
public sealed class Win32ScreenGeometry : IScreenGeometry
{
    public IReadOnlyList<MonitorInfo> Monitors { get; private set; } = Array.Empty<MonitorInfo>();

    public Win32ScreenGeometry() => Refresh();

    public void Refresh()
    {
        var list = new List<MonitorInfo>();

        bool Callback(IntPtr hMonitor, IntPtr hdc, ref RECT lprc, IntPtr data)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                var r = mi.rcMonitor;
                list.Add(new MonitorInfo(
                    new PhysicalRect(r.left, r.top, r.right - r.left, r.bottom - r.top),
                    (mi.dwFlags & MONITORINFOF_PRIMARY) != 0));
            }
            return true;
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
        Monitors = list.Count > 0 ? list : new[] { new MonitorInfo(new PhysicalRect(0, 0, 1920, 1080), true) };
    }

    public PhysicalRect VirtualDesktop
    {
        get
        {
            if (Monitors.Count == 0) return new PhysicalRect(0, 0, 1920, 1080);
            int minX = Monitors.Min(m => m.Bounds.X);
            int minY = Monitors.Min(m => m.Bounds.Y);
            int maxX = Monitors.Max(m => m.Bounds.Right);
            int maxY = Monitors.Max(m => m.Bounds.Bottom);
            return new PhysicalRect(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public PhysicalPoint GetCursorPosition()
        => GetCursorPos(out var p) ? new PhysicalPoint(p.X, p.Y) : new PhysicalPoint(0, 0);

    public bool IsNearAnyCorner(PhysicalPoint point, int threshold = 5)
    {
        foreach (var m in Monitors)
        {
            var b = m.Bounds;
            foreach (var corner in new[]
            {
                new PhysicalPoint(b.X, b.Y),
                new PhysicalPoint(b.Right - 1, b.Y),
                new PhysicalPoint(b.X, b.Bottom - 1),
                new PhysicalPoint(b.Right - 1, b.Bottom - 1)
            })
            {
                if (Math.Abs(point.X - corner.X) <= threshold && Math.Abs(point.Y - corner.Y) <= threshold)
                    return true;
            }
        }
        return false;
    }

    // ---- Win32 ----

    private const uint MONITORINFOF_PRIMARY = 0x1;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
}

using System.Runtime.InteropServices;
using AutoPaint.Core.Painting;
using AutoPaint.Core.Platform;
using AutoPaint.Core.Primitives;

namespace AutoPaint.Platform.Windows;

/// <summary>
/// <see cref="IInputSender"/> via SendInput (mouse absoluto no virtual desktop).
/// Sequência <see cref="Tap"/> = mover -> down -> segurar -> up -> esperar,
/// igual ao projeto de referência.
/// </summary>
public sealed class WindowsInputSender : IInputSender
{
    private readonly IDelayService _delay;
    private readonly IScreenGeometry _geometry;

    public WindowsInputSender(IDelayService delay, IScreenGeometry geometry)
    {
        _delay = delay;
        _geometry = geometry;
    }

    public void MoveTo(PhysicalPoint point)
    {
        var vd = _geometry.VirtualDesktop;
        // Normaliza para 0..65535 sobre o virtual desktop.
        int nx = (int)Math.Round((point.X - vd.X) * 65535.0 / Math.Max(1, vd.Width - 1));
        int ny = (int)Math.Round((point.Y - vd.Y) * 65535.0 / Math.Max(1, vd.Height - 1));

        SendMouse(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, nx, ny);
    }

    public void LeftDown() => SendMouse(MOUSEEVENTF_LEFTDOWN, 0, 0);
    public void LeftUp() => SendMouse(MOUSEEVENTF_LEFTUP, 0, 0);

    public void Tap(PhysicalPoint point, PaintTiming timing, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        MoveTo(point);
        _delay.Wait(TimeSpan.FromMilliseconds(timing.MoveDurationMs), ct);
        LeftDown();
        _delay.Wait(TimeSpan.FromMilliseconds(timing.MouseDownMs), ct);
        LeftUp();
        _delay.Wait(TimeSpan.FromMilliseconds(timing.AfterClickMs), ct);
    }

    public void DoubleTap(PhysicalPoint point, PaintTiming timing, CancellationToken ct)
    {
        Tap(point, timing, ct);
        Tap(point, timing, ct);
    }

    // ---- Win32 ----

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

    private static void SendMouse(uint flags, int dx, int dy)
    {
        var input = new INPUT
        {
            type = 0, // INPUT_MOUSE
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        _ = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}

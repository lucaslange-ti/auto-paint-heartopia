using System.Runtime.InteropServices;
using AutoPaint.Core.Platform;

namespace AutoPaint.Platform.Windows;

/// <summary>
/// Hotkeys globais via hook WH_KEYBOARD_LL <b>não-consumidor</b>, ativo só entre
/// <see cref="Start"/> e <see cref="Stop"/>. As combinações chegam em <see cref="Start"/>
/// (configuráveis; padrão Ctrl+Alt+X = abortar, Ctrl+Alt+Space = pausar/retomar).
/// <see cref="Start"/> deve ser chamado numa thread com bomba de mensagens (a thread de UI).
/// </summary>
public sealed class LowLevelHotkeyService : IHotkeyService
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt
    private const int VK_SHIFT = 0x10;

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hook = IntPtr.Zero;
    private HotkeyBinding _abort = HotkeyBinding.DefaultAbort;
    private HotkeyBinding _pauseResume = HotkeyBinding.DefaultPauseResume;

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public LowLevelHotkeyService() => _proc = HookCallback;

    public void Start(HotkeyBinding abort, HotkeyBinding pauseResume)
    {
        _abort = abort;
        _pauseResume = pauseResume;
        if (_hook != IntPtr.Zero) return;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
    }

    public void Stop()
    {
        if (_hook == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            int vk = Marshal.ReadInt32(lParam); // KBDLLHOOKSTRUCT.vkCode é o 1º campo
            bool ctrl = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
            bool alt = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
            bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

            if (_abort.Matches(vk, ctrl, alt, shift))
                HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(PaintHotkey.Abort));
            else if (_pauseResume.Matches(vk, ctrl, alt, shift))
                HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(PaintHotkey.PauseResume));
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam); // nunca consome
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

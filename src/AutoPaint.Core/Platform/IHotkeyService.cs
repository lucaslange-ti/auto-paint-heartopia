namespace AutoPaint.Core.Platform;

public enum PaintHotkey { Abort, PauseResume }

public sealed class HotkeyPressedEventArgs(PaintHotkey hotkey) : EventArgs
{
    public PaintHotkey Hotkey { get; } = hotkey;
}

/// <summary>
/// Combinação de teclas: modificadores + uma tecla (virtual-key code do Windows).
/// Os modificadores precisam bater exatamente: Ctrl+Alt+X não dispara Alt+X.
/// </summary>
public readonly record struct HotkeyBinding(bool Ctrl, bool Alt, bool Shift, int Key)
{
    public static HotkeyBinding DefaultAbort { get; } = new(true, true, false, 0x58);       // Ctrl+Alt+X
    public static HotkeyBinding DefaultPauseResume { get; } = new(true, true, false, 0x20); // Ctrl+Alt+Espaço

    public bool Matches(int key, bool ctrl, bool alt, bool shift) =>
        Key != 0 && key == Key && ctrl == Ctrl && alt == Alt && shift == Shift;

    /// <summary>Partes para exibir: ["Ctrl", "Alt", "Espaço"].</summary>
    public IReadOnlyList<string> Parts(string spaceLabel = "Espaço")
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(KeyName(Key, spaceLabel));
        return parts;
    }

    private static string KeyName(int vk, string spaceLabel) => vk switch
    {
        0x20 => spaceLabel,
        >= 0x41 and <= 0x5A or >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x60 and <= 0x69 => $"Num {vk - 0x60}",
        >= 0x70 and <= 0x7B => $"F{vk - 0x6F}",
        0x13 => "Pause", 0x2D => "Insert", 0x2E => "Delete", 0x24 => "Home", 0x23 => "End",
        0x21 => "PageUp", 0x22 => "PageDown", 0x25 => "←", 0x26 => "↑", 0x27 => "→", 0x28 => "↓",
        _ => $"#{vk}"
    };
}

/// <summary>
/// Hotkeys globais durante a pintura. Implementação Windows usa um hook
/// WH_KEYBOARD_LL não-consumidor, ativo apenas entre <see cref="Start"/> e
/// <see cref="Stop"/>. As combinações vêm da configuração a cada início.
/// </summary>
public interface IHotkeyService
{
    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    void Start(HotkeyBinding abort, HotkeyBinding pauseResume);
    void Stop();
}

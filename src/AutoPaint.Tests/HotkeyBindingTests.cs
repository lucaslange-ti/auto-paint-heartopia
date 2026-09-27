using AutoPaint.Core.Config;
using AutoPaint.Core.Platform;

namespace AutoPaint.Tests;

public class HotkeyBindingTests
{
    [Fact]
    public void Modifiers_must_match_exactly()
    {
        var ctrlAltX = HotkeyBinding.DefaultAbort;

        Assert.True(ctrlAltX.Matches(0x58, ctrl: true, alt: true, shift: false));
        Assert.False(ctrlAltX.Matches(0x58, ctrl: false, alt: true, shift: false));   // falta Ctrl
        Assert.False(ctrlAltX.Matches(0x58, ctrl: true, alt: true, shift: true));     // Shift a mais
        Assert.False(ctrlAltX.Matches(0x20, ctrl: true, alt: true, shift: false));    // outra tecla
    }

    [Fact]
    public void Key_without_modifiers_works_for_function_keys()
    {
        var f8 = new HotkeyBinding(false, false, false, 0x77);

        Assert.True(f8.Matches(0x77, false, false, false));
        Assert.False(f8.Matches(0x77, true, false, false));
    }

    [Fact]
    public void Empty_binding_never_fires()
    {
        Assert.False(default(HotkeyBinding).Matches(0, false, false, false));
    }

    [Fact]
    public void Parts_lists_modifiers_then_key()
    {
        Assert.Equal(new[] { "Ctrl", "Alt", "Espaço" }, HotkeyBinding.DefaultPauseResume.Parts());
        Assert.Equal(new[] { "Ctrl", "Alt", "X" }, HotkeyBinding.DefaultAbort.Parts());
        Assert.Equal(new[] { "Shift", "F8" }, new HotkeyBinding(false, false, true, 0x77).Parts());
        Assert.Equal(new[] { "Ctrl", "Space" }, new HotkeyBinding(true, false, false, 0x20).Parts("Space"));
    }

    [Fact]
    public void Config_defaults_are_the_classic_shortcuts()
    {
        var hk = new AppConfig().Hotkeys;

        Assert.Equal(HotkeyBinding.DefaultAbort, hk.Abort.ToBinding());
        Assert.Equal(HotkeyBinding.DefaultPauseResume, hk.PauseResume.ToBinding());
    }
}

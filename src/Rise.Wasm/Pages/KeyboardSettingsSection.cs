using Rise.Slices.Settings.Abstractions;

namespace Rise.Wasm.Pages;

/// <summary>Shell-owned section: the hotkey actions are app-wide, so the manager lives here, not in a slice.</summary>
internal sealed class KeyboardSettingsSection : ISettingsSection
{
    public string Title => "Keyboard";

    public int Order => 160;

    public string Description => "Keyboard shortcuts for Rise. Rebind any action, or reset to the defaults.";

    public Type ComponentType => typeof(KeyboardSettings);
}

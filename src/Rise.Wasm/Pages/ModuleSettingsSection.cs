using Rise.Slices.Settings.Abstractions;

namespace Rise.Wasm.Pages;

/// <summary>Shell-owned settings section: the module switchboard.</summary>
internal sealed class ModuleSettingsSection : ISettingsSection
{
    public string Title => "Modules";

    public int Order => 210;

    public string Description => "Turn modules on or off.";

    public Type ComponentType => typeof(ModuleSettings);
}

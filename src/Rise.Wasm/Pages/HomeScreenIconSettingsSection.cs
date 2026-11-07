using Rise.Slices.Settings.Abstractions;

namespace Rise.Wasm.Pages;

/// <summary>
/// Shell-owned settings section: the home screen icon. The manifest is
/// shell-owned, so the toggle lives here, not in a slice. Order 90 puts
/// it first among contributed sections, right after the built-in Theme
/// section (the next contributed section, Links, is Order 100).
/// </summary>
internal sealed class HomeScreenIconSettingsSection : ISettingsSection
{
    public string Title => "Home screen icon";

    public int Order => 90;

    public string Description => "The icon Rise installs with on your home screen.";

    public Type ComponentType => typeof(HomeScreenIconSettings);
}

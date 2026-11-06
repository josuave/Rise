using Rise.Slices.Settings.Abstractions;

namespace Rise.Wasm.Pages;

/// <summary>Shell-owned settings section: the background picker.</summary>
internal sealed class BackgroundSettingsSection : ISettingsSection
{
    public string Title => "Background";

    public int Order => 170;

    public string Description => "A calm image or gradient behind the page.";

    public Type ComponentType => typeof(BackgroundSettings);
}

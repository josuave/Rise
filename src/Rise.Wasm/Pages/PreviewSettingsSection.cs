using Rise.Slices.Settings.Abstractions;

namespace Rise.Wasm.Pages;

/// <summary>Shell-owned settings section: the preview-feature manager.</summary>
internal sealed class PreviewSettingsSection : ISettingsSection
{
    public string Title => "Preview features";

    public int Order => 215;

    public string Description => "Try upcoming features before they become the default.";

    public Type ComponentType => typeof(PreviewSettings);
}

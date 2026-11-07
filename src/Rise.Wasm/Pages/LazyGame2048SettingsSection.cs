using Rise.Slices.Settings.Abstractions;

namespace Rise.Wasm.Pages;

/// <summary>
/// Metadata for the 2048 settings section: 2048 is a lazy module,
/// so its section cannot be registered from the slice at startup.
/// The dialog renders it through the lazy section container, which
/// loads the 2048 assembly and then renders the real section.
/// Title, order, and description mirror the slice's section values.
/// </summary>
internal sealed class LazyGame2048SettingsSection : ILazySettingsSection
{
    public string Title => "2048";

    public int Order => 145;

    public string Description => "Best score, and a fresh board.";

    public Type ComponentType => typeof(Rise.Slices.Settings.LazySectionContainer);

    public string AssemblyFile => "Rise.Slices.Game2048.wasm";

    public string ComponentTypeName => "Rise.Slices.Game2048.Game2048Settings";
}

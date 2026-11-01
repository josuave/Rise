using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Settings;

/// <summary>
/// Imperative shell for the Settings slice: it renders the gear
/// button and the Settings dialog (theme section first, then sections
/// contributed by other slices), and calls the theme port. The quick
/// theme toggle lives in its own widget (ThemeToggle). It contains no
/// storage or JS code itself.
/// </summary>
public partial class SettingsGear
{
    [Inject]
    private IThemeStore? ThemeStore { get; set; }

    /// <summary>
    /// Sections contributed by other slices through the <see cref="ISettingsSection"/>
    /// contract. The dialog knows only the contract, never the contributing slice.
    /// </summary>
    [Inject]
    private IEnumerable<ISettingsSection> Sections { get; set; } = Array.Empty<ISettingsSection>();

    private bool IsOpen { get; set; }

    /// <summary>Stored preference: "light", "dark", or "system" (the default).</summary>
    private string StoredTheme { get; set; } = "system";

    protected override async Task OnInitializedAsync()
    {
        string? stored = ThemeStore is null ? null : await ThemeStore.GetStoredThemeAsync();
        StoredTheme = stored is "light" or "dark" or "system" ? stored : "system";

        if (ThemeStore is not null)
        {
            await ThemeStore.ApplyThemeAsync(StoredTheme);
        }
    }

    private async Task SetThemeAsync(string theme)
    {
        StoredTheme = theme;

        if (ThemeStore is not null)
        {
            await ThemeStore.SetStoredThemeAsync(theme);
            await ThemeStore.ApplyThemeAsync(theme);
        }
    }

    private void Open() => IsOpen = true;

    private void Close() => IsOpen = false;
}

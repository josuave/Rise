using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Settings;

/// <summary>
/// Imperative shell for the Settings slice: it renders the theme
/// toggle + gear button cluster and the Settings dialog (theme
/// section first, then sections contributed by other slices), and
/// calls the theme port. It contains no storage or JS code itself.
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

    /// <summary>Effective theme right now, after resolving "system" against the device.</summary>
    private bool EffectiveDark { get; set; }

    /// <summary>Glyph on the quick toggle: sun when dark is showing (tap for light), moon when light is showing.</summary>
    private string ToggleGlyph => EffectiveDark ? "\u2600" : "\u263E";

    private string ToggleLabel => EffectiveDark ? "Switch to light mode" : "Switch to dark mode";

    protected override async Task OnInitializedAsync()
    {
        string? stored = ThemeStore is null ? null : await ThemeStore.GetStoredThemeAsync();
        StoredTheme = stored is "light" or "dark" or "system" ? stored : "system";

        if (ThemeStore is not null)
        {
            await ThemeStore.ApplyThemeAsync(StoredTheme);
        }

        EffectiveDark = await ComputeEffectiveDarkAsync();
    }

    private async Task<bool> ComputeEffectiveDarkAsync() =>
        StoredTheme switch
        {
            "dark" => true,
            "light" => false,
            _ => ThemeStore is not null && await ThemeStore.PrefersDarkAsync(),
        };

    private async Task SetThemeAsync(string theme)
    {
        StoredTheme = theme;

        if (ThemeStore is not null)
        {
            await ThemeStore.SetStoredThemeAsync(theme);
            await ThemeStore.ApplyThemeAsync(theme);
        }

        EffectiveDark = await ComputeEffectiveDarkAsync();
    }

    /// <summary>
    /// Quick toggle next to the gear: flips light/dark directly. From
    /// "system" it picks the explicit opposite of whatever the device
    /// is currently showing, so the toggle always does the obvious thing.
    /// </summary>
    private async Task ToggleThemeAsync() =>
        await SetThemeAsync(EffectiveDark ? "light" : "dark");

    private void Open() => IsOpen = true;

    private void Close() => IsOpen = false;
}

using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Settings;

/// <summary>
/// The theme widget: the sun/moon quick toggle. It owns applying the
/// stored theme on startup and flipping between explicit light/dark.
/// The Settings dialog keeps its own Light/Dark/System picker. A
/// custom theme counts as "system" for the toggle's effective-dark
/// resolution (its dark half shows when the device prefers dark), and
/// toggling away from one switches to an explicit built-in theme.
/// </summary>
public partial class ThemeToggle
{
    [Inject]
    private IThemeStore? ThemeStore { get; set; }

    [Inject]
    private CustomThemeService? CustomThemeService { get; set; }

    /// <summary>Stored preference: "light", "dark", "system" (the default), or "custom:&lt;id&gt;".</summary>
    private string StoredTheme { get; set; } = "system";

    /// <summary>Effective theme right now, after resolving "system" against the device.</summary>
    private bool EffectiveDark { get; set; }

    /// <summary>Glyph on the quick toggle: sun when dark is showing (tap for light), moon when light is showing.</summary>
    private string ToggleGlyph => EffectiveDark ? "\u2600" : "\u263E";

    private string ToggleLabel => EffectiveDark ? "Switch to light mode" : "Switch to dark mode";

    protected override async Task OnInitializedAsync()
    {
        string? stored = ThemeStore is null ? null : await ThemeStore.GetStoredThemeAsync();
        if (stored is "light" or "dark" or "system")
        {
            StoredTheme = stored;
            if (ThemeStore is not null)
            {
                await ThemeStore.ApplyThemeAsync(StoredTheme);
            }
        }
        else if (stored is not null
            && CustomThemeService is not null
            && global::Rise.Application.Services.CustomThemeService.TryParseChoice(stored, out string id)
            && (await CustomThemeService.LoadAsync()).FirstOrDefault(t => t.Id == id) is CustomTheme active)
        {
            StoredTheme = stored;
            if (ThemeStore is not null)
            {
                await ThemeStore.ApplyCustomThemeAsync(await CustomThemeService.ResolveHalfAsync(active));
            }
        }
        else
        {
            // Unknown or orphaned choice (a deleted custom theme):
            // repair to the default rather than leaving a dead value.
            StoredTheme = "system";
            if (ThemeStore is not null)
            {
                await ThemeStore.SetStoredThemeAsync("system");
                await ThemeStore.ApplyThemeAsync("system");
            }
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
    /// Quick toggle: flips light/dark directly. From "system" it picks
    /// the explicit opposite of whatever the device is currently
    /// showing, so the toggle always does the obvious thing.
    /// </summary>
    private async Task ToggleThemeAsync() =>
        await SetThemeAsync(EffectiveDark ? "light" : "dark");
}

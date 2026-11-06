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
/// toggling away from one switches to an explicit built-in theme. A
/// pack theme resolves to its own declared mode instead, and
/// toggling away from one switches to a built-in the same way.
/// </summary>
public partial class ThemeToggle : IDisposable
{
    [Inject]
    private IThemeStore? ThemeStore { get; set; }

    [Inject]
    private CustomThemeService? CustomThemeService { get; set; }

    /// <summary>Null in older tests: no pack themes then, the toggle treats every choice as built-in or custom.</summary>
    [Inject]
    private IOmarchyThemeProvider? ThemePackProvider { get; set; }

    /// <summary>Null in older tests: no filtering then, pack themes always resolve.</summary>
    [Inject]
    private ModuleService? ModuleService { get; set; }

    /// <summary>Null in older tests: no filtering then, pack themes always resolve.</summary>
    [Inject]
    private IModuleCatalog? ModuleCatalog { get; set; }

    /// <summary>Stored preference: "light", "dark", "system" (the default), "custom:&lt;id&gt;", or "pack:omarchy:&lt;slug&gt;".</summary>
    private string StoredTheme { get; set; } = "system";

    /// <summary>The pack theme active right now, if the stored choice is one — its declared mode drives the glyph.</summary>
    private OmarchyTheme? ActivePack { get; set; }

    /// <summary>Effective theme right now, after resolving "system" against the device.</summary>
    private bool EffectiveDark { get; set; }

    /// <summary>Glyph on the quick toggle: sun when dark is showing (tap for light), moon when light is showing.</summary>
    private string ToggleGlyph => EffectiveDark ? "\u2600" : "\u263E";

    private string ToggleLabel => EffectiveDark ? "Switch to light mode" : "Switch to dark mode";

    protected override async Task OnInitializedAsync()
    {
        ThemeStore?.Changed += OnThemeChanged;

        await RefreshThemeAsync();
    }

    /// <summary>
    /// Re-reads the stored choice and applies it, so the toggle
    /// follows picks made in the Settings dialog as well as its own
    /// flips. Called on startup and whenever the stored theme
    /// changes.
    /// </summary>
    private async Task RefreshThemeAsync()
    {
        HashSet<string> disabled = ModuleCatalog is null || ModuleService is null
            ? new HashSet<string>()
            : await ModuleService.DisabledIdsAsync(ModuleCatalog.Modules);

        string? stored = ThemeStore is null ? null : await ThemeStore.GetStoredThemeAsync();
        if (stored is "light" or "dark" or "system")
        {
            StoredTheme = stored;
            ActivePack = null;
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
            ActivePack = null;
            if (ThemeStore is not null)
            {
                await ThemeStore.ApplyCustomThemeAsync(await CustomThemeService.ResolveHalfAsync(active));
            }
        }
        else if (stored is not null
            && !disabled.Contains(OmarchyTheme.ModuleId)
            && OmarchyTheme.TryParseChoice(stored, out string slug)
            && ThemePackProvider?.Themes.FirstOrDefault(t => t.Id == slug) is OmarchyTheme pack)
        {
            // A pack theme is one palette, not a pair: it applies
            // as-is, whatever the device prefers.
            StoredTheme = stored;
            ActivePack = pack;
            if (ThemeStore is not null)
            {
                await ThemeStore.ApplyCustomThemeAsync(pack.Palette);
            }
        }
        else
        {
            // Unknown or orphaned choice (a deleted custom theme, or
            // a pack theme whose module is switched off): repair to
            // the default rather than leaving a dead value.
            StoredTheme = "system";
            ActivePack = null;
            if (ThemeStore is not null)
            {
                await ThemeStore.SetStoredThemeAsync("system");
                await ThemeStore.ApplyThemeAsync("system");
            }
        }

        EffectiveDark = await ComputeEffectiveDarkAsync();
    }

    private async void OnThemeChanged(object? sender, EventArgs args)
    {
        await RefreshThemeAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> ComputeEffectiveDarkAsync() =>
        ActivePack is not null
            ? ActivePack.IsDark
            : StoredTheme switch
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && ThemeStore is not null)
        {
            ThemeStore.Changed -= OnThemeChanged;
        }
    }
}

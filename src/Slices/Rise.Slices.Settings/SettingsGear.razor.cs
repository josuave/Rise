using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Settings;

/// <summary>
/// Imperative shell for the Settings slice: it renders the gear
/// button and the Settings dialog (theme section first, then sections
/// contributed by other slices), and calls the theme port. The quick
/// theme toggle lives in its own widget (ThemeToggle). It contains no
/// storage or JS code itself. Custom themes appear in the theme
/// picker alongside the built-ins; the theme editor edits one.
/// </summary>
public partial class SettingsGear
{
    [Inject]
    private IThemeStore? ThemeStore { get; set; }

    [Inject]
    private CustomThemeService? CustomThemeService { get; set; }

    /// <summary>
    /// Sections contributed by other slices through the <see cref="ISettingsSection"/>
    /// contract. The dialog knows only the contract, never the contributing slice.
    /// </summary>
    [Inject]
    private IEnumerable<ISettingsSection> Sections { get; set; } = Array.Empty<ISettingsSection>();

    private bool IsOpen { get; set; }

    /// <summary>Stored preference: "light", "dark", "system" (the default), or "custom:&lt;id&gt;".</summary>
    private string StoredTheme { get; set; } = "system";

    private IReadOnlyList<CustomTheme> CustomThemes { get; set; } = Array.Empty<CustomTheme>();

    private CustomTheme? EditingTheme { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (CustomThemeService is not null)
        {
            CustomThemes = await CustomThemeService.LoadAsync();
        }

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
            && CustomThemes.FirstOrDefault(t => t.Id == id) is CustomTheme active)
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

    private async Task SetCustomThemeAsync(CustomTheme theme)
    {
        if (ThemeStore is null || CustomThemeService is null)
        {
            return;
        }

        StoredTheme = global::Rise.Application.Services.CustomThemeService.ChoiceFor(theme.Id);
        await CustomThemeService.SetActiveAsync(theme.Id);
        await ThemeStore.ApplyCustomThemeAsync(await CustomThemeService.ResolveHalfAsync(theme));
    }

    private bool IsActive(CustomTheme theme) =>
        StoredTheme == global::Rise.Application.Services.CustomThemeService.ChoiceFor(theme.Id);

    private async Task CreateThemeAsync()
    {
        if (CustomThemeService is null)
        {
            return;
        }

        CustomTheme created = await CustomThemeService.CreateAsync("New theme");
        CustomThemes = await CustomThemeService.LoadAsync();
        EditingTheme = created;
    }

    private async Task SaveThemeAsync(CustomTheme theme)
    {
        if (CustomThemeService is null)
        {
            return;
        }

        CustomThemes = await CustomThemeService.UpdateAsync(theme);
        EditingTheme = null;

        // Editing the active theme re-applies it, so saved edits show at once.
        if (IsActive(theme) && ThemeStore is not null)
        {
            await ThemeStore.ApplyCustomThemeAsync(await CustomThemeService.ResolveHalfAsync(theme));
        }
    }

    private async Task DeleteThemeAsync(string id)
    {
        if (CustomThemeService is null)
        {
            return;
        }

        CustomThemes = await CustomThemeService.DeleteAsync(id);
        if (EditingTheme?.Id == id)
        {
            EditingTheme = null;
        }

        // The service resets the choice to "system" when the active
        // theme is deleted; re-read so the picker reflects it.
        StoredTheme = ThemeStore is null ? StoredTheme : await ThemeStore.GetStoredThemeAsync() ?? "system";
    }

    private void CancelEdit() => EditingTheme = null;

    private void Open() => IsOpen = true;

    private void Close() => IsOpen = false;
}

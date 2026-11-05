using System.IO;
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
/// Sections belonging to a switched-off module are hidden: a module's
/// sections live in the module's assembly, so the dialog filters by
/// assembly name.
/// </summary>
public partial class SettingsGear : IDisposable
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

    /// <summary>Null in older tests: no filtering then, every section shows.</summary>
    [Inject]
    private ModuleService? ModuleService { get; set; }

    /// <summary>Null in older tests: no filtering then, every section shows.</summary>
    [Inject]
    private IModuleCatalog? ModuleCatalog { get; set; }

    /// <summary>Ids of modules currently switched off.</summary>
    private HashSet<string> DisabledModules { get; set; } = new();

    private bool IsOpen { get; set; }

    /// <summary>Stored preference: "light", "dark", "system" (the default), or "custom:&lt;id&gt;".</summary>
    private string StoredTheme { get; set; } = "system";

    private IReadOnlyList<CustomTheme> CustomThemes { get; set; } = Array.Empty<CustomTheme>();

    private CustomTheme? EditingTheme { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshDisabledAsync();
        ModuleService?.Changed += OnModulesChanged;

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

    private async Task RefreshDisabledAsync() =>
        DisabledModules = ModuleCatalog is null || ModuleService is null
            ? new HashSet<string>()
            : await ModuleService.DisabledIdsAsync(ModuleCatalog.Modules);

    private async void OnModulesChanged(object? sender, EventArgs args)
    {
        await RefreshDisabledAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Contributed sections ordered by <see cref="ISettingsSection.Order"/>,
    /// skipping sections whose component lives in a switched-off
    /// module's assembly. Sections from unclaimed assemblies always show.
    /// A lazy section's component lives in the container's assembly, so
    /// it filters by its own assembly file name instead.
    /// </summary>
    private IEnumerable<ISettingsSection> VisibleSections()
    {
        IOrderedEnumerable<ISettingsSection> ordered = Sections.OrderBy(s => s.Order);
        if (ModuleCatalog is null || ModuleService is null || DisabledModules.Count == 0)
        {
            return ordered;
        }

        var disabledAssemblies = ModuleCatalog.Modules
            .Where(m => DisabledModules.Contains(m.Id))
            .Select(m => m.AssemblyName)
            .ToHashSet();
        return ordered.Where(s => SectionAssemblyName(s) is not string assemblyName
            || !disabledAssemblies.Contains(assemblyName));
    }

    private static string? SectionAssemblyName(ISettingsSection section) =>
        section is ILazySettingsSection lazy
            ? Path.GetFileNameWithoutExtension(lazy.AssemblyFile)
            : section.ComponentType.Assembly.GetName().Name;

    private static Dictionary<string, object?> LazySectionParameters(ILazySettingsSection lazy) =>
        new() { ["Section"] = lazy };

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && ModuleService is not null)
        {
            ModuleService.Changed -= OnModulesChanged;
        }
    }
}

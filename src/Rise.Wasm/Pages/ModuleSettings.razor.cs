using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Wasm.Pages;

/// <summary>
/// The Modules settings section: one row per catalog module with an
/// enable/disable button. Disabling restarts the page so the module
/// is truly gone; enabling is live.
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class ModuleSettings : IDisposable
{
    [Inject]
    private IModuleCatalog Catalog { get; set; } = null!;

    [Inject]
    private ModuleService ModuleService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private HashSet<string> DisabledModules { get; set; } = new();

    private bool Loaded { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshDisabledAsync();
        Loaded = true;
        ModuleService.Changed += OnModulesChanged;
    }

    private async void OnModulesChanged(object? sender, EventArgs args)
    {
        await RefreshDisabledAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshDisabledAsync() =>
        DisabledModules = await ModuleService.DisabledIdsAsync(Catalog.Modules);

    private bool IsEnabled(ModuleInfo module) => !DisabledModules.Contains(module.Id);

    private async Task EnableAsync(ModuleInfo module) =>
        await ModuleService.SetEnabledAsync(module.Id, true);

    private async Task DisableAsync(ModuleInfo module)
    {
        await ModuleService.SetEnabledAsync(module.Id, false);
        // Restart at the app's own base, not the site root: the app
        // is served under a subpath (/Rise/), so NavigateTo("/")
        // would leave the app entirely instead of restarting it.
        Navigation.NavigateTo(Navigation.BaseUri, forceLoad: true);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ModuleService.Changed -= OnModulesChanged;
        }
    }
}

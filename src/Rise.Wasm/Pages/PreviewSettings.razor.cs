using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Wasm.Pages;

/// <summary>
/// The Preview features settings section: one row per registered
/// preview feature with an opt-in checkbox. Toggling is live — the
/// page re-renders through the service's Changed event, no restart.
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class PreviewSettings : IDisposable
{
    [Inject]
    private PreviewFeatureService PreviewFeatureService { get; set; } = null!;

    private IReadOnlyList<(PreviewFeature Feature, bool Enabled)> Features { get; set; } =
        Array.Empty<(PreviewFeature, bool)>();

    private bool Loaded { get; set; }

    private string? Status { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
        Loaded = true;
        PreviewFeatureService.Changed += OnPreviewFeaturesChanged;
    }

    private async void OnPreviewFeaturesChanged(object? sender, EventArgs args)
    {
        await RefreshAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshAsync() => Features = await PreviewFeatureService.GetAllAsync();

    private async Task ToggleAsync(PreviewFeature feature, bool enabled)
    {
        await PreviewFeatureService.SetEnabledAsync(feature.Id, enabled);
        Status = enabled ? $"{feature.Title} on." : $"{feature.Title} off.";
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
            PreviewFeatureService.Changed -= OnPreviewFeaturesChanged;
        }
    }
}

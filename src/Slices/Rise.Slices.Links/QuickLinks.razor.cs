using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// The Links widget: tiles only. Editing lives in the Settings
/// dialog. Tiles stay fresh because the component reloads whenever
/// the link service saves.
/// </summary>
public partial class QuickLinks : IDisposable
{
    [Inject]
    private StartPageService? StartPageService { get; set; }

    private StartPage Page { get; set; } = StartPage.Default();

    protected override async Task OnInitializedAsync()
    {
        if (StartPageService is not null)
        {
            Page = await StartPageService.LoadAsync();
            StartPageService.Changed += OnPageChanged;
        }
    }

    private async void OnPageChanged(object? sender, EventArgs args)
    {
        if (StartPageService is null)
        {
            return;
        }

        Page = await StartPageService.LoadAsync();
        await InvokeAsync(StateHasChanged);
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
            StartPageService?.Changed -= OnPageChanged;
        }
    }
}

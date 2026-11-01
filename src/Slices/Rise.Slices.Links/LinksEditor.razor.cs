using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// The Link editor widget: add a link, remove links, done hides the
/// widget again. It shares the page with the Links widget through the
/// link service, and reloads whenever that service saves elsewhere.
/// </summary>
public partial class LinksEditor : IDisposable
{
    [Inject]
    private StartPageService? StartPageService { get; set; }

    [Inject]
    private LayoutService? LayoutService { get; set; }

    private StartPage Page { get; set; } = StartPage.Default();

    private string NewName { get; set; } = string.Empty;
    private string NewUrl { get; set; } = string.Empty;
    private string? FormError { get; set; }

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

    private async Task AddAsync()
    {
        if (StartPageService is null)
        {
            return;
        }

        Result<StartPage> result = await StartPageService.AddLinkAsync(Page, NewName, NewUrl);
        if (result.IsSuccess && result.Value is not null)
        {
            Page = result.Value;
            NewName = string.Empty;
            NewUrl = string.Empty;
            FormError = null;
        }
        else
        {
            FormError = result.Error;
        }
    }

    private async Task RemoveAsync(Link link)
    {
        if (StartPageService is null)
        {
            return;
        }

        Page = await StartPageService.RemoveLinkAsync(Page, link.Id);
    }

    private async Task DoneAsync()
    {
        if (LayoutService is not null)
        {
            await LayoutService.HideWidgetAsync(LinkEditorWidgetDefinition.WidgetId);
        }
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

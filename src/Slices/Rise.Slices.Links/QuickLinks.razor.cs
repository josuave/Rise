using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// Imperative shell for the Links slice: it renders, collects input, and calls the
/// application service. All link rules (validation, duplicates) live in the pure core;
/// this component knows nothing about localStorage, JS interop, or JSON.
/// </summary>
public partial class QuickLinks
{
    [Inject]
    private StartPageService? StartPageService { get; set; }

    private StartPage Page { get; set; } = StartPage.Default();

    private bool Editing { get; set; }
    private string NewName { get; set; } = string.Empty;
    private string NewUrl { get; set; } = string.Empty;
    private string? FormError { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (StartPageService is not null)
        {
            Page = await StartPageService.LoadAsync();
        }
    }

    private async Task AddAsync()
    {
        if (StartPageService is null)
        {
            return;
        }

        var result = await StartPageService.AddLinkAsync(Page, NewName, NewUrl);
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
}

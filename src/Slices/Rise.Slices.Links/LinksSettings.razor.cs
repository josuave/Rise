using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// The Links slice's injected Settings section (see <see cref="LinksSettingsSection"/>).
/// Behavior lives here, in the slice, not in the Settings dialog: it loads the
/// page through the core service, adds and removes links, and resets to defaults.
/// This is the links' only editor.
/// </summary>
public partial class LinksSettings
{
    [Inject]
    private StartPageService? StartPageService { get; set; }

    private StartPage Page { get; set; } = StartPage.Default();
    private string? Message { get; set; }

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

        Result<StartPage> result = await StartPageService.AddLinkAsync(Page, NewName, NewUrl);
        switch (result)
        {
            case Ok<StartPage> ok:
                Page = ok.Value;
                NewName = string.Empty;
                NewUrl = string.Empty;
                FormError = null;
                break;
            case Error error:
                FormError = error.Message;
                break;
            case null:
                FormError = null;
                break;
        }
    }

    private async Task RemoveAsync(Link link)
    {
        if (StartPageService is null)
        {
            return;
        }

        Page = await StartPageService.RemoveLinkAsync(Page, link.Id);
        Message = null;
    }

    private async Task ResetAsync()
    {
        if (StartPageService is null)
        {
            return;
        }

        await StartPageService.ResetToDefaultsAsync();
        Page = await StartPageService.LoadAsync();
        Message = "Links reset to defaults.";
    }
}

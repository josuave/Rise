using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>Outbound adapter: opens a URL in a new browser tab.</summary>
public sealed class BrowserSearchLauncher(IJSRuntime js) : ISearchLauncher
{
    public async Task LaunchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("open", cancellationToken, url.ToString(), "_blank");
        }
        catch (JSException)
        {
            // Opening the tab failed (e.g. popup blocked); there is nothing useful to show for a start-page search, so swallow it.
        }
    }
}

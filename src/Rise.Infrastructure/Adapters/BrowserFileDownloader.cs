using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: saves a file to the user's machine through the
/// shell's riseDownload helper (blob + anchor click). A failed
/// download leaves the page working; the caller reports it.
/// </summary>
public sealed class BrowserFileDownloader(IJSRuntime js) : IFileDownloader
{
    public async Task DownloadAsync(string filename, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseDownload.save", cancellationToken, filename, content);
        }
        catch (JSException)
        {
            // Download unavailable: the caller reports the failure.
            throw new InvalidOperationException("The download couldn't be started.");
        }
    }
}

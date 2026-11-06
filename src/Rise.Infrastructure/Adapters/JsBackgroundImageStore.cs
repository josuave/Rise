using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: uploaded background bytes through the
/// riseBackgroundImage JS helper (IndexedDB, keyed by hash). A
/// failed save reports false rather than throwing, so the service
/// can say the image did not store; a failed load reads as missing
/// bytes, which is exactly the re-upload prompt — never a crash.
/// Delete and URL clearing are best-effort: a failure there leaves
/// the page working.
/// </summary>
public sealed class JsBackgroundImageStore(IJSRuntime js) : IBackgroundImageStore
{
    public async Task<bool> SaveAsync(string hash, byte[] bytes, string mimeType, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseBackgroundImage.save", cancellationToken, hash, bytes, mimeType);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async Task<string?> GetUrlAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<string?>("riseBackgroundImage.load", cancellationToken, hash);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseBackgroundImage.remove", cancellationToken, hash);
        }
        catch (JSException)
        {
            // Best effort: an orphaned blob is inert until replaced.
        }
    }

    public async Task ClearUrlAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseBackgroundImage.clearUrl", cancellationToken);
        }
        catch (JSException)
        {
            // Best effort: the URL dies with the page.
        }
    }
}

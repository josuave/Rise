using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: diagnostics/maintenance via the <c>window.riseDiagnostics</c>
/// script block in the shell's index.html. All calls swallow JS failures so a
/// broken browser feature reports as "unavailable" instead of breaking the page.
/// </summary>
public sealed class DiagnosticsProbe(IJSRuntime js) : IDiagnosticsProbe
{
    private const string StorageKey = "rise.quicklinks.v1";

    public async Task<string?> GetRawStoredLinksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<string?>("riseDiagnostics.getRaw", cancellationToken, StorageKey);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<string?> GetUserAgentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<string?>("riseDiagnostics.getUserAgent", cancellationToken);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<string> GetServiceWorkerStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<string>("riseDiagnostics.getServiceWorkers", cancellationToken);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return "unavailable";
        }
    }

    public async Task<bool> CopyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>("riseDiagnostics.copyText", cancellationToken, text);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<bool> ClearStoredLinksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>("riseDiagnostics.clearKey", cancellationToken, StorageKey);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<bool> UnregisterServiceWorkersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>("riseDiagnostics.unregisterServiceWorkers", cancellationToken);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return false;
        }
    }
}

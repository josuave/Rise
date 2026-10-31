namespace Rise.Application.Ports;

/// <summary>
/// Outbound port: read-only diagnostics and maintenance actions the browser can do
/// (raw storage, user agent, clipboard, service workers). The core knows nothing
/// about localStorage or JS; the adapter implements these. Every method degrades
/// gracefully (null/false/"unavailable") when the browser cannot do it, so the
/// diagnostics dialog never breaks the page it is diagnosing.
/// </summary>
public interface IDiagnosticsProbe
{
    /// <summary>Raw value of the saved-links storage key, or null when nothing is saved.</summary>
    Task<string?> GetRawStoredLinksAsync(CancellationToken cancellationToken = default);

    /// <summary>Browser user-agent string, or null when unavailable.</summary>
    Task<string?> GetUserAgentAsync(CancellationToken cancellationToken = default);

    /// <summary>Service-worker state summary, e.g. "none registered" or scopes of registered workers.</summary>
    Task<string> GetServiceWorkerStateAsync(CancellationToken cancellationToken = default);

    /// <summary>Copies text to the clipboard; false when the browser refuses.</summary>
    Task<bool> CopyTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Deletes the saved-links storage key; true when the key is gone afterwards.</summary>
    Task<bool> ClearStoredLinksAsync(CancellationToken cancellationToken = default);

    /// <summary>Unregisters all service workers for this origin; false when none could be unregistered.</summary>
    Task<bool> UnregisterServiceWorkersAsync(CancellationToken cancellationToken = default);
}

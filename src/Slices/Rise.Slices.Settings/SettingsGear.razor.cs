using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Settings.Abstractions;
using Rise.Domain;

namespace Rise.Slices.Settings;

/// <summary>
/// Imperative shell for the Settings slice: it renders the gear button and the
/// diagnostics/maintenance dialog, and calls application services and the
/// diagnostics port. It contains no storage, JS, or service-worker code itself.
/// </summary>
public partial class SettingsGear
{
    [Inject]
    private StartPageService? StartPageService { get; set; }

    [Inject]
    private IDiagnosticsProbe? DiagnosticsProbe { get; set; }

    [Inject]
    private IClock? Clock { get; set; }

    /// <summary>
    /// Sections contributed by other slices through the <see cref="ISettingsSection"/>
    /// contract. The dialog knows only the contract, never the contributing slice.
    /// </summary>
    [Inject]
    private IEnumerable<ISettingsSection> Sections { get; set; } = Array.Empty<ISettingsSection>();

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private bool IsOpen { get; set; }
    private bool Loading { get; set; }
    private int ClickCount { get; set; }
    private string? CheckedAt { get; set; }
    private string? PageUri { get; set; }
    private string? UserAgent { get; set; }
    private int LinkCount { get; set; }
    private string LinkNames { get; set; } = string.Empty;
    private string LinksSource { get; set; } = string.Empty;
    private string? RawStoredLinks { get; set; }
    private string ServiceWorkerState { get; set; } = "unavailable";
    private string? CopyMessage { get; set; }
    private string? ActionMessage { get; set; }

    private async Task OpenAsync()
    {
        IsOpen = true;
        Loading = true;
        ClickCount = 0;
        CopyMessage = null;
        ActionMessage = null;
        StateHasChanged();

        CheckedAt = Clock is null ? "unavailable" : Clock.Now.ToString("yyyy-MM-dd HH:mm:ss");
        PageUri = Nav.BaseUri;

        var page = StartPageService is null ? StartPage.Default() : await StartPageService.LoadAsync();
        LinkCount = page.Links.Count;
        LinkNames = string.Join(", ", page.Links.Select(l => l.Name));

        RawStoredLinks = DiagnosticsProbe is null ? null : await DiagnosticsProbe.GetRawStoredLinksAsync();
        LinksSource = RawStoredLinks is null
            ? "defaults (nothing saved yet)"
            : "saved links";

        UserAgent = DiagnosticsProbe is null ? null : await DiagnosticsProbe.GetUserAgentAsync();
        ServiceWorkerState = DiagnosticsProbe is null
            ? "unavailable"
            : await DiagnosticsProbe.GetServiceWorkerStateAsync();

        Loading = false;
    }

    private void Close() => IsOpen = false;

    private string DiagnosticsText() =>
        "Rise diagnostics\n" +
        $"Checked at: {CheckedAt}\n" +
        $"Page: {PageUri}\n" +
        $"Browser: {UserAgent ?? "unavailable"}\n" +
        $"Links shown ({LinkCount}): {LinkNames}\n" +
        $"Links source: {LinksSource}\n" +
        $"Saved links (raw): {RawStoredLinks ?? "(nothing saved)"}\n" +
        $"Service workers: {ServiceWorkerState}\n" +
        $"Click test taps: {ClickCount}\n";

    private async Task CopyAsync()
    {
        if (DiagnosticsProbe is null)
        {
            CopyMessage = "Copy is unavailable here.";
            return;
        }

        CopyMessage = await DiagnosticsProbe.CopyTextAsync(DiagnosticsText())
            ? "Copied. Paste it into chat with Muse."
            : "Copy was refused by the browser. Read the values above instead.";
    }

    private async Task ResetLinksAsync()
    {
        if (StartPageService is null)
        {
            ActionMessage = "Links service is unavailable, so nothing was changed.";
            return;
        }

        await StartPageService.ResetToDefaultsAsync();
        Reload();
    }

    private async Task ClearSavedLinksAsync()
    {
        if (DiagnosticsProbe is null || !await DiagnosticsProbe.ClearStoredLinksAsync())
        {
            ActionMessage = "Could not clear saved links.";
            return;
        }

        Reload();
    }

    private async Task UnregisterServiceWorkersAsync()
    {
        if (DiagnosticsProbe is null || !await DiagnosticsProbe.UnregisterServiceWorkersAsync())
        {
            ActionMessage = "No service workers were unregistered.";
            return;
        }

        Reload();
    }

    private void Reload() => Nav.NavigateTo(Nav.Uri, forceLoad: true);
}

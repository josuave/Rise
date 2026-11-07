using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Wasm.Pages;

/// <summary>
/// The Home screen icon settings section: one toggle for whether the
/// install-time manifest follows the theme, plus a preview of the icon
/// a fresh install would use. The toggle lives in localStorage under
/// "rise.home-icon.v1" ("follow" or "static"; follow when nothing is
/// stored). The manifest swap itself is the shell's window.riseHomeIcon
/// script in index.html, which this component re-applies after every
/// toggle change and every theme change (via <see cref="IThemeStore.Changed"/>:
/// every theme pick — built-in, custom, or pack — persists through the
/// theme store, so one subscription covers them all). The honest limit
/// is stated in the UI: post-install theme changes do not update the
/// icon — Chrome re-reads only the static manifest for an installed app.
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class HomeScreenIconSettings : IDisposable
{
    private const string StorageKey = "rise.home-icon.v1";

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    private IThemeStore? ThemeStore { get; set; }

    private bool Loaded { get; set; }

    private bool FollowsTheme { get; set; } = true;

    private string? IconHref { get; set; }

    private string? Status { get; set; }

    private string PreviewCaption => FollowsTheme
        ? "Following your theme."
        : "Using the default Rise icon.";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            string? stored = await JSRuntime.InvokeAsync<string?>("riseStorage.get", CancellationToken.None, StorageKey);
            FollowsTheme = stored is not "static";
        }
        catch (JSException)
        {
            FollowsTheme = true;
        }

        ThemeStore?.Changed += OnThemeChanged;
        await ApplyAndRefreshAsync();
        Loaded = true;
    }

    private async void OnThemeChanged(object? sender, EventArgs args)
    {
        await ApplyAndRefreshAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ApplyAndRefreshAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("riseHomeIcon.apply", CancellationToken.None);
            IconHref = await JSRuntime.InvokeAsync<string?>("riseHomeIcon.iconHref", CancellationToken.None);
        }
        catch (JSException)
        {
            // The icon is a nicety; a failed swap must not break Settings.
        }
    }

    private async Task ToggleAsync(bool follow)
    {
        FollowsTheme = follow;
        try
        {
            await JSRuntime.InvokeVoidAsync("riseStorage.set", CancellationToken.None, StorageKey, follow ? "follow" : "static");
        }
        catch (JSException)
        {
            // Keep the toggle state even when the save fails.
        }

        await ApplyAndRefreshAsync();
        Status = follow
            ? "Home screen icon will follow your theme when you install."
            : "Home screen icon will use the default Rise icon when you install.";
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && ThemeStore is not null)
        {
            ThemeStore.Changed -= OnThemeChanged;
        }
    }
}

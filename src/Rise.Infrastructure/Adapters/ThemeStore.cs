using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: theme preference via the <c>window.riseTheme</c>
/// script block in the shell's index.html (localStorage + the
/// document's data-theme attribute + matchMedia). The core knows
/// nothing about localStorage or JS; the adapter implements these.
/// </summary>
public sealed class ThemeStore(IJSRuntime js) : IThemeStore
{
    private const string StorageKey = "rise.theme.v1";

    public async Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<string?>("riseTheme.getStored", cancellationToken, StorageKey);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseTheme.setStored", cancellationToken, StorageKey, theme);
        }
        catch (JSException)
        {
            // Theme preference is a nicety; a failed save must not break the page.
        }
    }

    public async Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseTheme.apply", cancellationToken, theme);
        }
        catch (JSException)
        {
            // Same: a failed apply must not break the page.
        }
    }

    public async Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>("riseTheme.prefersDark", cancellationToken);
        }
        catch (JSException)
        {
            return false;
        }
    }
}

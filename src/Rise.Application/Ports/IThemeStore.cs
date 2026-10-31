namespace Rise.Application.Ports;

/// <summary>
/// Outbound port: the user's theme preference. Stored values are
/// "light", "dark", or "system" (follow the device). "system" is the
/// default when nothing is stored. Applying a theme sets the page's
/// data-theme attribute ("light"/"dark") or removes it ("system", so
/// the CSS system media query decides). Every method degrades
/// gracefully when the browser cannot do it, so theme code never
/// breaks the page it is styling.
/// </summary>
public interface IThemeStore
{
    /// <summary>Stored theme preference, or null when nothing is stored yet.</summary>
    Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a theme preference ("light", "dark", or "system").</summary>
    Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default);

    /// <summary>Applies a theme to the page (data-theme attribute, or removed for "system").</summary>
    Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default);

    /// <summary>Whether the device currently prefers a dark theme; false when unavailable.</summary>
    Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default);
}

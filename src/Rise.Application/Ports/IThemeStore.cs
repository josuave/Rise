using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>
/// Outbound port: the user's theme preference. Stored values are
/// "light", "dark", "system" (follow the device), or "custom:&lt;id&gt;"
/// for a user-made theme. "system" is the default when nothing is
/// stored. Applying a built-in theme sets the page's data-theme
/// attribute ("light"/"dark") or removes it ("system", so the CSS
/// system media query decides); applying a custom palette sets the
/// calm CSS variables inline. Every method degrades gracefully when
/// the browser cannot do it, so theme code never breaks the page it
/// is styling.
/// </summary>
public interface IThemeStore
{
    /// <summary>Stored theme preference, or null when nothing is stored yet.</summary>
    Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a theme preference ("light", "dark", "system", or "custom:&lt;id&gt;").</summary>
    Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default);

    /// <summary>Applies a theme to the page (data-theme attribute, or removed for "system"). Clears any custom palette.</summary>
    Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default);

    /// <summary>Applies one half of a custom theme as inline CSS variables. Clears the data-theme attribute.</summary>
    Task ApplyCustomThemeAsync(ThemePalette palette, CancellationToken cancellationToken = default);

    /// <summary>Whether the device currently prefers a dark theme; false when unavailable.</summary>
    Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default);
}

using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for user-made themes: load the stored set
/// (sanitized: blank ids dropped, blank names become "Untitled"),
/// create a theme as a copy of the built-in palettes, update, delete,
/// and activate. A theme choice is stored as "custom:&lt;id&gt;" in
/// the same preference the built-ins use; deleting the active theme
/// falls back to "system". The service never touches JS itself —
/// applying a palette goes through the theme port.
/// </summary>
public sealed class CustomThemeService(ICustomThemeStore store, IThemeStore themeStore)
{
    public const string CustomPrefix = "custom:";

    public static string ChoiceFor(string id) => $"{CustomPrefix}{id}";

    public static bool TryParseChoice(string? choice, out string id)
    {
        id = string.Empty;
        if (choice is null || !choice.StartsWith(CustomPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        id = choice[CustomPrefix.Length..];
        return id.Length != 0;
    }

    public async Task<IReadOnlyList<CustomTheme>> LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CustomTheme>? stored = await store.LoadAsync(cancellationToken);
        if (stored is null)
        {
            return Array.Empty<CustomTheme>();
        }

        return stored
            .Where(theme => !string.IsNullOrWhiteSpace(theme.Id))
            .GroupBy(theme => theme.Id)
            .Select(group => group.First())
            .Select(theme => string.IsNullOrWhiteSpace(theme.Name)
                ? new CustomTheme(theme.Id, "Untitled", theme.Light, theme.Dark)
                : theme)
            .ToList();
    }

    /// <summary>Creates a theme as a copy of the built-in palettes, with a unique name, and saves it. Not applied.</summary>
    public async Task<CustomTheme> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CustomTheme> themes = await LoadAsync(cancellationToken);
        string trimmed = name.Trim();
        string unique = UniqueName(trimmed.Length == 0 ? "New theme" : trimmed, themes.Select(t => t.Name));
        var created = new CustomTheme(
            Guid.NewGuid().ToString(),
            unique,
            ThemePalette.DefaultLight,
            ThemePalette.DefaultDark);
        await store.SaveAsync(themes.Append(created).ToList(), cancellationToken);
        return created;
    }

    public async Task<IReadOnlyList<CustomTheme>> UpdateAsync(CustomTheme theme, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CustomTheme> themes = await LoadAsync(cancellationToken);
        var next = themes
            .Select(t => t.Id == theme.Id ? theme : t)
            .ToList();
        await store.SaveAsync(next, cancellationToken);
        return next;
    }

    /// <summary>
    /// Deletes a theme. If it was active, the choice falls back to
    /// "system" (persisted and applied). Returns the remaining themes.
    /// </summary>
    public async Task<IReadOnlyList<CustomTheme>> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CustomTheme> themes = await LoadAsync(cancellationToken);
        var next = themes.Where(t => t.Id != id).ToList();
        await store.SaveAsync(next, cancellationToken);

        string? stored = await themeStore.GetStoredThemeAsync(cancellationToken);
        if (stored == ChoiceFor(id))
        {
            await themeStore.SetStoredThemeAsync("system", cancellationToken);
            await themeStore.ApplyThemeAsync("system", cancellationToken);
        }

        return next;
    }

    /// <summary>Activates a theme: persists the "custom:&lt;id&gt;" choice. The caller applies the resolved half.</summary>
    public async Task SetActiveAsync(string id, CancellationToken cancellationToken = default) =>
        await themeStore.SetStoredThemeAsync(ChoiceFor(id), cancellationToken);

    /// <summary>Resolves which half of a theme shows right now: the dark half when the device prefers dark.</summary>
    public async Task<ThemePalette> ResolveHalfAsync(CustomTheme theme, CancellationToken cancellationToken = default)
    {
        bool dark = await themeStore.PrefersDarkAsync(cancellationToken);
        return dark ? theme.Dark : theme.Light;
    }

    private static string UniqueName(string name, IEnumerable<string> existing)
    {
        var taken = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(name))
        {
            return name;
        }

        int suffix = 2;
        while (taken.Contains($"{name} {suffix}"))
        {
            suffix++;
        }

        return $"{name} {suffix}";
    }
}

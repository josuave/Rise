using System.Collections.ObjectModel;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Tests;

/// <summary>In-memory link store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeLinkStore : ILinkStore
{
    public StartPage? StoredPage { get; private set; }

    public FakeLinkStore(StartPage? initial = null) => StoredPage = initial;

    public Task<StartPage?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredPage);

    public Task SaveAsync(StartPage page, CancellationToken cancellationToken = default)
    {
        StoredPage = page;
        return Task.CompletedTask;
    }
}

/// <summary>Search launcher that records URLs instead of opening tabs.</summary>
internal sealed class FakeSearchLauncher : ISearchLauncher
{
    public Collection<Uri> LaunchedUrls { get; } = new();

    public Task LaunchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        LaunchedUrls.Add(url);
        return Task.CompletedTask;
    }
}

/// <summary>Clock that always reports the same moment, for header tests.</summary>
internal sealed class FakeClock(DateTime now) : IClock
{
    public DateTime Now => now;
}

/// <summary>In-memory custom theme store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeCustomThemeStore : ICustomThemeStore
{
    public IReadOnlyList<CustomTheme>? StoredThemes { get; set; }

    public FakeCustomThemeStore(IReadOnlyList<CustomTheme>? initial = null) => StoredThemes = initial;

    public Task<IReadOnlyList<CustomTheme>?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredThemes);

    public Task SaveAsync(IReadOnlyList<CustomTheme> themes, CancellationToken cancellationToken = default)
    {
        StoredThemes = themes;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory layout collection store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeLayoutCollectionStore : ILayoutCollectionStore
{
    public LayoutCollection? StoredCollection { get; private set; }

    public FakeLayoutCollectionStore(LayoutCollection? initial = null) => StoredCollection = initial;

    public Task<LayoutCollection?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredCollection);

    public Task SaveAsync(LayoutCollection collection, CancellationToken cancellationToken = default)
    {
        StoredCollection = collection;
        return Task.CompletedTask;
    }
}

/// <summary>Theme store that records preference/apply calls instead of touching the DOM.</summary>
internal sealed class FakeThemeStore : IThemeStore
{
    public string? Stored { get; private set; }
    public string? Applied { get; private set; }
    public ThemePalette? AppliedPalette { get; private set; }
    public bool SystemPrefersDark { get; set; }

    public FakeThemeStore(string? stored = null, bool systemPrefersDark = false)
    {
        Stored = stored;
        SystemPrefersDark = systemPrefersDark;
    }

    public Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Stored);

    public Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        Stored = theme;
        return Task.CompletedTask;
    }

    public Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        Applied = theme;
        return Task.CompletedTask;
    }

    public Task ApplyCustomThemeAsync(ThemePalette palette, CancellationToken cancellationToken = default)
    {
        AppliedPalette = palette;
        return Task.CompletedTask;
    }

    public Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(SystemPrefersDark);
}

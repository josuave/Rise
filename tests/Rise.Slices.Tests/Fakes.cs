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

/// <summary>Theme store that records preference/apply calls instead of touching the DOM.</summary>
internal sealed class FakeThemeStore : IThemeStore
{
    public string? Stored { get; private set; }
    public string? Applied { get; private set; }
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

    public Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(SystemPrefersDark);
}

using Rise.Application.Ports;
using Xunit;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Core.Tests;

/// <summary>Shell orchestration tests: services with fake ports, no browser, no JS.</summary>
public class ServiceTests
{
    private sealed class FakeLinkStore : ILinkStore
    {
        public StartPage? Stored { get; private set; }

        public Task<StartPage?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(StartPage page, CancellationToken cancellationToken = default)
        {
            Stored = page;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLauncher : ISearchLauncher
    {
        public List<Uri> Launched { get; } = new();

        public Task LaunchAsync(Uri url, CancellationToken cancellationToken = default)
        {
            Launched.Add(url);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEngineProvider(SearchEngine engine) : ISearchEngineProvider
    {
        public Task<SearchEngine> GetEngineAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(engine);
    }

    [Fact]
    public async Task StartPageService_Load_ReturnsDefaultsWhenStoreIsEmpty()
    {
        var service = new StartPageService(new FakeLinkStore());

        StartPage page = await service.LoadAsync();

        Assert.Equal(StartPage.Default().Links.Count, page.Links.Count);
    }

    [Fact]
    public async Task StartPageService_AddLink_PersistsThroughCore()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        StartPage page = await service.LoadAsync();

        Result<StartPage> result = await service.AddLinkAsync(page, "Example", "example.com");

        _ = ResultAssertions.RequireOk(result);
        Assert.NotNull(store.Stored);
        Assert.Contains(store.Stored!.Links, l => l.Name == "Example");
    }

    [Fact]
    public async Task StartPageService_AddLink_DoesNotPersistOnFailure()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        StartPage page = await service.LoadAsync();

        Result<StartPage> result = await service.AddLinkAsync(page, "Example", "ftp://example.com");

        Assert.NotEmpty(ResultAssertions.RequireError(result));
        Assert.Null(store.Stored);
    }

    [Fact]
    public async Task StartPageService_ResetToDefaults_SavesDefaultPage()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);

        await service.ResetToDefaultsAsync();

        Assert.NotNull(store.Stored);
        Assert.Equal(StartPage.Default().Links.Count, store.Stored!.Links.Count);
        Assert.Equal(StartPage.Default().Links[0].Name, store.Stored.Links[0].Name);
    }

    [Fact]
    public async Task SearchService_LaunchesBuiltUrl_AndIgnoresBlank()
    {
        var launcher = new FakeLauncher();
        var service = new SearchService(launcher);

        Assert.True(await service.LaunchSearchAsync("hello world"));
        Assert.Single(launcher.Launched);
        Assert.Equal("https://www.google.com/search?q=hello%20world", launcher.Launched[0].AbsoluteUri);

        Assert.False(await service.LaunchSearchAsync("   "));
        Assert.Single(launcher.Launched);
    }

    [Fact]
    public async Task SearchService_UsesConfiguredEngine()
    {
        var launcher = new FakeLauncher();
        var service = new SearchService(launcher, new FakeEngineProvider(SearchEngines.FromId("brave")));

        Assert.True(await service.LaunchSearchAsync("hello world"));

        Assert.Single(launcher.Launched);
        Assert.Equal("https://search.brave.com/search?q=hello%20world", launcher.Launched[0].AbsoluteUri);
    }

    [Fact]
    public async Task SearchService_UsesCustomEngine()
    {
        var launcher = new FakeLauncher();
        var engine = new SearchEngine("custom:mojeek", "Mojeek", "https://www.mojeek.com/search?q=%s");
        var service = new SearchService(launcher, new FakeEngineProvider(engine));

        Assert.True(await service.LaunchSearchAsync("hello world"));

        Assert.Single(launcher.Launched);
        Assert.Equal("https://www.mojeek.com/search?q=hello%20world", launcher.Launched[0].AbsoluteUri);
    }
}

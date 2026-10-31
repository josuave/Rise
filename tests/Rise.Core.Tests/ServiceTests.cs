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

    [Fact]
    public async Task StartPageService_Load_ReturnsDefaultsWhenStoreIsEmpty()
    {
        var service = new StartPageService(new FakeLinkStore());

        var page = await service.LoadAsync();

        Assert.Equal(StartPage.Default().Links.Count, page.Links.Count);
    }

    [Fact]
    public async Task StartPageService_AddLink_PersistsThroughCore()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        var page = await service.LoadAsync();

        var result = await service.AddLinkAsync(page, "Example", "example.com");

        Assert.True(result.IsSuccess);
        Assert.NotNull(store.Stored);
        Assert.Contains(store.Stored!.Links, l => l.Name == "Example");
    }

    [Fact]
    public async Task StartPageService_AddLink_DoesNotPersistOnFailure()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        var page = await service.LoadAsync();

        var result = await service.AddLinkAsync(page, "Example", "ftp://example.com");

        Assert.False(result.IsSuccess);
        Assert.Null(store.Stored);
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
}

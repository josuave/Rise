using System.Text.Json.Nodes;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class BackupServiceTests
{
    private sealed record TestDefinition(
        string Id,
        string Title,
        WidgetZone Zone,
        bool DefaultVisible,
        int Order,
        Type ComponentType) : IWidgetDefinition;

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

    private sealed class FakeThemeStore : IThemeStore
    {
        public string? Stored { get; private set; }

        public Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default)
        {
            Stored = theme;
            return Task.CompletedTask;
        }

        public Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ApplyCustomThemeAsync(ThemePalette palette, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeCustomThemeStore : ICustomThemeStore
    {
        public IReadOnlyList<CustomTheme>? Stored { get; private set; }

        public Task<IReadOnlyList<CustomTheme>?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(IReadOnlyList<CustomTheme> themes, CancellationToken cancellationToken = default)
        {
            Stored = themes;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLayoutCollectionStore : ILayoutCollectionStore
    {
        public LayoutCollection? Stored { get; private set; }

        public Task<LayoutCollection?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(LayoutCollection collection, CancellationToken cancellationToken = default)
        {
            Stored = collection;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWidgetConfigStore : IWidgetConfigStore
    {
        public Dictionary<string, string> Stored { get; } = new();

        public Task<string?> GetAsync(string widgetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.TryGetValue(widgetId, out string? json) ? json : null);

        public Task SetAsync(string widgetId, string json, CancellationToken cancellationToken = default)
        {
            Stored[widgetId] = json;
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private sealed class Fixture
    {
        public FakeLinkStore Links { get; } = new();

        public FakeThemeStore Theme { get; } = new();

        public FakeCustomThemeStore Themes { get; } = new();

        public FakeLayoutCollectionStore Layouts { get; } = new();

        public FakeWidgetConfigStore Widgets { get; } = new();

        public BackupService Service { get; }

        public Fixture()
        {
            var pageService = new StartPageService(Links);
            var themeService = new CustomThemeService(Themes, Theme);
            var layoutService = new LayoutService(Layouts, new[]
            {
                new TestDefinition("Search", "Search", WidgetZone.Main, true, 10, typeof(object)),
                new TestDefinition("Links", "Links", WidgetZone.Main, true, 20, typeof(object)),
            });
            Service = new BackupService(pageService, themeService, layoutService, Links, Theme, Themes, Layouts, Widgets);
        }
    }

    private static async Task SeedAsync(Fixture fixture)
    {
        await fixture.Links.SaveAsync(new StartPage(new[]
        {
            new Link(Guid.NewGuid(), "Example", new LinkUrl("https://example.com/")),
        }));
        await fixture.Theme.SetStoredThemeAsync("dark");
        await fixture.Themes.SaveAsync(new[]
        {
            new CustomTheme("t1", "Mine", ThemePalette.DefaultLight, ThemePalette.DefaultDark),
        });
        await fixture.Layouts.SaveAsync(new LayoutCollection("default", new[]
        {
            new NamedLayout("default", "Default", new PageLayout(new[]
            {
                new WidgetPlacement("Search", WidgetZone.Main, true),
            })),
        }));
        await fixture.Widgets.SetAsync("Clock", "{\"Use24Hour\":true,\"ShowSeconds\":false}");
    }

    [Fact]
    public async Task Export_WritesCurrentVersionEnvelope_WithEffectiveState()
    {
        var fixture = new Fixture();
        await SeedAsync(fixture);

        string json = await fixture.Service.ExportAsync();

        Result<BackupEnvelope> parsed = BackupFormat.Parse(json);
        Assert.True(parsed.IsSuccess);
        Assert.NotNull(parsed.Value);
        Assert.Equal(BackupFormat.CurrentVersion, parsed.Value.FormatVersion);
        BackupStateDto state = System.Text.Json.JsonSerializer.Deserialize<BackupStateDto>(
            parsed.Value.State.ToJsonString())!;
        Assert.Single(state.Links);
        Assert.Equal("Example", state.Links[0].Name);
        Assert.Equal("dark", state.Theme);
        Assert.Single(state.Themes);
        Assert.Equal("Mine", state.Themes[0].Name);
        Assert.Equal("default", state.Layouts.ActiveId);
        Assert.Equal(2, state.Layouts.Layouts[0].Widgets.Count);
        Assert.Equal("{\"Use24Hour\":true,\"ShowSeconds\":false}", state.WidgetConfigs["Clock"]);
    }

    [Fact]
    public async Task Import_RoundTrip_RestoresExportedState()
    {
        var source = new Fixture();
        await SeedAsync(source);
        string json = await source.Service.ExportAsync();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(json);

        Assert.True(result.IsSuccess);
        Assert.NotNull(target.Links.Stored);
        Assert.Single(target.Links.Stored.Links);
        Assert.Equal("Example", target.Links.Stored.Links[0].Name);
        Assert.Equal("dark", target.Theme.Stored);
        Assert.NotNull(target.Themes.Stored);
        Assert.Single(target.Themes.Stored);
        Assert.Equal("Mine", target.Themes.Stored[0].Name);
        Assert.NotNull(target.Layouts.Stored);
        Assert.Equal("default", target.Layouts.Stored.ActiveId);
        Assert.Equal(2, target.Layouts.Stored.Layouts[0].Layout.Widgets.Count);
        Assert.Equal("{\"Use24Hour\":true,\"ShowSeconds\":false}", target.Widgets.Stored["Clock"]);
    }

    [Fact]
    public async Task Import_RejectsNewerVersion_AndSavesNothing()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["FormatVersion"] = 2;
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.False(result.IsSuccess);
        Assert.Null(target.Links.Stored);
        Assert.Null(target.Theme.Stored);
        Assert.Null(target.Themes.Stored);
        Assert.Null(target.Layouts.Stored);
        Assert.Empty(target.Widgets.Stored);
    }

    [Fact]
    public async Task Import_RejectsCorruptJson_AndSavesNothing()
    {
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync("not json");

        Assert.False(result.IsSuccess);
        Assert.Null(target.Links.Stored);
    }

    [Fact]
    public async Task Import_RejectsMissingSection_AndSavesNothing()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["State"]!.AsObject().Remove("Themes");
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.False(result.IsSuccess);
        Assert.Null(target.Links.Stored);
        Assert.Null(target.Theme.Stored);
    }

    [Fact]
    public async Task Import_RepairsOrphanedCustomTheme_ToSystem()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["State"]!.AsObject()["Theme"] = "custom:missing";
        root["State"]!.AsObject()["Themes"] = new JsonArray();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.True(result.IsSuccess);
        Assert.Equal("system", target.Theme.Stored);
    }
}

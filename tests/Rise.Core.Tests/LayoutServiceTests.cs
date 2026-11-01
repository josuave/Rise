using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class LayoutServiceTests
{
    private static readonly string[] DefaultOrder = new[] { "Search", "Links", "LinkEditor", "Theme", "Settings" };
    private static readonly string[] ReconciledOrder = new[] { "Links", "Search", "Theme", "LinkEditor", "Settings" };

    private sealed record TestDefinition(
        string Id,
        string Title,
        WidgetZone Zone,
        bool DefaultVisible,
        int Order,
        Type ComponentType) : IWidgetDefinition;

    private sealed class FakeLayoutStore : ILayoutStore
    {
        public PageLayout? StoredLayout { get; private set; }

        public FakeLayoutStore(PageLayout? initial = null) => StoredLayout = initial;

        public Task<PageLayout?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(StoredLayout);

        public Task SaveAsync(PageLayout layout, CancellationToken cancellationToken = default)
        {
            StoredLayout = layout;
            return Task.CompletedTask;
        }
    }

    private static IWidgetDefinition[] Definitions() => new IWidgetDefinition[]
    {
        new TestDefinition("Search", "Search", WidgetZone.Main, true, 10, typeof(object)),
        new TestDefinition("Links", "Links", WidgetZone.Main, true, 20, typeof(object)),
        new TestDefinition("LinkEditor", "Link editor", WidgetZone.Main, false, 30, typeof(object)),
        new TestDefinition("Theme", "Theme switcher", WidgetZone.Cluster, true, 40, typeof(object)),
        new TestDefinition("Settings", "Settings", WidgetZone.Cluster, true, 50, typeof(object)),
    };

    [Fact]
    public async Task Load_WithNothingStored_ReturnsDefinitionsInOrder()
    {
        var service = new LayoutService(new FakeLayoutStore(), Definitions());

        PageLayout layout = await service.LoadAsync();

        Assert.Equal(
            DefaultOrder,
            layout.Widgets.Select(w => w.Id));
        Assert.False(layout.Widgets.Single(w => w.Id == "LinkEditor").Visible);
        Assert.True(layout.Widgets.Where(w => w.Id != "LinkEditor").All(w => w.Visible));
    }

    [Fact]
    public async Task Load_ReconcilesStoredLayout_AgainstDefinitions()
    {
        // Stored order is user-chosen; zones are placeholders (Main) as
        // the store persists them; "Ghost" is a widget that no longer exists.
        var stored = new PageLayout(new[]
        {
            new WidgetPlacement("Links", WidgetZone.Main, false),
            new WidgetPlacement("Search", WidgetZone.Main, true),
            new WidgetPlacement("Ghost", WidgetZone.Main, true),
            new WidgetPlacement("Theme", WidgetZone.Main, true),
        });
        var service = new LayoutService(new FakeLayoutStore(stored), Definitions());

        PageLayout layout = await service.LoadAsync();

        // Kept order and visibility, unknown id dropped, zones taken
        // from the definitions, missing widgets appended with defaults.
        Assert.Equal(
            ReconciledOrder,
            layout.Widgets.Select(w => w.Id));
        Assert.False(layout.Widgets.Single(w => w.Id == "Links").Visible);
        Assert.Equal(WidgetZone.Cluster, layout.Widgets.Single(w => w.Id == "Theme").Zone);
        Assert.False(layout.Widgets.Single(w => w.Id == "LinkEditor").Visible);
        Assert.True(layout.Widgets.Single(w => w.Id == "Settings").Visible);
    }

    [Fact]
    public async Task Reset_RestoresDefaults_AndNotifies()
    {
        var store = new FakeLayoutStore();
        var service = new LayoutService(store, Definitions());
        PageLayout hidden = await service.SetVisibleAsync(await service.LoadAsync(), "Search", false);
        Assert.False(hidden.Widgets.Single(w => w.Id == "Search").Visible);

        bool notified = false;
        service.Changed += (_, _) => notified = true;

        PageLayout reset = await service.ResetAsync();

        Assert.True(reset.Widgets.Single(w => w.Id == "Search").Visible);
        Assert.True(notified);
        Assert.NotNull(store.StoredLayout);
    }
}

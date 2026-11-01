using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class LayoutServiceTests
{
    private sealed record TestDefinition(
        string Id,
        string Title,
        WidgetZone Zone,
        bool DefaultVisible,
        int Order,
        Type ComponentType) : IWidgetDefinition;

    private sealed class FakeLayoutCollectionStore : ILayoutCollectionStore
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

    private static readonly string[] DefaultOrder = new[] { "Header", "Search", "Links", "LinkEditor", "Theme", "Settings" };
    private static readonly string[] ReconciledOrder = new[] { "Header", "Links", "Search", "LinkEditor", "Theme", "Settings" };

    private static IWidgetDefinition[] Definitions() => new IWidgetDefinition[]
    {
        new TestDefinition("Header", "Header", WidgetZone.Main, true, 5, typeof(object)),
        new TestDefinition("Search", "Search", WidgetZone.Main, true, 10, typeof(object)),
        new TestDefinition("Links", "Links", WidgetZone.Main, true, 20, typeof(object)),
        new TestDefinition("LinkEditor", "Link editor", WidgetZone.Main, false, 30, typeof(object)),
        new TestDefinition("Theme", "Theme switcher", WidgetZone.Cluster, true, 40, typeof(object)),
        new TestDefinition("Settings", "Settings", WidgetZone.Cluster, true, 50, typeof(object)),
    };

    [Fact]
    public async Task LoadCollection_WithNothingStored_ReturnsJustTheDefaultLayout()
    {
        var service = new LayoutService(new FakeLayoutCollectionStore(), Definitions());

        LayoutCollection collection = await service.LoadCollectionAsync();

        Assert.Equal(LayoutCollection.DefaultId, collection.ActiveId);
        Assert.Single(collection.Layouts);
        NamedLayout? active = collection.Active;
        Assert.NotNull(active);
        Assert.Equal(LayoutCollection.DefaultName, active.Name);
        Assert.Equal(DefaultOrder, active.Layout.Widgets.Select(w => w.Id));
        Assert.False(active.Layout.Widgets.Single(w => w.Id == "LinkEditor").Visible);
    }

    [Fact]
    public async Task LoadCollection_ReconcilesStoredLayouts_AgainstDefinitions()
    {
        var stored = new LayoutCollection("ghost", new[]
        {
            new NamedLayout("work", "Work", new PageLayout(new[]
            {
                new WidgetPlacement("Links", WidgetZone.Main, false),
                new WidgetPlacement("Search", WidgetZone.Main, true),
                new WidgetPlacement("Ghost", WidgetZone.Main, true),
                new WidgetPlacement("Theme", WidgetZone.Main, true),
            })),
            new NamedLayout("morning", "   ", new PageLayout(new[]
            {
                new WidgetPlacement("Search", WidgetZone.Main, true),
            })),
        });
        var service = new LayoutService(new FakeLayoutCollectionStore(stored), Definitions());

        LayoutCollection collection = await service.LoadCollectionAsync();

        // Default layout is always present (first); unknown active id
        // falls back to it; unknown widget ids are dropped; zones come
        // from the definitions; blank names become "Untitled"; widgets
        // a layout predates slot in by definition Order.
        Assert.Equal(LayoutCollection.DefaultId, collection.ActiveId);
        Assert.Equal(
            new[] { LayoutCollection.DefaultId, "work", "morning" },
            collection.Layouts.Select(l => l.Id));
        NamedLayout work = collection.Layouts.Single(l => l.Id == "work");
        Assert.Equal(
            ReconciledOrder,
            work.Layout.Widgets.Select(w => w.Id));
        Assert.False(work.Layout.Widgets.Single(w => w.Id == "Links").Visible);
        Assert.Equal(WidgetZone.Cluster, work.Layout.Widgets.Single(w => w.Id == "Theme").Zone);
        Assert.Equal("Untitled", collection.Layouts.Single(l => l.Id == "morning").Name);
        Assert.Equal(DefaultOrder, collection.Layouts.Single(l => l.Id == "morning").Layout.Widgets.Select(w => w.Id));
    }

    [Fact]
    public async Task ActiveOperations_AffectOnlyTheActiveLayout()
    {
        var store = new FakeLayoutCollectionStore();
        var service = new LayoutService(store, Definitions());
        await service.SaveAsAsync("Work");

        PageLayout active = await service.LoadAsync();
        await service.SetVisibleAsync(active, "Search", false);

        Assert.NotNull(store.StoredCollection);
        NamedLayout work = store.StoredCollection.Layouts.Single(l => l.Name == "Work");
        Assert.False(work.Layout.Widgets.Single(w => w.Id == "Search").Visible);
        NamedLayout defaults = store.StoredCollection.Layouts.Single(l => l.Id == LayoutCollection.DefaultId);
        Assert.True(defaults.Layout.Widgets.Single(w => w.Id == "Search").Visible);
    }

    [Fact]
    public async Task SaveSwitchRenameDelete_ManageTheCollection()
    {
        var store = new FakeLayoutCollectionStore();
        var service = new LayoutService(store, Definitions());

        LayoutCollection saved = await service.SaveAsAsync("Work");
        Assert.Equal(2, saved.Layouts.Count);
        Assert.Equal("Work", saved.Active?.Name);

        LayoutCollection savedAgain = await service.SaveAsAsync("Work");
        Assert.Equal("Work 2", savedAgain.Active?.Name);

        LayoutCollection switched = await service.SetActiveLayoutAsync(LayoutCollection.DefaultId);
        Assert.Equal(LayoutCollection.DefaultId, switched.ActiveId);

        string workId = store.StoredCollection!.Layouts.Single(l => l.Name == "Work").Id;
        LayoutCollection renamed = await service.RenameLayoutAsync(workId, "Morning");
        Assert.Equal("Morning", renamed.Layouts.Single(l => l.Id == workId).Name);

        string work2Id = store.StoredCollection.Layouts.Single(l => l.Name == "Work 2").Id;
        LayoutCollection deleted = await service.DeleteLayoutAsync(work2Id);
        Assert.DoesNotContain(deleted.Layouts, l => l.Id == work2Id);

        LayoutCollection defaultKept = await service.DeleteLayoutAsync(LayoutCollection.DefaultId);
        Assert.Contains(defaultKept.Layouts, l => l.Id == LayoutCollection.DefaultId);
    }

    [Fact]
    public async Task Reset_RestoresActiveDefaults_AndNotifies()
    {
        var store = new FakeLayoutCollectionStore();
        var service = new LayoutService(store, Definitions());
        PageLayout hidden = await service.SetVisibleAsync(await service.LoadAsync(), "Search", false);
        Assert.False(hidden.Widgets.Single(w => w.Id == "Search").Visible);

        bool notified = false;
        service.Changed += (_, _) => notified = true;

        PageLayout reset = await service.ResetAsync();

        Assert.True(reset.Widgets.Single(w => w.Id == "Search").Visible);
        Assert.True(notified);
        Assert.NotNull(store.StoredCollection);
    }
}

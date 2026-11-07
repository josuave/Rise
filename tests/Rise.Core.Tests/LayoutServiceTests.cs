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
    public async Task ApplyCalmBoardArrangement_SetsDVisibilityInDefinitionOrder_AndNotifies()
    {
        var store = new FakeLayoutCollectionStore();
        var service = new LayoutService(store, BoardDefinitions());
        await service.SaveAsAsync("Work");
        await service.SetVisibleAsync(await service.LoadAsync(), "Links", false);
        await service.SetVisibleAsync(await service.LoadAsync(), "Backup", true);

        bool notified = false;
        service.Changed += (_, _) => notified = true;

        PageLayout arranged = await service.ApplyCalmBoardArrangementAsync();

        Assert.Equal(
            BoardOrder,
            arranged.Widgets.Select(w => w.Id));
        Assert.True(arranged.Widgets.Single(w => w.Id == "Weather").Visible);
        Assert.True(arranged.Widgets.Single(w => w.Id == "Links").Visible);
        Assert.True(arranged.Widgets.Single(w => w.Id == "Todo").Visible);
        Assert.True(arranged.Widgets.Single(w => w.Id == "Notes").Visible);
        Assert.False(arranged.Widgets.Single(w => w.Id == "Backup").Visible);
        Assert.False(arranged.Widgets.Single(w => w.Id == "Group").Visible);
        Assert.True(arranged.Widgets.Single(w => w.Id == "Theme").Visible);
        Assert.True(notified);

        // Only the active layout changed: the default still hides the widgets it hid.
        Assert.NotNull(store.StoredCollection);
        NamedLayout defaults = store.StoredCollection.Layouts.Single(l => l.Id == LayoutCollection.DefaultId);
        Assert.False(defaults.Layout.Widgets.Single(w => w.Id == "Weather").Visible);
        Assert.False(defaults.Layout.Widgets.Single(w => w.Id == "Backup").Visible);
    }

    [Fact]
    public async Task ApplyCalmBoardArrangement_ClearsGroup_AndShowsMembers()
    {
        var stored = new LayoutCollection("default", new[]
        {
            new NamedLayout("default", "Default", new PageLayout(
                BoardDefinitions().Select(d => new WidgetPlacement(d.Id, d.Zone, d.DefaultVisible)).ToArray(),
                SearchGrouped)),
        });
        var service = new LayoutService(new FakeLayoutCollectionStore(stored), BoardDefinitions());

        PageLayout arranged = await service.ApplyCalmBoardArrangementAsync();

        Assert.Empty(arranged.GroupChildren);
        Assert.True(arranged.Widgets.Single(w => w.Id == "Search").Visible);
    }

    [Fact]
    public async Task ApplyArrangementAsync_AppliesGivenPlacementsToActive()
    {
        var store = new FakeLayoutCollectionStore();
        var service = new LayoutService(store, Definitions());
        PageLayout rearranged = (await service.LoadAsync()).SetVisible("Search", false);

        bool notified = false;
        service.Changed += (_, _) => notified = true;

        PageLayout applied = await service.ApplyArrangementAsync(rearranged);

        Assert.False(applied.Widgets.Single(w => w.Id == "Search").Visible);
        Assert.True(notified);
        Assert.NotNull(store.StoredCollection);
        Assert.False(store.StoredCollection.Layouts.Single(l => l.Id == LayoutCollection.DefaultId).Layout.Widgets.Single(w => w.Id == "Search").Visible);
    }

    private static readonly string[] BoardOrder = new[] { "Date", "Clock", "Greeting", "Weather", "Search", "Links", "Backup", "Todo", "Notes", "Group", "Theme" };

    private static IWidgetDefinition[] BoardDefinitions() => new IWidgetDefinition[]
    {
        new TestDefinition("Date", "Date", WidgetZone.Main, true, 5, typeof(object)),
        new TestDefinition("Clock", "Clock", WidgetZone.Main, true, 6, typeof(object)),
        new TestDefinition("Greeting", "Greeting", WidgetZone.Main, true, 7, typeof(object)),
        new TestDefinition("Weather", "Weather", WidgetZone.Main, false, 8, typeof(object)),
        new TestDefinition("Search", "Search", WidgetZone.Main, true, 10, typeof(object)),
        new TestDefinition("Links", "Links", WidgetZone.Main, true, 20, typeof(object)),
        new TestDefinition("Backup", "Backup", WidgetZone.Main, false, 35, typeof(object)),
        new TestDefinition("Todo", "Todo", WidgetZone.Main, false, 36, typeof(object)),
        new TestDefinition("Notes", "Notes", WidgetZone.Main, false, 37, typeof(object)),
        new TestGroupDefinition("Group", "Group", WidgetZone.Main, false, 45, typeof(object)),
        new TestDefinition("Theme", "Theme switcher", WidgetZone.Cluster, true, 50, typeof(object)),
    };

    private sealed record TestGroupDefinition(
        string Id,
        string Title,
        WidgetZone Zone,
        bool DefaultVisible,
        int Order,
        Type ComponentType) : IWidgetDefinition
    {
        public bool IsGroup => true;
    }

    private static readonly string[] StoredGroupMembers = new[] { "Links", "Ghost", "Theme", "Group" };
    private static readonly string[] ReconciledGroupMembers = new[] { "Links" };
    private static readonly string[] SearchGrouped = new[] { "Search" };
    private static readonly string[] DefaultOrderWithGroup = new[] { "Header", "Search", "Links", "LinkEditor", "Theme", "Group", "Settings" };

    private static IWidgetDefinition[] DefinitionsWithGroup() => Definitions().Append(
        new TestGroupDefinition("Group", "Group", WidgetZone.Main, false, 45, typeof(object))).ToArray();

    private static LayoutCollection GroupedCollection(bool groupVisible) => new("default", new[]
    {
        new NamedLayout("default", "Default", new PageLayout(
            new[]
            {
                new WidgetPlacement("Header", WidgetZone.Main, true),
                new WidgetPlacement("Search", WidgetZone.Main, true),
                new WidgetPlacement("Links", WidgetZone.Main, true),
                new WidgetPlacement("LinkEditor", WidgetZone.Main, false),
                new WidgetPlacement("Theme", WidgetZone.Cluster, true),
                new WidgetPlacement("Group", WidgetZone.Main, groupVisible),
                new WidgetPlacement("Settings", WidgetZone.Cluster, true),
            },
            SearchGrouped)),
    });

    [Fact]
    public async Task LoadCollection_Reconcile_FiltersGroupMembers()
    {
        var stored = new LayoutCollection("default", new[]
        {
            new NamedLayout("default", "Default", new PageLayout(
                new[]
                {
                    new WidgetPlacement("Search", WidgetZone.Main, true),
                    new WidgetPlacement("Links", WidgetZone.Main, true),
                    new WidgetPlacement("Theme", WidgetZone.Main, true),
                    new WidgetPlacement("Group", WidgetZone.Main, false),
                },
                StoredGroupMembers)),
        });
        var service = new LayoutService(new FakeLayoutCollectionStore(stored), DefinitionsWithGroup());

        LayoutCollection collection = await service.LoadCollectionAsync();

        NamedLayout? active = collection.Active;
        Assert.NotNull(active);

        // Ghost is unknown, Theme is Cluster, Group can't contain itself.
        Assert.Equal(ReconciledGroupMembers, active.Layout.GroupChildren);
        Assert.True(active.Layout.Widgets.Any(w => w.Id == "Group"));
    }

    [Fact]
    public async Task AddToGroup_ShowsGroup_AndRejectsClusterAndGroup()
    {
        var service = new LayoutService(new FakeLayoutCollectionStore(), DefinitionsWithGroup());
        PageLayout layout = await service.LoadAsync();

        PageLayout grouped = await service.AddToGroupAsync(layout, "Search");
        Assert.Equal(SearchGrouped, grouped.GroupChildren);
        Assert.True(grouped.Widgets.Single(w => w.Id == "Group").Visible);

        Assert.Same(layout, await service.AddToGroupAsync(layout, "Theme"));
        Assert.Same(layout, await service.AddToGroupAsync(layout, "Group"));
    }

    [Fact]
    public async Task RemoveFromGroup_RestoresTopLevelPosition()
    {
        var service = new LayoutService(new FakeLayoutCollectionStore(GroupedCollection(groupVisible: true)), DefinitionsWithGroup());
        PageLayout layout = await service.LoadAsync();

        PageLayout removed = await service.RemoveFromGroupAsync(layout, "Search");

        Assert.Empty(removed.GroupChildren);
        Assert.Equal(DefaultOrderWithGroup, removed.Widgets.Select(w => w.Id));
    }

    [Fact]
    public async Task DeleteGroup_Only_ReturnsMembers_AndHidesGroup()
    {
        var service = new LayoutService(new FakeLayoutCollectionStore(GroupedCollection(groupVisible: true)), DefinitionsWithGroup());

        PageLayout result = await service.DeleteGroupAsync(deleteWidgets: false);

        Assert.Empty(result.GroupChildren);
        Assert.False(result.Widgets.Single(w => w.Id == "Group").Visible);
        Assert.Contains("Search", result.Widgets.Select(w => w.Id));
    }

    [Fact]
    public async Task DeleteGroup_WithWidgets_HidesMembers()
    {
        var service = new LayoutService(new FakeLayoutCollectionStore(GroupedCollection(groupVisible: true)), DefinitionsWithGroup());

        PageLayout result = await service.DeleteGroupAsync(deleteWidgets: true);

        Assert.Empty(result.GroupChildren);
        Assert.False(result.Widgets.Single(w => w.Id == "Group").Visible);

        // Hidden, not removed: reconcile would re-add a missing widget,
        // so a deleted member stays in the layout, hidden, until the
        // editor shows it again.
        Assert.False(result.Widgets.Single(w => w.Id == "Search").Visible);
    }
}

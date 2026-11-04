using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Layout;
using Rise.Slices.Backup;
using Rise.Slices.Header;
using Rise.Slices.Links;
using Rise.Slices.Notes;
using Rise.Slices.Search;
using Rise.Slices.Settings;
using Rise.Slices.Todo;
using Xunit;

namespace Rise.Slices.Tests;

public class LayoutEditorTests : BunitContext
{
    private static readonly string[] DefaultOrder = new[] { "Date", "Clock", "Greeting", "Search", "Links", "LinkEditor", "Backup", "Todo", "Notes", "Theme", "Group", "Settings" };
    private static readonly string[] MovedOrder = new[] { "Date", "Clock", "Greeting", "Links", "Search", "LinkEditor", "Backup", "Todo", "Notes", "Theme", "Group", "Settings" };
    private static readonly bool[] DefaultVisibility = new[] { true, true, true, true, true, false, false, false, false, true, false, true };
    private static readonly string[] GroupedLinksSearch = new[] { "Links", "Search" };

    private readonly FakeLayoutCollectionStore _layoutStore = new();

    public LayoutEditorTests()
    {
        Services.AddScoped<ILayoutCollectionStore>(_ => _layoutStore);
        Services.AddScoped<LayoutService>();
        Services.AddSingleton<IWidgetDefinition, DateWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, ClockWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, GreetingWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, BackupWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, SearchWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, LinksWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, LinkEditorWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, ThemeWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, TodoWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, NotesWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, GroupWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, SettingsWidgetDefinition>();
    }

    private static NamedLayout ActiveOf(LayoutCollection? collection)
    {
        Assert.NotNull(collection);
        NamedLayout? active = collection.Active;
        Assert.NotNull(active);
        return active;
    }

    private static IElement ButtonWithText(IRenderedComponent<LayoutEditor> cut, string text) =>
        cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

    [Fact]
    public void RendersLayoutsAndWidgetRows()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();

        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Default", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(24, cut.FindAll("[aria-label^=\"Move\"]").Count);
    }

    [Fact]
    public void Configure_ExpandsWidgetConfigEditor()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore());
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<ClockConfig>();
        Services.AddScoped<GreetingConfig>();

        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        Assert.Equal(3, cut.FindAll("[aria-label^=\"Configure\"]").Count);

        cut.Find("[aria-label=\"Configure Clock\"]").Click();
        cut.WaitForAssertion(() => Assert.Contains("24-hour clock", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Configure Clock\"]").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain("24-hour clock", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void MoveUp_ReordersWithinZone_AndPersists()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Move Links up\"]").Click();

        Assert.Equal(MovedOrder, ActiveOf(_layoutStore.StoredCollection).Layout.Widgets.Select(w => w.Id));
    }

    [Fact]
    public void HideAndShow_PersistVisibility()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Hide Search\"]").Click();
        Assert.False(ActiveOf(_layoutStore.StoredCollection).Layout.Widgets.Single(w => w.Id == "Search").Visible);

        cut.Find("[aria-label=\"Show Link editor\"]").Click();
        Assert.True(ActiveOf(_layoutStore.StoredCollection).Layout.Widgets.Single(w => w.Id == "LinkEditor").Visible);
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Hide Search\"]").Click();
        cut.Find(".calm-layout-reset").Click();

        Assert.Equal(DefaultOrder, ActiveOf(_layoutStore.StoredCollection).Layout.Widgets.Select(w => w.Id));
        Assert.Equal(DefaultVisibility, ActiveOf(_layoutStore.StoredCollection).Layout.Widgets.Select(w => w.Visible));
    }

    [Fact]
    public void SaveAs_CreatesAndActivatesNewLayout()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"New layout name\"]").Change("Work");
        ButtonWithText(cut, "Save current as new").Click();

        cut.WaitForAssertion(() => Assert.Contains("Work", cut.Markup, StringComparison.Ordinal));
        LayoutCollection? stored = _layoutStore.StoredCollection;
        Assert.NotNull(stored);
        Assert.Equal(2, stored.Layouts.Count);
        Assert.Equal("Work", stored.Active?.Name);
    }

    [Fact]
    public void Switch_ChangesActiveLayout()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));
        cut.Find("[aria-label=\"New layout name\"]").Change("Work");
        ButtonWithText(cut, "Save current as new").Click();
        cut.WaitForAssertion(() => Assert.Contains("Work", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Switch to Default\"]").Click();

        Assert.Equal(LayoutCollection.DefaultId, ActiveOf(_layoutStore.StoredCollection).Id);
    }

    [Fact]
    public void Delete_RemovesLayout_AndFallsBackToDefault()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));
        cut.Find("[aria-label=\"New layout name\"]").Change("Work");
        ButtonWithText(cut, "Save current as new").Click();
        cut.WaitForAssertion(() => Assert.Contains("Work", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Delete Work\"]").Click();

        LayoutCollection? stored = _layoutStore.StoredCollection;
        Assert.NotNull(stored);
        Assert.Single(stored.Layouts);
        Assert.Equal(LayoutCollection.DefaultId, stored.ActiveId);
    }

    [Fact]
    public void Rename_ChangesActiveLayoutName()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Rename active layout\"]").Change("Morning");
        ButtonWithText(cut, "Rename active").Click();

        Assert.Equal("Morning", ActiveOf(_layoutStore.StoredCollection).Name);
    }

    private static void AddToGroup(IRenderedComponent<LayoutEditor> cut, string title)
    {
        cut.Find($"[aria-label=\"Add {title} to group\"]").Click();
        cut.WaitForAssertion(() => Assert.Contains($"Remove {title} from group", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Group_MoveIntoGroup_ShowsChildRow_AndShowsGroup()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        AddToGroup(cut, "Search");

        PageLayout active = ActiveOf(_layoutStore.StoredCollection).Layout;
        Assert.Single(active.GroupChildren);
        Assert.Equal("Search", active.GroupChildren[0]);
        Assert.True(active.Widgets.Single(w => w.Id == "Group").Visible);
    }

    [Fact]
    public void Group_MoveWithinGroup_ReordersMembers()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        AddToGroup(cut, "Search");
        AddToGroup(cut, "Links");

        cut.Find("[aria-label=\"Move Links up\"]").Click();

        Assert.Equal(GroupedLinksSearch, ActiveOf(_layoutStore.StoredCollection).Layout.GroupChildren);
    }

    [Fact]
    public void Group_RemoveFromGroup_ReturnsWidgetToTopLevel()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        AddToGroup(cut, "Search");

        cut.Find("[aria-label=\"Remove Search from group\"]").Click();

        PageLayout active = ActiveOf(_layoutStore.StoredCollection).Layout;
        Assert.Empty(active.GroupChildren);
        Assert.Contains("Search", active.Widgets.Select(w => w.Id));
    }

    [Fact]
    public void Group_DeleteOnly_KeepsWidgets_AndHidesGroup()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        AddToGroup(cut, "Search");

        cut.Find("[aria-label=\"Delete group\"]").Click();
        cut.Find("[aria-label=\"Delete group only\"]").Click();

        PageLayout active = ActiveOf(_layoutStore.StoredCollection).Layout;
        Assert.Empty(active.GroupChildren);
        Assert.Contains("Search", active.Widgets.Select(w => w.Id));
        Assert.False(active.Widgets.Single(w => w.Id == "Group").Visible);
    }

    [Fact]
    public void Group_DeleteWithWidgets_HidesMembers()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        AddToGroup(cut, "Search");

        cut.Find("[aria-label=\"Delete group\"]").Click();
        cut.Find("[aria-label=\"Delete group and widgets\"]").Click();

        PageLayout active = ActiveOf(_layoutStore.StoredCollection).Layout;
        Assert.Empty(active.GroupChildren);
        Assert.False(active.Widgets.Single(w => w.Id == "Search").Visible);
        Assert.False(active.Widgets.Single(w => w.Id == "Group").Visible);
    }

    [Fact]
    public void Group_Configure_ExpandsNameEditor_AndSaves()
    {
        var store = new FakeWidgetConfigStore();
        Services.AddScoped<IWidgetConfigStore>(_ => store);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<GroupConfig>();

        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Configure Group\"]").Click();
        cut.WaitForAssertion(() => Assert.Contains("Group name", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Group name\"]").Change("Morning");
        Assert.Equal("{\"Name\":\"Morning\"}", store.Stored["Group"]);
    }
}

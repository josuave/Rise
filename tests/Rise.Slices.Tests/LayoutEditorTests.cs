using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Layout;
using Rise.Slices.Header;
using Rise.Slices.Links;
using Rise.Slices.Search;
using Rise.Slices.Settings;
using Xunit;

namespace Rise.Slices.Tests;

public class LayoutEditorTests : BunitContext
{
    private static readonly string[] DefaultOrder = new[] { "Header", "Search", "Links", "LinkEditor", "Theme", "Settings" };
    private static readonly string[] MovedOrder = new[] { "Header", "Links", "Search", "LinkEditor", "Theme", "Settings" };
    private static readonly bool[] DefaultVisibility = new[] { true, true, true, false, true, true };

    private readonly FakeLayoutCollectionStore _layoutStore = new();

    public LayoutEditorTests()
    {
        Services.AddScoped<ILayoutCollectionStore>(_ => _layoutStore);
        Services.AddScoped<LayoutService>();
        Services.AddSingleton<IWidgetDefinition, HeaderWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, SearchWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, LinksWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, LinkEditorWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, ThemeWidgetDefinition>();
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
        Assert.Equal(12, cut.FindAll("[aria-label^=\"Move\"]").Count);
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
}

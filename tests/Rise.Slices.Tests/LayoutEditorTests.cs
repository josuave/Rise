using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Layout;
using Rise.Slices.Links;
using Rise.Slices.Search;
using Rise.Slices.Settings;
using Xunit;

namespace Rise.Slices.Tests;

public class LayoutEditorTests : BunitContext
{
    private static readonly string[] DefaultOrder = new[] { "Search", "Links", "LinkEditor", "Theme", "Settings" };
    private static readonly string[] MovedOrder = new[] { "Links", "Search", "LinkEditor", "Theme", "Settings" };
    private static readonly bool[] DefaultVisibility = new[] { true, true, false, true, true };

    private readonly FakeLayoutStore _layoutStore = new();

    public LayoutEditorTests()
    {
        Services.AddScoped<ILayoutStore>(_ => _layoutStore);
        Services.AddScoped<LayoutService>();
        Services.AddSingleton<IWidgetDefinition, SearchWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, LinksWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, LinkEditorWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, ThemeWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, SettingsWidgetDefinition>();
    }

    [Fact]
    public void RendersOneRowPerWidget()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();

        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));
        Assert.Equal(5, cut.FindAll(".calm-layout-row").Count);
    }

    [Fact]
    public void MoveUp_ReordersWithinZone_AndPersists()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Move Links up\"]").Click();

        Assert.NotNull(_layoutStore.StoredLayout);
        Assert.Equal(
            MovedOrder,
            _layoutStore.StoredLayout.Widgets.Select(w => w.Id));
    }

    [Fact]
    public void HideAndShow_PersistVisibility()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Hide Search\"]").Click();
        Assert.NotNull(_layoutStore.StoredLayout);
        Assert.False(_layoutStore.StoredLayout.Widgets.Single(w => w.Id == "Search").Visible);

        cut.Find("[aria-label=\"Show Link editor\"]").Click();
        Assert.True(_layoutStore.StoredLayout.Widgets.Single(w => w.Id == "LinkEditor").Visible);
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Hide Search\"]").Click();
        cut.Find(".calm-layout-reset").Click();

        Assert.NotNull(_layoutStore.StoredLayout);
        Assert.Equal(
            DefaultOrder,
            _layoutStore.StoredLayout.Widgets.Select(w => w.Id));
        Assert.Equal(
            DefaultVisibility,
            _layoutStore.StoredLayout.Widgets.Select(w => w.Visible));
    }
}

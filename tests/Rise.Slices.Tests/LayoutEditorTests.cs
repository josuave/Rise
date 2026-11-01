using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Layout;
using Xunit;

namespace Rise.Slices.Tests;

public class LayoutEditorTests : BunitContext
{
    private readonly FakeLayoutStore _layoutStore = new();

    public LayoutEditorTests()
    {
        Services.AddScoped<ILayoutStore>(_ => _layoutStore);
        Services.AddScoped<LayoutService>();
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
            new[] { WidgetKind.Links, WidgetKind.Search, WidgetKind.LinkEditor, WidgetKind.Theme, WidgetKind.Settings },
            _layoutStore.StoredLayout.Widgets.Select(w => w.Kind));
    }

    [Fact]
    public void HideAndShow_PersistVisibility()
    {
        IRenderedComponent<LayoutEditor> cut = Render<LayoutEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Link editor", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Hide Search\"]").Click();
        Assert.NotNull(_layoutStore.StoredLayout);
        Assert.False(_layoutStore.StoredLayout.Widgets.Single(w => w.Kind == WidgetKind.Search).Visible);

        cut.Find("[aria-label=\"Show Link editor\"]").Click();
        Assert.True(_layoutStore.StoredLayout.Widgets.Single(w => w.Kind == WidgetKind.LinkEditor).Visible);
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
            PageLayout.Default().Widgets.Select(w => w.Kind),
            _layoutStore.StoredLayout.Widgets.Select(w => w.Kind));
        Assert.Equal(
            PageLayout.Default().Widgets.Select(w => w.Visible),
            _layoutStore.StoredLayout.Widgets.Select(w => w.Visible));
    }
}

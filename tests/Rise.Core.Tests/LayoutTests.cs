using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class LayoutTests
{
    [Fact]
    public void Default_HasAllWidgets_WithEditorHidden()
    {
        var layout = PageLayout.Default();

        Assert.Equal(
            new[] { WidgetKind.Search, WidgetKind.Links, WidgetKind.LinkEditor, WidgetKind.Theme, WidgetKind.Settings },
            layout.Widgets.Select(w => w.Kind));
        Assert.False(layout.Widgets.Single(w => w.Kind == WidgetKind.LinkEditor).Visible);
        Assert.True(layout.Widgets.Where(w => w.Kind != WidgetKind.LinkEditor).All(w => w.Visible));
    }

    [Fact]
    public void Zones_SplitClusterFromMain()
    {
        var layout = PageLayout.Default();

        Assert.Equal(
            new[] { WidgetKind.Search, WidgetKind.Links, WidgetKind.LinkEditor },
            layout.MainWidgets().Select(w => w.Kind));
        Assert.Equal(
            new[] { WidgetKind.Theme, WidgetKind.Settings },
            layout.ClusterWidgets().Select(w => w.Kind));
    }

    [Fact]
    public void MoveUp_SwapsWithSameZoneNeighbourOnly()
    {
        var layout = PageLayout.Default();

        // Links moves above Search (same zone).
        Assert.Equal(
            new[] { WidgetKind.Links, WidgetKind.Search, WidgetKind.LinkEditor, WidgetKind.Theme, WidgetKind.Settings },
            layout.MoveUp(WidgetKind.Links).Widgets.Select(w => w.Kind));

        // Settings moves above Theme (cluster zone) but never into the main zone.
        Assert.Equal(
            new[] { WidgetKind.Search, WidgetKind.Links, WidgetKind.LinkEditor, WidgetKind.Settings, WidgetKind.Theme },
            layout.MoveUp(WidgetKind.Settings).Widgets.Select(w => w.Kind));
    }

    [Fact]
    public void MoveDown_AtZoneEdge_IsNoOp()
    {
        var layout = PageLayout.Default();

        Assert.Same(layout, layout.MoveDown(WidgetKind.LinkEditor));
        Assert.Same(layout, layout.MoveUp(WidgetKind.Search));
        Assert.Same(layout, layout.MoveDown(WidgetKind.Settings));
    }

    [Fact]
    public void SetVisible_HidesWithoutLosingPosition()
    {
        var layout = PageLayout.Default();
        PageLayout hidden = layout.SetVisible(WidgetKind.Links, false);

        Assert.False(hidden.Widgets.Single(w => w.Kind == WidgetKind.Links).Visible);
        Assert.Equal(
            new[] { WidgetKind.Search, WidgetKind.Links, WidgetKind.LinkEditor, WidgetKind.Theme, WidgetKind.Settings },
            hidden.Widgets.Select(w => w.Kind));
        Assert.True(hidden.SetVisible(WidgetKind.Links, true).Widgets.Single(w => w.Kind == WidgetKind.Links).Visible);
    }
}

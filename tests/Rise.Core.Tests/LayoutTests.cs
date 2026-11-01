using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class LayoutTests
{
    private static readonly string[] MainIds = new[] { "Search", "Links", "LinkEditor" };
    private static readonly string[] ClusterIds = new[] { "Theme", "Settings" };
    private static readonly string[] FullOrder = new[] { "Search", "Links", "LinkEditor", "Theme", "Settings" };
    private static readonly string[] LinksUpOrder = new[] { "Links", "Search", "LinkEditor", "Theme", "Settings" };
    private static readonly string[] SettingsUpOrder = new[] { "Search", "Links", "LinkEditor", "Settings", "Theme" };

    private static PageLayout Sample() => new(new[]
    {
        new WidgetPlacement("Search", WidgetZone.Main, true),
        new WidgetPlacement("Links", WidgetZone.Main, true),
        new WidgetPlacement("LinkEditor", WidgetZone.Main, false),
        new WidgetPlacement("Theme", WidgetZone.Cluster, true),
        new WidgetPlacement("Settings", WidgetZone.Cluster, true),
    });

    [Fact]
    public void Zones_SplitClusterFromMain()
    {
        PageLayout layout = Sample();

        Assert.Equal(
            MainIds,
            layout.MainWidgets().Select(w => w.Id));
        Assert.Equal(
            ClusterIds,
            layout.ClusterWidgets().Select(w => w.Id));
    }

    [Fact]
    public void MoveUp_SwapsWithSameZoneNeighbourOnly()
    {
        PageLayout layout = Sample();

        // Links moves above Search (same zone).
        Assert.Equal(
            LinksUpOrder,
            layout.MoveUp("Links").Widgets.Select(w => w.Id));

        // Settings moves above Theme (cluster zone) but never into the main zone.
        Assert.Equal(
            SettingsUpOrder,
            layout.MoveUp("Settings").Widgets.Select(w => w.Id));
    }

    [Fact]
    public void MoveDown_AtZoneEdge_IsNoOp()
    {
        PageLayout layout = Sample();

        Assert.Same(layout, layout.MoveDown("LinkEditor"));
        Assert.Same(layout, layout.MoveUp("Search"));
        Assert.Same(layout, layout.MoveDown("Settings"));
    }

    [Fact]
    public void SetVisible_HidesWithoutLosingPosition()
    {
        PageLayout layout = Sample();
        PageLayout hidden = layout.SetVisible("Links", false);

        Assert.False(hidden.Widgets.Single(w => w.Id == "Links").Visible);
        Assert.Equal(
            FullOrder,
            hidden.Widgets.Select(w => w.Id));
        Assert.True(hidden.SetVisible("Links", true).Widgets.Single(w => w.Id == "Links").Visible);
    }
}

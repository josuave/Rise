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
    private static readonly string[] GroupMembers = new[] { "Links", "Search" };
    private static readonly string[] TopLevelAfterGroup = new[] { "LinkEditor", "Theme", "Settings" };
    private static readonly string[] GroupedLinksFirst = new[] { "Search", "Links" };
    private static readonly string[] SearchOnly = new[] { "Search" };
    private static readonly string[] LinksOnly = new[] { "Links" };
    private static readonly string[] SkipDownOrder = new[] { "Search", "LinkEditor", "Links" };

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

    private static PageLayout GroupedSample() => new(Sample().Widgets, GroupMembers);

    [Fact]
    public void Group_TopLevelAndGrouped_SplitByMemberList()
    {
        PageLayout layout = GroupedSample();

        Assert.Equal(TopLevelAfterGroup, layout.TopLevelWidgets().Select(w => w.Id));
        Assert.Equal(GroupMembers, layout.GroupedWidgets().Select(w => w.Id));
    }

    [Fact]
    public void Group_MoveUp_ReordersWithinGroupOnly()
    {
        PageLayout layout = GroupedSample();

        Assert.Equal(GroupedLinksFirst, layout.MoveUp("Search").GroupChildren);
        Assert.Same(layout, layout.MoveDown("Search"));
    }

    [Fact]
    public void Move_TopLevel_SkipsGroupedNeighbours()
    {
        PageLayout layout = new(
            new[]
            {
                new WidgetPlacement("Search", WidgetZone.Main, true),
                new WidgetPlacement("Links", WidgetZone.Main, true),
                new WidgetPlacement("LinkEditor", WidgetZone.Main, true),
            },
            SearchOnly);

        // Links can't move up past the grouped Search: no top-level neighbour above.
        Assert.Same(layout, layout.MoveUp("Links"));

        // LinkEditor swaps with Links (both top-level), never with Search.
        Assert.Equal(
            SkipDownOrder,
            layout.MoveDown("Links").Widgets.Select(w => w.Id));
    }

    [Fact]
    public void Group_AddRemoveClear_ManageMembership()
    {
        PageLayout layout = Sample();
        PageLayout grouped = layout.AddToGroup("Links");

        Assert.Equal(LinksOnly, grouped.GroupChildren);
        Assert.Same(grouped, grouped.AddToGroup("Links"));
        Assert.Equal(GroupMembers, grouped.AddToGroup("Search").GroupChildren);

        PageLayout removed = grouped.AddToGroup("Search").RemoveFromGroup("Links");
        Assert.Single(removed.GroupChildren);
        Assert.Equal("Search", removed.GroupChildren[0]);

        // Ungrouping keeps the flat list: the widget renders at its old position.
        Assert.Equal(FullOrder, removed.Widgets.Select(w => w.Id));
        Assert.Empty(removed.ClearGroup().GroupChildren);
    }

    [Fact]
    public void SetVisible_PreservesGroupMembership()
    {
        PageLayout layout = GroupedSample();
        PageLayout hidden = layout.SetVisible("Links", false);

        Assert.Equal(GroupMembers, hidden.GroupChildren);
    }
}

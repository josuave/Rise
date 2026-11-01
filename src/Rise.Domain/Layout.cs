namespace Rise.Domain;

/// <summary>
/// The widgets on the start page. Cluster widgets render in the
/// top-right cluster; every other widget renders in the main column.
/// Order is the order within each zone, not across zones.
/// </summary>
public enum WidgetKind
{
    Search,
    Links,
    LinkEditor,
    Theme,
    Settings,
}

public enum WidgetZone
{
    Main,
    Cluster,
}

public sealed record WidgetPlacement(WidgetKind Kind, bool Visible);

/// <summary>
/// Pure layout model: an ordered list of widget placements. Moves only
/// ever swap a widget with a neighbour in its own zone, so a widget can
/// never wander into the other zone by reordering. Visibility is
/// separate from order, so hiding a widget never loses its position.
/// </summary>
public sealed record PageLayout(IReadOnlyList<WidgetPlacement> Widgets)
{
    public static PageLayout Default() => new(new[]
    {
        new WidgetPlacement(WidgetKind.Search, true),
        new WidgetPlacement(WidgetKind.Links, true),
        new WidgetPlacement(WidgetKind.LinkEditor, false),
        new WidgetPlacement(WidgetKind.Theme, true),
        new WidgetPlacement(WidgetKind.Settings, true),
    });

    public static WidgetZone ZoneOf(WidgetKind kind) =>
        kind is WidgetKind.Theme or WidgetKind.Settings ? WidgetZone.Cluster : WidgetZone.Main;

    public IReadOnlyList<WidgetPlacement> MainWidgets() =>
        Widgets.Where(w => ZoneOf(w.Kind) == WidgetZone.Main).ToList();

    public IReadOnlyList<WidgetPlacement> ClusterWidgets() =>
        Widgets.Where(w => ZoneOf(w.Kind) == WidgetZone.Cluster).ToList();

    public PageLayout MoveUp(WidgetKind kind) => Move(kind, -1);

    public PageLayout MoveDown(WidgetKind kind) => Move(kind, 1);

    private PageLayout Move(WidgetKind kind, int delta)
    {
        WidgetZone zone = ZoneOf(kind);
        int i = Widgets.ToList().FindIndex(w => w.Kind == kind);
        if (i < 0)
        {
            return this;
        }

        int j = i + delta;
        while (j >= 0 && j < Widgets.Count && ZoneOf(Widgets[j].Kind) != zone)
        {
            j += delta;
        }
        if (j < 0 || j >= Widgets.Count)
        {
            return this;
        }

        var next = Widgets.ToList();
        (next[i], next[j]) = (next[j], next[i]);
        return new PageLayout(next);
    }

    public PageLayout SetVisible(WidgetKind kind, bool visible)
    {
        var next = Widgets
            .Select(w => w.Kind == kind ? new WidgetPlacement(w.Kind, visible) : w)
            .ToList();
        return new PageLayout(next);
    }
}

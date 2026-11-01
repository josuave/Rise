namespace Rise.Domain;

/// <summary>Where a widget renders: the main column or the top-right cluster.</summary>
public enum WidgetZone
{
    Main,
    Cluster,
}

/// <summary>One widget's slot in the layout: an opaque id, its zone, and whether it shows.</summary>
public sealed record WidgetPlacement(string Id, WidgetZone Zone, bool Visible);

/// <summary>
/// Pure layout model: an ordered list of widget placements. The core
/// knows no concrete widgets — which ids exist, what they are called,
/// and their default order and visibility all come from the registered
/// widget definitions (see IWidgetDefinition), and the layout service
/// reconciles stored layouts against them. Moves only swap a widget
/// with a neighbour in its own zone, so a widget can never wander into
/// the other zone by reordering. Visibility is separate from order, so
/// hiding a widget never loses its position.
/// </summary>
public sealed record PageLayout(IReadOnlyList<WidgetPlacement> Widgets)
{
    public IReadOnlyList<WidgetPlacement> MainWidgets() =>
        Widgets.Where(w => w.Zone == WidgetZone.Main).ToList();

    public IReadOnlyList<WidgetPlacement> ClusterWidgets() =>
        Widgets.Where(w => w.Zone == WidgetZone.Cluster).ToList();

    public PageLayout MoveUp(string id) => Move(id, -1);

    public PageLayout MoveDown(string id) => Move(id, 1);

    private PageLayout Move(string id, int delta)
    {
        int i = Widgets.ToList().FindIndex(w => w.Id == id);
        if (i < 0)
        {
            return this;
        }

        WidgetZone zone = Widgets[i].Zone;
        int j = i + delta;
        while (j >= 0 && j < Widgets.Count && Widgets[j].Zone != zone)
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

    public PageLayout SetVisible(string id, bool visible)
    {
        var next = Widgets
            .Select(w => w.Id == id ? new WidgetPlacement(w.Id, w.Zone, visible) : w)
            .ToList();
        return new PageLayout(next);
    }
}

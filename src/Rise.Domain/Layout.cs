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
/// Pure layout model: an ordered list of widget placements, plus the
/// group overlay: the ids of the widgets contained in the page's group,
/// in the order they render inside it. Grouping is deliberately NOT a
/// property on placements — every widget would carry a field that only
/// matters to grouped ones. Instead the layout carries the group's
/// member list, and grouped widgets stay in the flat list too, so
/// ungrouping one loses nothing: it renders at its flat position again.
/// The core knows no concrete widgets — which ids exist, what they are
/// called, and their default order and visibility all come from the
/// registered widget definitions (see IWidgetDefinition), and the
/// layout service reconciles stored layouts against them. Moves only
/// swap a widget with a neighbour in its own zone and container, so a
/// widget can never wander into the other zone, or across a group
/// boundary, by reordering. Visibility is separate from order, so
/// hiding a widget never loses its position.
/// </summary>
public sealed record PageLayout(IReadOnlyList<WidgetPlacement> Widgets, IReadOnlyList<string> GroupChildren)
{
    public PageLayout(IReadOnlyList<WidgetPlacement> widgets)
        : this(widgets, Array.Empty<string>())
    {
    }

    /// <summary>Placements not inside the group, in flat order: what the shell and editor render as top-level rows.</summary>
    public IReadOnlyList<WidgetPlacement> TopLevelWidgets() =>
        Widgets.Where(w => !GroupChildren.Contains(w.Id)).ToList();

    /// <summary>Grouped placements, in group order: what renders inside the group shell.</summary>
    public IReadOnlyList<WidgetPlacement> GroupedWidgets() =>
        GroupChildren.Join(Widgets, id => id, w => w.Id, (_, w) => w).ToList();

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

        // A grouped widget moves within the group, never in the flat
        // list: its render order is the group list's order.
        if (GroupChildren.Contains(id))
        {
            var children = GroupChildren.ToList();
            int ci = children.IndexOf(id);
            int cj = ci + delta;
            if (cj < 0 || cj >= children.Count)
            {
                return this;
            }

            (children[ci], children[cj]) = (children[cj], children[ci]);
            return new PageLayout(Widgets, children);
        }

        // A top-level widget moves past top-level neighbours only:
        // grouped widgets are invisible at top level, so swapping with
        // one would be a button that appears to do nothing.
        WidgetZone zone = Widgets[i].Zone;
        int j = i + delta;
        while (j >= 0 && j < Widgets.Count && (Widgets[j].Zone != zone || GroupChildren.Contains(Widgets[j].Id)))
        {
            j += delta;
        }
        if (j < 0 || j >= Widgets.Count)
        {
            return this;
        }

        var next = Widgets.ToList();
        (next[i], next[j]) = (next[j], next[i]);
        return new PageLayout(next, GroupChildren);
    }

    public PageLayout SetVisible(string id, bool visible)
    {
        var next = Widgets
            .Select(w => w.Id == id ? new WidgetPlacement(w.Id, w.Zone, visible) : w)
            .ToList();
        return new PageLayout(next, GroupChildren);
    }

    /// <summary>
    /// Applies a named arrangement: the listed widgets take the given
    /// visibility and move to the front in the listed order, and any
    /// grouping is cleared — an arrangement is a flat board, and a
    /// grouped widget would otherwise stay hidden inside a group the
    /// arrangement hides. Widgets the arrangement doesn't list keep
    /// their visibility and their relative order behind the listed
    /// ones, so cluster widgets and future widgets are untouched.
    /// Ids the arrangement lists but the layout doesn't have are
    /// ignored: this never invents placements.
    /// </summary>
    public PageLayout ApplyArrangement(IReadOnlyList<string> orderedIds, IReadOnlySet<string> visibleIds)
    {
        var order = orderedIds
            .Select((id, index) => (id, index))
            .GroupBy(pair => pair.id)
            .ToDictionary(group => group.Key, group => group.First().index);
        var next = Widgets
            .Select(w => order.ContainsKey(w.Id)
                ? new WidgetPlacement(w.Id, w.Zone, visibleIds.Contains(w.Id))
                : w)
            .OrderBy(w => order.TryGetValue(w.Id, out int index) ? index : int.MaxValue)
            .ToList();
        return new PageLayout(next, Array.Empty<string>());
    }

    public PageLayout AddToGroup(string id) =>
        GroupChildren.Contains(id)
            ? this
            : new PageLayout(Widgets, GroupChildren.Append(id).ToList());

    public PageLayout RemoveFromGroup(string id) =>
        GroupChildren.Contains(id)
            ? new PageLayout(Widgets, GroupChildren.Where(c => c != id).ToList())
            : this;

    public PageLayout ClearGroup() =>
        GroupChildren.Count == 0 ? this : new PageLayout(Widgets, Array.Empty<string>());
}

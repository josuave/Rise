using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for layouts: load the stored collection reconciled
/// against the registered widget definitions (or the default collection
/// when nothing is stored), apply one operation, persist the result,
/// and notify listeners (the shell page re-renders when another widget
/// changes the layout). The single-layout operations (move, hide,
/// reset, show/hide widget) always act on the active layout; the
/// collection operations manage the saved set itself.
/// </summary>
public sealed class LayoutService(ILayoutCollectionStore store, IEnumerable<IWidgetDefinition> definitions)
{
    private readonly IReadOnlyList<IWidgetDefinition> _definitions =
        definitions.OrderBy(d => d.Order).ToList();

    public event EventHandler? Changed;

    public async Task<LayoutCollection> LoadCollectionAsync(CancellationToken cancellationToken = default)
    {
        LayoutCollection? stored = await store.LoadAsync(cancellationToken);
        return Reconcile(stored);
    }

    /// <summary>The active layout: what the shell renders.</summary>
    public async Task<PageLayout> LoadAsync(CancellationToken cancellationToken = default)
    {
        LayoutCollection collection = await LoadCollectionAsync(cancellationToken);
        return collection.Active?.Layout ?? DefaultPlacements();
    }

    /// <summary>
    /// Brings a stored collection up to date with the registered
    /// widgets: per layout, unknown widget ids are dropped, zones come
    /// from the definitions, and visibility is kept; widgets a layout
    /// predates are appended with defaults. Blank layout names become
    /// "Untitled", duplicate names are made unique, and the default
    /// layout is always present; the active id falls back to it when
    /// it points nowhere. A null stored collection reconciles to a
    /// collection holding just the default layout.
    /// </summary>
    private LayoutCollection Reconcile(LayoutCollection? stored)
    {
        var layouts = new List<NamedLayout>();
        if (stored is not null)
        {
            foreach (NamedLayout saved in stored.Layouts)
            {
                string name = saved.Name.Trim();
                PageLayout layout = ReconcilePlacements(saved.Layout);
                layouts.Add(new NamedLayout(saved.Id, name.Length == 0 ? "Untitled" : name, layout));
            }

            // Layout ids are unique by construction; keep the first of any duplicates.
            layouts = layouts
                .GroupBy(l => l.Id)
                .Select(group => group.First())
                .ToList();

            // Names are unique in a collection; later duplicates get a suffix.
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            layouts = layouts
                .Select(l =>
                {
                    string unique = l.Name;
                    int suffix = 2;
                    while (!taken.Add(unique))
                    {
                        unique = $"{l.Name} {suffix}";
                        suffix++;
                    }

                    return new NamedLayout(l.Id, unique, l.Layout);
                })
                .ToList();
        }

        if (!layouts.Any(l => l.Id == LayoutCollection.DefaultId))
        {
            layouts.Insert(0, new NamedLayout(LayoutCollection.DefaultId, LayoutCollection.DefaultName, DefaultPlacements()));
        }

        string activeId = stored is not null && layouts.Any(l => l.Id == stored.ActiveId)
            ? stored.ActiveId
            : LayoutCollection.DefaultId;
        return new LayoutCollection(activeId, layouts);
    }

    private PageLayout ReconcilePlacements(PageLayout saved)
    {
        // Join keeps stored order and visibility, drops unknown ids,
        // and takes zones from the definitions. Stored layouts are
        // deduped by the store, so widget ids are unique here.
        var placements = saved.Widgets
            .Join(
                _definitions,
                placement => placement.Id,
                definition => definition.Id,
                (placement, definition) => new WidgetPlacement(definition.Id, definition.Zone, placement.Visible))
            .ToList();

        var known = placements.Select(w => w.Id).ToHashSet();
        placements.AddRange(_definitions
            .Where(definition => !known.Contains(definition.Id))
            .Select(definition => new WidgetPlacement(definition.Id, definition.Zone, definition.DefaultVisible)));

        return new PageLayout(placements);
    }

    private PageLayout DefaultPlacements() => new(_definitions
        .Select(definition => new WidgetPlacement(definition.Id, definition.Zone, definition.DefaultVisible))
        .ToList());

    public async Task<PageLayout> MoveUpAsync(PageLayout layout, string id, CancellationToken cancellationToken = default) =>
        await ApplyToActiveAsync(layout.MoveUp(id), cancellationToken);

    public async Task<PageLayout> MoveDownAsync(PageLayout layout, string id, CancellationToken cancellationToken = default) =>
        await ApplyToActiveAsync(layout.MoveDown(id), cancellationToken);

    public async Task<PageLayout> SetVisibleAsync(PageLayout layout, string id, bool visible, CancellationToken cancellationToken = default) =>
        await ApplyToActiveAsync(layout.SetVisible(id, visible), cancellationToken);

    public async Task<PageLayout> ResetAsync(CancellationToken cancellationToken = default)
    {
        LayoutCollection collection = await LoadCollectionAsync(cancellationToken);
        return await ApplyToActiveAsync(DefaultPlacements(), cancellationToken, collection);
    }

    /// <summary>
    /// Convenience for widgets that don't own the layout: show (or hide)
    /// one widget in the active layout, whatever it currently is.
    /// </summary>
    public async Task<PageLayout> ShowWidgetAsync(string id, CancellationToken cancellationToken = default)
    {
        PageLayout layout = await LoadAsync(cancellationToken);
        return await SetVisibleAsync(layout, id, true, cancellationToken);
    }

    public async Task<PageLayout> HideWidgetAsync(string id, CancellationToken cancellationToken = default)
    {
        PageLayout layout = await LoadAsync(cancellationToken);
        return await SetVisibleAsync(layout, id, false, cancellationToken);
    }

    public async Task<LayoutCollection> SetActiveLayoutAsync(string id, CancellationToken cancellationToken = default)
    {
        LayoutCollection collection = await LoadCollectionAsync(cancellationToken);
        return await ApplyCollectionAsync(collection.SetActive(id), cancellationToken);
    }

    /// <summary>Saves the active layout's current arrangement under a new name and switches to it.</summary>
    public async Task<LayoutCollection> SaveAsAsync(string name, CancellationToken cancellationToken = default)
    {
        LayoutCollection collection = await LoadCollectionAsync(cancellationToken);
        PageLayout active = collection.Active?.Layout ?? DefaultPlacements();
        return await ApplyCollectionAsync(collection.SaveAs(name, active), cancellationToken);
    }

    public async Task<LayoutCollection> RenameLayoutAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        LayoutCollection collection = await LoadCollectionAsync(cancellationToken);
        return await ApplyCollectionAsync(collection.Rename(id, name), cancellationToken);
    }

    public async Task<LayoutCollection> DeleteLayoutAsync(string id, CancellationToken cancellationToken = default)
    {
        LayoutCollection collection = await LoadCollectionAsync(cancellationToken);
        return await ApplyCollectionAsync(collection.Delete(id), cancellationToken);
    }

    private async Task<PageLayout> ApplyToActiveAsync(PageLayout next, CancellationToken cancellationToken, LayoutCollection? collection = null)
    {
        collection ??= await LoadCollectionAsync(cancellationToken);
        await ApplyCollectionAsync(collection.UpdateActive(next), cancellationToken);
        return next;
    }

    private async Task<LayoutCollection> ApplyCollectionAsync(LayoutCollection next, CancellationToken cancellationToken)
    {
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for the layout: load the stored layout reconciled
/// against the registered widget definitions (or the default layout
/// when nothing is stored), apply one pure layout operation, persist
/// the result, and notify listeners (the shell page re-renders when
/// another widget changes the layout).
/// </summary>
public sealed class LayoutService(ILayoutStore store, IEnumerable<IWidgetDefinition> definitions)
{
    private readonly IReadOnlyList<IWidgetDefinition> _definitions =
        definitions.OrderBy(d => d.Order).ToList();

    public event EventHandler? Changed;

    public async Task<PageLayout> LoadAsync(CancellationToken cancellationToken = default)
    {
        PageLayout? stored = await store.LoadAsync(cancellationToken);
        return Reconcile(stored);
    }

    /// <summary>
    /// Brings a stored layout up to date with the registered widgets:
    /// unknown ids (removed widgets) are dropped, zones always come
    /// from the definition (so a widget that moves zones is not stuck
    /// in the old one), visibility is kept, and widgets the stored
    /// layout predates are appended with their defaults. A null stored
    /// layout reconciles to the default layout.
    /// </summary>
    private PageLayout Reconcile(PageLayout? stored)
    {
        // Join keeps stored order and visibility, drops unknown ids,
        // and takes zones from the definitions. Stored layouts are
        // deduped by the store, so ids are unique here.
        var result = (stored is null
                ? Enumerable.Empty<WidgetPlacement>()
                : stored.Widgets)
            .Join(
                _definitions,
                placement => placement.Id,
                definition => definition.Id,
                (placement, definition) => new WidgetPlacement(definition.Id, definition.Zone, placement.Visible))
            .ToList();

        // Widgets the stored layout predates are appended with defaults.
        var known = result.Select(w => w.Id).ToHashSet();
        result.AddRange(_definitions
            .Where(definition => !known.Contains(definition.Id))
            .Select(definition => new WidgetPlacement(definition.Id, definition.Zone, definition.DefaultVisible)));

        return new PageLayout(result);
    }

    public async Task<PageLayout> MoveUpAsync(PageLayout layout, string id, CancellationToken cancellationToken = default) =>
        await ApplyAsync(layout.MoveUp(id), cancellationToken);

    public async Task<PageLayout> MoveDownAsync(PageLayout layout, string id, CancellationToken cancellationToken = default) =>
        await ApplyAsync(layout.MoveDown(id), cancellationToken);

    public async Task<PageLayout> SetVisibleAsync(PageLayout layout, string id, bool visible, CancellationToken cancellationToken = default) =>
        await ApplyAsync(layout.SetVisible(id, visible), cancellationToken);

    public async Task<PageLayout> ResetAsync(CancellationToken cancellationToken = default) =>
        await ApplyAsync(Reconcile(null), cancellationToken);

    /// <summary>
    /// Convenience for widgets that don't own the layout: show (or hide)
    /// one widget, whatever the current layout is.
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

    private async Task<PageLayout> ApplyAsync(PageLayout next, CancellationToken cancellationToken)
    {
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

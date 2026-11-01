using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for the layout: load the stored layout (or the
/// default when nothing is stored), apply one pure layout operation,
/// persist the result, and notify listeners (the shell page re-renders
/// when another widget changes the layout).
/// </summary>
public sealed class LayoutService(ILayoutStore store)
{
    public event EventHandler? Changed;

    public async Task<PageLayout> LoadAsync(CancellationToken cancellationToken = default) =>
        await store.LoadAsync(cancellationToken) ?? PageLayout.Default();

    public async Task<PageLayout> MoveUpAsync(PageLayout layout, WidgetKind kind, CancellationToken cancellationToken = default) =>
        await ApplyAsync(layout.MoveUp(kind), cancellationToken);

    public async Task<PageLayout> MoveDownAsync(PageLayout layout, WidgetKind kind, CancellationToken cancellationToken = default) =>
        await ApplyAsync(layout.MoveDown(kind), cancellationToken);

    public async Task<PageLayout> SetVisibleAsync(PageLayout layout, WidgetKind kind, bool visible, CancellationToken cancellationToken = default) =>
        await ApplyAsync(layout.SetVisible(kind, visible), cancellationToken);

    public async Task<PageLayout> ResetAsync(CancellationToken cancellationToken = default) =>
        await ApplyAsync(PageLayout.Default(), cancellationToken);

    /// <summary>
    /// Convenience for widgets that don't own the layout: show (or hide)
    /// one widget, whatever the current layout is.
    /// </summary>
    public async Task<PageLayout> ShowWidgetAsync(WidgetKind kind, CancellationToken cancellationToken = default)
    {
        PageLayout layout = await LoadAsync(cancellationToken);
        return await SetVisibleAsync(layout, kind, true, cancellationToken);
    }

    public async Task<PageLayout> HideWidgetAsync(WidgetKind kind, CancellationToken cancellationToken = default)
    {
        PageLayout layout = await LoadAsync(cancellationToken);
        return await SetVisibleAsync(layout, kind, false, cancellationToken);
    }

    private async Task<PageLayout> ApplyAsync(PageLayout next, CancellationToken cancellationToken)
    {
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

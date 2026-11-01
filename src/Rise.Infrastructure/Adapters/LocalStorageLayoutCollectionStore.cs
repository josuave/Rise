using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists the set of saved layouts in the browser's
/// localStorage under "rise.layouts.v1", through the same riseStorage
/// helper the link store uses. Only layout ids, names, widget ids, and
/// visibility are stored — zones come from the definitions, so this
/// adapter never needs to know them. Zones here are placeholders
/// (Main); the layout service reconciles them against the registered
/// definitions on load. Corrupt or unavailable storage returns null
/// and the caller falls back to the default collection.
/// </summary>
public sealed class LocalStorageLayoutCollectionStore(IJSRuntime js) : ILayoutCollectionStore
{
    private const string StorageKey = "rise.layouts.v1";

    private sealed record StoredWidget(string Id, bool Visible);

    private sealed record StoredLayout(string Id, string Name, List<StoredWidget> Widgets);

    private sealed record StoredCollection(string ActiveId, List<StoredLayout> Layouts);

    public async Task<LayoutCollection?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            StoredCollection? parsed = JsonSerializer.Deserialize<StoredCollection>(stored);
            if (parsed is null || parsed.Layouts.Count == 0)
            {
                return null;
            }

            var layouts = parsed.Layouts
                .Where(layout => !string.IsNullOrWhiteSpace(layout.Id))
                .GroupBy(layout => layout.Id)
                .Select(group => group.First())
                .Select(layout => new NamedLayout(
                    layout.Id,
                    layout.Name,
                    new PageLayout(layout.Widgets
                        .Where(widget => !string.IsNullOrWhiteSpace(widget.Id))
                        .GroupBy(widget => widget.Id)
                        .Select(widgetGroup => new WidgetPlacement(widgetGroup.Key, WidgetZone.Main, widgetGroup.First().Visible))
                        .ToList())))
                .ToList();

            return layouts.Count == 0 ? null : new LayoutCollection(parsed.ActiveId, layouts);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Corrupt or unavailable storage: the caller falls back to defaults rather than breaking the page.
            return null;
        }
    }

    public async Task SaveAsync(LayoutCollection collection, CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = new StoredCollection(
                collection.ActiveId,
                collection.Layouts
                    .Select(layout => new StoredLayout(
                        layout.Id,
                        layout.Name,
                        layout.Layout.Widgets
                            .Select(widget => new StoredWidget(widget.Id, widget.Visible))
                            .ToList()))
                    .ToList());
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

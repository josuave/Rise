using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists the widget layout in the browser's
/// localStorage under "rise.layout.v1", through the same riseStorage
/// helper the link store uses. Only widget ids and visibility are
/// stored — zones come from the definitions, so this adapter never
/// needs to know them. Zones here are placeholders (Main); the layout
/// service reconciles them against the registered definitions on load.
/// Corrupt or unavailable storage returns null and the caller falls
/// back to defaults.
/// </summary>
public sealed class LocalStorageLayoutStore(IJSRuntime js) : ILayoutStore
{
    private const string StorageKey = "rise.layout.v1";

    private sealed record StoredWidget(string Id, bool Visible);

    public async Task<PageLayout?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            List<StoredWidget>? parsed = JsonSerializer.Deserialize<List<StoredWidget>>(stored);
            if (parsed is null || parsed.Count == 0)
            {
                return null;
            }

            // Blank ids are skipped; duplicates keep the first occurrence.
            var placements = parsed
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id)
                .Select(group => new WidgetPlacement(group.Key, WidgetZone.Main, group.First().Visible))
                .ToList();

            return placements.Count == 0 ? null : new PageLayout(placements);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Corrupt or unavailable storage: the caller falls back to defaults rather than breaking the page.
            return null;
        }
    }

    public async Task SaveAsync(PageLayout layout, CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = layout.Widgets
                .Select(w => new StoredWidget(w.Id, w.Visible))
                .ToList();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists the widget layout in the browser's
/// localStorage under "rise.layout.v1", through the same riseStorage
/// helper the link store uses. Unknown widget kinds in stored data are
/// skipped, and kinds added by a later version are appended with their
/// default visibility, so an old layout never breaks a new release.
/// </summary>
public sealed class LocalStorageLayoutStore(IJSRuntime js) : ILayoutStore
{
    private const string StorageKey = "rise.layout.v1";

    private sealed record StoredWidget(string Kind, bool Visible);

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

            var placements = new List<WidgetPlacement>();
            foreach (StoredWidget item in parsed)
            {
                if (Enum.TryParse<WidgetKind>(item.Kind, out WidgetKind kind) &&
                    !placements.Any(w => w.Kind == kind))
                {
                    placements.Add(new WidgetPlacement(kind, item.Visible));
                }
            }

            // Kinds a newer version added that this layout predates.
            foreach (WidgetPlacement fallback in PageLayout.Default().Widgets)
            {
                if (!placements.Any(w => w.Kind == fallback.Kind))
                {
                    placements.Add(fallback);
                }
            }

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
                .Select(w => new StoredWidget(w.Kind.ToString(), w.Visible))
                .ToList();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

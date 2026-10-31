using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists a start page in the browser's localStorage.
/// Storage key is unchanged from the Calm release ("rise.quicklinks.v1") so existing links survive this refactor.
/// </summary>
public sealed class LocalStorageLinkStore(IJSRuntime js) : ILinkStore
{
    private const string StorageKey = "rise.quicklinks.v1";

    // Stored shape: flat records, so the domain types (LinkUrl etc.) never leak into the JSON.
    private sealed record StoredLink(Guid Id, string Name, string Url);

    public async Task<StartPage?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            List<StoredLink>? parsed = JsonSerializer.Deserialize<List<StoredLink>>(stored);
            if (parsed is null)
            {
                return null;
            }

            var links = new List<Link>();
            foreach (StoredLink item in parsed)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }
                if (!LinkUrl.TryCreate(item.Url, out LinkUrl? url, out _) || url is null)
                {
                    continue;
                }
                links.Add(new Link(item.Id == Guid.Empty ? Guid.NewGuid() : item.Id, item.Name.Trim(), url));
            }
            return new StartPage(links);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Corrupt or unavailable storage: the caller falls back to defaults rather than breaking the page.
            return null;
        }
    }

    public async Task SaveAsync(StartPage page, CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = page.Links.Select(l => new StoredLink(l.Id, l.Name, l.Url.Value)).ToList();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists a start page in the browser's localStorage.
/// Storage key is unchanged from the Calm release ("rise.quicklinks.v1") so existing links survive this refactor.
/// The stored shape is a compatible extension of the old Id/Name/Url shape:
/// the new members are optional, so a file written before them still
/// deserialises — old entries load with no description or tags, no icon,
/// unpinned, zero clicks, and a creation time of "now" (their real
/// creation time was never recorded). New saves write every field.
/// </summary>
public sealed class LocalStorageLinkStore(IJSRuntime js) : ILinkStore
{
    private const string StorageKey = "rise.quicklinks.v1";

    // Stored shape: flat records, so the domain types (LinkUrl etc.) never leak into the JSON.
    // The new members carry defaults, so JSON written by the old shape (Id/Name/Url only) still deserialises.
    private sealed record StoredLink(
        Guid Id,
        string Name,
        string Url,
        string? Description = null,
        IReadOnlyList<string>? Tags = null,
        string? IconUrl = null,
        bool IsPinned = false,
        long ClickCount = 0,
        DateTimeOffset? CreatedAt = null,
        DateTimeOffset? LastAccessedAt = null);

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
                // An old entry has no creation time: stamp load time.
                // (The Link constructor also stamps UtcNow for a default
                // creation time, so this is the same rule stated once here.)
                DateTimeOffset createdAt = item.CreatedAt is { } storedAt && storedAt != default ? storedAt : DateTimeOffset.UtcNow;
                links.Add(new Link(
                    item.Id == Guid.Empty ? Guid.NewGuid() : item.Id,
                    item.Name.Trim(),
                    url,
                    item.Description,
                    item.Tags,
                    item.IconUrl,
                    item.IsPinned,
                    item.ClickCount,
                    createdAt,
                    item.LastAccessedAt));
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
            var stored = page.Links.Select(l => new StoredLink(
                l.Id,
                l.Name,
                l.Url.Value,
                l.Description,
                l.Tags,
                l.IconUrl,
                l.IsPinned,
                l.ClickCount,
                l.CreatedAt,
                l.LastAccessedAt)).ToList();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

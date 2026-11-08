using System.Diagnostics.CodeAnalysis;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for links: the only place that talks to the store.
/// It loads a page, hands it to the pure core (StartPage.AddLink/RemoveLink),
/// and persists whatever the core returns. It contains no link rules itself.
/// </summary>
public sealed class StartPageService(ILinkStore store)
{
    /// <summary>
    /// Raised after any successful save, so tiles and editor widgets that
    /// render the same page can reload instead of going stale.
    /// </summary>
    public event EventHandler? Changed;

    public async Task<StartPage> LoadAsync(CancellationToken cancellationToken = default) =>
        await store.LoadAsync(cancellationToken) ?? StartPage.Default();

    // CA1054 does not fit here either: this only forwards unvalidated user
    // input to StartPage.AddLink, which validates it (see its suppression).
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Forwards unvalidated user input to StartPage.AddLink for validation.")]
    public async Task<Result<StartPage>> AddLinkAsync(StartPage page, string name, string url, CancellationToken cancellationToken = default) =>
        await AddLinkAsync(page, name, url, null, null, false, null, cancellationToken);

    // CA1054 does not fit here either: see AddLinkAsync above.
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Forwards unvalidated user input to StartPage.AddLink for validation.")]
    public async Task<Result<StartPage>> AddLinkAsync(StartPage page, string name, string urlInput, string? description, IReadOnlyList<string>? tags, bool isPinned, string? iconAddress = null, CancellationToken cancellationToken = default)
    {
        Result<StartPage> result = page.AddLink(name, urlInput, description, tags, isPinned, iconAddress);
        switch (result)
        {
            case Ok<StartPage> ok:
                await store.SaveAsync(ok.Value, cancellationToken);
                Changed?.Invoke(this, EventArgs.Empty);
                break;
            case Error:
            case null:
                break;
        }

        return result;
    }

    /// <summary>Edits a link in place (same id, clicks, and creation time); persists and raises <see cref="Changed"/> on success only.</summary>
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Forwards unvalidated user input to StartPage.UpdateLink for validation.")]
    public async Task<Result<StartPage>> UpdateLinkAsync(StartPage page, Guid id, string name, string urlInput, string? description, IReadOnlyList<string>? tags, bool isPinned, string? iconAddress = null, CancellationToken cancellationToken = default)
    {
        Result<StartPage> result = page.UpdateLink(id, name, urlInput, description, tags, isPinned, iconAddress);
        switch (result)
        {
            case Ok<StartPage> ok:
                await store.SaveAsync(ok.Value, cancellationToken);
                Changed?.Invoke(this, EventArgs.Empty);
                break;
            case Error:
            case null:
                break;
        }

        return result;
    }

    /// <summary>
    /// Merges already-built links (e.g. parsed from a bookmark file)
    /// into the page, preserving each link's id, tags, notes, and
    /// creation time — <see cref="AddLinkAsync"/> cannot do that, it
    /// always stamps a fresh id and "now". A link whose address is
    /// already on the page (case-insensitive), or that duplicates an
    /// earlier link in the same batch, is skipped, never added twice.
    /// Saves once and raises <see cref="Changed"/> once, only when at
    /// least one link lands.
    /// </summary>
    public async Task<(StartPage Page, int Added, int Skipped)> ImportLinksAsync(StartPage page, IEnumerable<Link> links, CancellationToken cancellationToken = default)
    {
        HashSet<string> seen = new(page.Links.Select(l => l.Url.Value), StringComparer.OrdinalIgnoreCase);
        List<Link> added = new();
        int skipped = 0;
        foreach (Link link in links)
        {
            if (!seen.Add(link.Url.Value))
            {
                skipped++;
                continue;
            }

            added.Add(link);
        }

        if (added.Count == 0)
        {
            return (page, 0, skipped);
        }

        StartPage next = new([.. page.Links, .. added]);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return (next, added.Count, skipped);
    }

    /// <summary>Records a click on a link (count + 1, last-accessed stamped); persists and raises <see cref="Changed"/>.</summary>
    public async Task<StartPage> RecordClickAsync(StartPage page, Guid id, CancellationToken cancellationToken = default)
    {
        StartPage next = page.RecordClick(id);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }

    /// <summary>Saves the default page, discarding any saved links.</summary>
    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(StartPage.Default(), cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<StartPage> RemoveLinkAsync(StartPage page, Guid id, CancellationToken cancellationToken = default)
    {
        StartPage next = page.RemoveLink(id);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

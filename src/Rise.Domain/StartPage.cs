using System.Diagnostics.CodeAnalysis;

namespace Rise.Domain;

/// <summary>
/// The start page aggregate. Pure functional core: every operation takes a page
/// and returns a new page (or a failure) — it never touches storage, the browser, or the clock,
/// with one documented exception: <see cref="RecordClick"/> stamps
/// <see cref="Link.LastAccessedAt"/> with <see cref="DateTimeOffset.UtcNow"/>,
/// because a click time has to come from somewhere and the shell must not
/// be the one deciding what "now" a click means.
/// The imperative shell (application services) is what persists the returned page.
/// </summary>
public sealed record StartPage(IReadOnlyList<Link> Links)
{
    public static StartPage Default() => new(new[]
    {
        Link.Create("Google", new LinkUrl("https://www.google.com/")),
        Link.Create("GitHub", new LinkUrl("https://github.com/")),
        Link.Create("YouTube", new LinkUrl("https://www.youtube.com/")),
        Link.Create("Wikipedia", new LinkUrl("https://www.wikipedia.org/")),
    });

    // CA1054 does not fit here: the parameter is unvalidated user input.
    // LinkUrl.TryCreate validates it and reports failures via Result;
    // a Uri parameter could not represent invalid input at all.
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Parameter is unvalidated user input validated by LinkUrl.TryCreate.")]
    public Result<StartPage> AddLink(string name, string urlInput) =>
        AddLink(name, urlInput, null, null, false);

    // CA1054 does not fit here either: see AddLink above.
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Parameter is unvalidated user input validated by LinkUrl.TryCreate.")]
    public Result<StartPage> AddLink(string name, string urlInput, string? description, IReadOnlyList<string>? tags, bool isPinned, string? iconAddress = null)
    {
        if (!TryValidate(name, urlInput, null, out LinkUrl? url, out string? error) || url is null)
        {
            return Result<StartPage>.Failure(error ?? "That doesn't look like a valid web address.");
        }

        return Result<StartPage>.Success(new StartPage([..Links, Link.Create(name, url, description, tags, isPinned, iconAddress)]));
    }

    // CA1054 does not fit here either: see AddLink above.
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Parameter is unvalidated user input validated by LinkUrl.TryCreate.")]
    public Result<StartPage> UpdateLink(Guid id, string name, string urlInput, string? description, IReadOnlyList<string>? tags, bool isPinned, string? iconAddress = null)
    {
        Link? existing = Links.FirstOrDefault(l => l.Id == id);
        if (existing is null)
        {
            return Result<StartPage>.Failure("That link isn't on the page anymore.");
        }

        if (!TryValidate(name, urlInput, id, out LinkUrl? url, out string? error) || url is null)
        {
            return Result<StartPage>.Failure(error ?? "That doesn't look like a valid web address.");
        }

        // A null icon address keeps the link's current icon; a blank one
        // clears it; anything else replaces it. (An update that did not
        // mean to touch the icon must not wipe it, and there is no other
        // "leave it alone" value for a string.)
        string? nextIconUrl = iconAddress is null ? existing.IconUrl : iconAddress;
        Link updated = new(
            existing.Id,
            name,
            url,
            description,
            tags,
            nextIconUrl,
            isPinned,
            existing.ClickCount,
            existing.CreatedAt,
            existing.LastAccessedAt);
        var links = Links.Select(l => l.Id == id ? updated : l).ToList();
        return Result<StartPage>.Success(new StartPage(links));
    }

    /// <summary>
    /// Records a click: the link's click count goes up by one and its
    /// last-accessed time is stamped with the current UTC time. The one
    /// impure spot in this core (see the class summary). Clicking a link
    /// that is not on the page leaves the page unchanged.
    /// </summary>
    public StartPage RecordClick(Guid id) =>
        new(Links.Select(l => l.Id == id
            ? l with { ClickCount = l.ClickCount + 1, LastAccessedAt = DateTimeOffset.UtcNow }
            : l).ToList());

    public StartPage RemoveLink(Guid id) =>
        new(Links.Where(l => l.Id != id).ToList());

    /// <summary>
    /// The links in the requested order. Pinned links come first in
    /// every sort. The remaining order is the sort's: stored order for
    /// <see cref="LinkSort.Custom"/>, click count descending for
    /// <see cref="LinkSort.Frequency"/>, name ordinal-ignore-case for
    /// <see cref="LinkSort.Alphabetical"/>, creation time descending for
    /// <see cref="LinkSort.DateAdded"/>. Ties keep stored order (the
    /// sorts are stable). The stored order itself is never rewritten.
    /// </summary>
    public IReadOnlyList<Link> Sorted(LinkSort sort)
    {
        IEnumerable<Link> ordered = sort switch
        {
            LinkSort.Frequency => Links.OrderByDescending(l => l.ClickCount),
            LinkSort.Alphabetical => Links.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase),
            LinkSort.DateAdded => Links.OrderByDescending(l => l.CreatedAt),
            LinkSort.Custom => Links,
            // An unnamed value (e.g. a cast) has no defined order: stored order.
            _ => Links,
        };
        return ordered.OrderByDescending(l => l.IsPinned).ToList();
    }

    private bool TryValidate(string name, string urlInput, Guid? excludeId, out LinkUrl? url, out string? error)
    {
        url = null;
        error = null;
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Give the link a name and a URL.";
            return false;
        }

        if (!LinkUrl.TryCreate(urlInput, out url, out error) || url is null)
        {
            return false;
        }

        // An out parameter cannot be captured by the lambda below, so copy it to a local first.
        LinkUrl validated = url;
        if (Links.Any(l => l.Id != excludeId && string.Equals(l.Url.Value, validated.Value, StringComparison.OrdinalIgnoreCase)))
        {
            error = "That link is already on the page.";
            return false;
        }

        return true;
    }
}

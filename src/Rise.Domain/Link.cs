using System.Diagnostics.CodeAnalysis;

namespace Rise.Domain;

/// <summary>
/// A start-page link. An entity: it has an identity (Id) and can be edited or removed.
/// Tags keep the casing the user typed (for display) and are compared
/// case-insensitively when filtering; on the way in they are trimmed,
/// empties are dropped, and duplicates that differ only by casing are
/// collapsed to the first spelling. <see cref="Description"/> is plain
/// text (a note); blank or whitespace-only descriptions read as null.
/// <see cref="CreatedAt"/> is set to the current UTC time when a link
/// is created without one; <see cref="LastAccessedAt"/> is set by
/// <see cref="StartPage.RecordClick"/>.
/// </summary>
// CA1056 does not fit here: IconUrl is a user-supplied address stored
// as a string (a favicon or custom icon URL), validated where it is
// used, not a Uri the domain can construct.
[SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "IconUrl is a user-supplied icon address stored as a string; the link URL itself is the validated LinkUrl value object.")]
public sealed record Link
{
    public Link(
        Guid id,
        string name,
        LinkUrl url,
        string? description = null,
        IReadOnlyList<string>? tags = null,
        string? iconAddress = null,
        bool isPinned = false,
        long clickCount = 0,
        DateTimeOffset createdAt = default,
        DateTimeOffset? lastAccessedAt = null)
    {
        Id = id;
        Name = name.Trim();
        Url = url;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Tags = NormalizeTags(tags);
        IconUrl = string.IsNullOrWhiteSpace(iconAddress) ? null : iconAddress.Trim();
        IsPinned = isPinned;
        ClickCount = clickCount < 0 ? 0 : clickCount;
        CreatedAt = createdAt == default ? DateTimeOffset.UtcNow : createdAt;
        LastAccessedAt = lastAccessedAt;
    }

    public Guid Id { get; init; }

    public string Name { get; init; }

    public LinkUrl Url { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<string> Tags { get; init; }

    public string? IconUrl { get; init; }

    public bool IsPinned { get; init; }

    public long ClickCount { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastAccessedAt { get; init; }

    /// <summary>Creates a link with a fresh id and the current UTC time as its creation time.</summary>
    public static Link Create(
        string name,
        LinkUrl url,
        string? description = null,
        IReadOnlyList<string>? tags = null,
        bool isPinned = false,
        string? iconAddress = null) =>
        new(Guid.NewGuid(), name, url, description, tags, iconAddress, isPinned);

    /// <summary>Trims tags, drops empties, and collapses duplicates that differ only by casing (first spelling wins).</summary>
    internal static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string tag in tags)
        {
            string trimmed = tag.Trim();
            if (trimmed.Length == 0 || !seen.Add(trimmed))
            {
                continue;
            }

            normalized.Add(trimmed);
        }

        return normalized;
    }

    /// <summary>Pure display helper: initials from the name, e.g. "GitHub" -> "GI".</summary>
    public string Initials
    {
        get
        {
            string[] words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return "?";
            }
            if (words.Length == 1)
            {
                return words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant();
            }
            return string.Concat(words[0][..1], words[1][..1]).ToUpperInvariant();
        }
    }
}

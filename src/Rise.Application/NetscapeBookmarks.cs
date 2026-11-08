using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Rise.Domain;

namespace Rise.Application;

/// <summary>
/// One bookmark parsed out of a Netscape bookmark file: the title,
/// the validated address, the optional note, the tags (the link's own
/// <c>TAGS</c> attribute plus every folder name enclosing it), and the
/// creation time from <c>ADD_DATE</c> (or the parse time when the file
/// carries none). A plain record, not a <see cref="Link"/>: the
/// favicon address is a slice concern (<c>LinkFavicon</c> lives in
/// the Links slice, which the core must not reference), so the
/// importer derives it when it builds the link.
/// </summary>
// CA1054/CA1056 do not fit here: Url is the validated address string
// (a LinkUrl value, normalized by TryCreate), stored as a string
// exactly like Link.Url.Value — a Uri property could not round-trip
// the exporter's escaping, and the importer hands it straight to
// LinkUrl.
[SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Url is a validated LinkUrl value stored as a string, like Link.Url.Value.")]
[SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Url is a validated LinkUrl value stored as a string, like Link.Url.Value.")]
public sealed record ParsedBookmark(
    string Name,
    string Url,
    string? Description,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt);

/// <summary>
/// Netscape Bookmark HTML, both directions. Pure string-in/objects-out:
/// no storage, no browser, no clock — the caller supplies the "now"
/// a missing <c>ADD_DATE</c> falls back to, so tests can pin it.
/// Folders flatten on the way in (a folder name becomes a tag on every
/// link inside it) and stay flat on the way out (the model has no
/// folders, so export never invents grouping). Malformed entries are
/// skipped, never fatal: a link without a usable address, or a date
/// that is not a plausible epoch, is dropped (or falls back to "now"
/// for the date) and parsing carries on.
/// </summary>
public static class NetscapeBookmarks
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

    // A <DD> note's end tag is optional in the files browsers actually
    // export (Chrome/Firefox write "<DD>note" with no "</DD>"), so a
    // note also ends where the next entry or list-close begins.
    private static readonly Regex TokenPattern = new(
        "(?<folder><h3[^>]*>(?<folderName>.*?)</h3>)"
        + "|(?<open><dl[^>]*>)"
        + "|(?<close></dl\\s*>)"
        + "|(?<link><a\\s+(?<attrs>[^>]*)>(?<title>.*?)</a>)"
        + "|(?<note><dd[^>]*>(?<noteText>.*?)(?:</dd>|(?=<dt)|(?=</dl)|$))",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex AttributePattern = new(
        "(?<name>[A-Za-z_][A-Za-z0-9_-]*)\\s*=\\s*(?:\"(?<value>[^\"]*)\"|'(?<value>[^']*)'|(?<value>[^\\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex TagPattern = new("<[^>]+>", RegexOptions.Compiled, RegexTimeout);

    /// <summary>
    /// Parses Netscape Bookmark HTML into bookmarks. A <c>&lt;DD&gt;</c>
    /// attaches to the link directly before it; anything else between
    /// a link and its note breaks the attachment. Folder names stack,
    /// so a link nested two folders deep carries both names as tags.
    /// </summary>
    public static IReadOnlyList<ParsedBookmark> Parse(string html, DateTimeOffset now)
    {
        List<ParsedBookmark> parsed = new();
        List<string?> folders = new();
        string? pendingFolder = null;
        int lastLinkIndex = -1;
        bool awaitingNote = false;
        foreach (Match match in TokenPattern.Matches(html))
        {
            if (match.Groups["folder"].Success)
            {
                pendingFolder = CleanText(match.Groups["folderName"].Value, collapseWhitespace: true);
                awaitingNote = false;
            }
            else if (match.Groups["open"].Success)
            {
                folders.Add(pendingFolder);
                pendingFolder = null;
                awaitingNote = false;
            }
            else if (match.Groups["close"].Success)
            {
                if (folders.Count > 0)
                {
                    folders.RemoveAt(folders.Count - 1);
                }

                awaitingNote = false;
            }
            else if (match.Groups["link"].Success)
            {
                ParsedBookmark? bookmark = ParseLink(match, folders, now);
                if (bookmark is not null)
                {
                    parsed.Add(bookmark);
                    lastLinkIndex = parsed.Count - 1;
                    awaitingNote = true;
                }
                else
                {
                    awaitingNote = false;
                }
            }
            else if (match.Groups["note"].Success)
            {
                if (awaitingNote && lastLinkIndex >= 0)
                {
                    string? note = CleanText(match.Groups["noteText"].Value, collapseWhitespace: false);
                    if (!string.IsNullOrEmpty(note))
                    {
                        parsed[lastLinkIndex] = parsed[lastLinkIndex] with { Description = note };
                    }
                }

                awaitingNote = false;
            }
            else
            {
                awaitingNote = false;
            }
        }

        return parsed;
    }

    /// <summary>Renders links as Netscape Bookmark HTML: folders flattened, tags as one comma list, notes as <c>&lt;DD&gt;</c>.</summary>
    public static string Export(IEnumerable<Link> links)
    {
        StringBuilder builder = new();
        builder.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
        builder.AppendLine("<!-- This is an automatically generated file.");
        builder.AppendLine("     It will be read and overwritten.");
        builder.AppendLine("     DO NOT EDIT! -->");
        builder.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
        builder.AppendLine("<TITLE>Bookmarks</TITLE>");
        builder.AppendLine("<H1>Bookmarks</H1>");
        builder.AppendLine("<DL><p>");
        foreach (Link link in links)
        {
            builder.Append("    <DT><A HREF=\"");
            builder.Append(WebUtility.HtmlEncode(link.Url.Value));
            builder.Append("\" ADD_DATE=\"");
            builder.Append(link.CreatedAt.ToUnixTimeSeconds());
            builder.Append('"');
            if (link.Tags.Count > 0)
            {
                builder.Append(" TAGS=\"");
                builder.Append(WebUtility.HtmlEncode(string.Join(",", link.Tags)));
                builder.Append('"');
            }

            builder.Append('>');
            builder.Append(WebUtility.HtmlEncode(link.Name));
            builder.AppendLine("</A>");
            if (!string.IsNullOrEmpty(link.Description))
            {
                builder.Append("    <DD>");
                builder.Append(WebUtility.HtmlEncode(link.Description));
                builder.AppendLine("</DD>");
            }
        }

        builder.AppendLine("</DL><p>");
        return builder.ToString();
    }

    private static ParsedBookmark? ParseLink(Match match, List<string?> folders, DateTimeOffset now)
    {
        Dictionary<string, string> attrs = new(StringComparer.OrdinalIgnoreCase);
        foreach (GroupCollection groups in AttributePattern.Matches(match.Groups["attrs"].Value).Cast<Match>().Select(attr => attr.Groups))
        {
            attrs[groups["name"].Value] = groups["value"].Value;
        }

        if (!attrs.TryGetValue("HREF", out string? href) || string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        // Attribute values are entity-encoded in the file (&amp; in a
        // query string, &lt; in a tag); decode before validating.
        if (!LinkUrl.TryCreate(WebUtility.HtmlDecode(href), out LinkUrl? url, out _) || url is null)
        {
            return null;
        }

        string title = CleanText(match.Groups["title"].Value, collapseWhitespace: true) ?? string.Empty;
        string name = string.IsNullOrWhiteSpace(title) ? url.Value : title;
        var tags = folders.OfType<string>().Where(folder => !string.IsNullOrWhiteSpace(folder)).ToList();

        if (attrs.TryGetValue("TAGS", out string? tagsValue))
        {
            foreach (string tag in WebUtility.HtmlDecode(tagsValue).Split(','))
            {
                string trimmed = tag.Trim();
                if (trimmed.Length > 0)
                {
                    tags.Add(trimmed);
                }
            }
        }

        return new ParsedBookmark(name, url.Value, null, DedupeTags(tags), ParseDate(attrs, now));
    }

    private static DateTimeOffset ParseDate(Dictionary<string, string> attrs, DateTimeOffset now)
    {
        if (!attrs.TryGetValue("ADD_DATE", out string? raw) || !long.TryParse(raw, out long seconds))
        {
            return now;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return now;
        }
    }

    private static List<string> DedupeTags(List<string> tags)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        return tags.Where(tag => seen.Add(tag)).ToList();
    }

    private static string? CleanText(string value, bool collapseWhitespace)
    {
        string stripped = TagPattern.Replace(value, string.Empty);
        string decoded = WebUtility.HtmlDecode(stripped).Trim();
        if (decoded.Length == 0)
        {
            return null;
        }

        if (!collapseWhitespace)
        {
            return decoded;
        }

        StringBuilder collapsed = new();
        bool lastWasSpace = false;
        foreach (char c in decoded)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    collapsed.Append(' ');
                }

                lastWasSpace = true;
            }
            else
            {
                collapsed.Append(c);
                lastWasSpace = false;
            }
        }

        return collapsed.ToString().Trim();
    }
}

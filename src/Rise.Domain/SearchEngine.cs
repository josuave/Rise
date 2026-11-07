using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Rise.Domain;

// CA1054/CA1056 do not fit here: a search URL template contains %s
// and is not a URI until the query is substituted; IsValidTemplate
// validates it, and the favicon URL is composed from it for the picker.
[SuppressMessage("Design", "CA1054", Justification = "A search URL template contains %s and is not a URI until the query is substituted; IsValidTemplate validates it.")]
[SuppressMessage("Design", "CA1056", Justification = "A search URL template contains %s and is not a URI until the query is substituted; the favicon URL is composed from it for the picker markup.")]
/// <summary>One search engine: an id, a display name, and the search URL template, with %s where the query goes.</summary>
public sealed record SearchEngine(string Id, string Name, string UrlTemplate)
{
    /// <summary>Ids of user-added engines start with this; built-in ids never do.</summary>
    public const string CustomPrefix = "custom:";

    /// <summary>The engine's own favicon, or null when the template has no usable host.</summary>
    public string? FaviconUrl
    {
        get
        {
            string probe = UrlTemplate.Replace("%s", "x", StringComparison.Ordinal);
            if (!Uri.TryCreate(probe, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            return string.Create(CultureInfo.InvariantCulture, $"https://{uri.Host}/favicon.ico");
        }
    }

    /// <summary>Whether the template is a usable search URL: absolute http(s) with exactly one %s.</summary>
    public static bool IsValidTemplate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        int first = template.IndexOf("%s", StringComparison.Ordinal);
        if (first < 0 || first != template.LastIndexOf("%s", StringComparison.Ordinal))
        {
            return false;
        }

        string probe = template.Replace("%s", "x", StringComparison.Ordinal);
        return Uri.TryCreate(probe, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>A new id for a user-added engine named <paramref name="name"/>, unlike any id in <paramref name="takenIds"/>.</summary>
    [SuppressMessage("Globalization", "CA1308", Justification = "Engine ids are lowercase slugs by convention, matching the built-in ids; the uniqueness check is ordinal.")]
    public static string NewCustomId(string name, IEnumerable<string> takenIds)
    {
        string slug = new string(name
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray()).Trim('-');
        if (slug.Length == 0)
        {
            slug = "engine";
        }

        HashSet<string> taken = [with(StringComparer.Ordinal), .. takenIds];
        string id = CustomPrefix + slug;
        int suffix = 2;
        while (taken.Contains(id))
        {
            id = CustomPrefix + slug + "-" + suffix;
            suffix++;
        }

        return id;
    }
}

/// <summary>
/// The search-engine registry: every built-in engine the search box
/// and the command palette can use. An engine joins by adding one
/// entry here; the query builder, the settings picker, and the search
/// service all read this list. Entries are composed from a host and a
/// path on purpose: no absolute URI literal sits in the registry.
/// User-added engines live in the Search widget's config, not here.
/// </summary>
public static class SearchEngines
{
    /// <summary>Google, the default engine.</summary>
    public const string Google = "google";

    /// <summary>DuckDuckGo.</summary>
    public const string DuckDuckGo = "duckduckgo";

    /// <summary>Bing.</summary>
    public const string Bing = "bing";

    /// <summary>Brave Search.</summary>
    public const string Brave = "brave";

    public static IReadOnlyList<SearchEngine> All { get; } = new[]
    {
        BuiltIn(Google, "Google", "www.google.com", "/search?q="),
        BuiltIn(DuckDuckGo, "DuckDuckGo", "duckduckgo.com", "/?q="),
        BuiltIn(Bing, "Bing", "www.bing.com", "/search?q="),
        BuiltIn(Brave, "Brave", "search.brave.com", "/search?q="),
    };

    /// <summary>The engine used when nothing is configured, or the configured id is unknown.</summary>
    public static SearchEngine Default => All[0];

    /// <summary>Resolves an engine id, falling back to the default for a null or unknown id.</summary>
    public static SearchEngine FromId(string? id) =>
        All.FirstOrDefault(engine => engine.Id == id) ?? Default;

    private static SearchEngine BuiltIn(string id, string name, string host, string path) =>
        new(id, name, string.Create(CultureInfo.InvariantCulture, $"https://{host}{path}%s"));
}

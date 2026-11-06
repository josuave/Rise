namespace Rise.Domain;

/// <summary>One search engine: an id, a display name, and the host and path the query is appended to.</summary>
public sealed record SearchEngine(string Id, string Name, string Host, string Path);

/// <summary>
/// The search-engine registry: every engine the search box and the
/// command palette can use. An engine joins by adding one entry
/// here; the query builder, the settings picker, and the search
/// service all read this list, so nothing else needs to know the
/// engines exist. Host and path are separate on purpose: no absolute
/// URI literal sits in the registry.
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
        new SearchEngine(Google, "Google", "www.google.com", "/search?q="),
        new SearchEngine(DuckDuckGo, "DuckDuckGo", "duckduckgo.com", "/?q="),
        new SearchEngine(Bing, "Bing", "www.bing.com", "/search?q="),
        new SearchEngine(Brave, "Brave", "search.brave.com", "/search?q="),
    };

    /// <summary>The engine used when nothing is configured, or the configured id is unknown.</summary>
    public static SearchEngine Default => All[0];

    /// <summary>Resolves an engine id, falling back to the default for a null or unknown id.</summary>
    public static SearchEngine FromId(string? id) =>
        All.FirstOrDefault(engine => engine.Id == id) ?? Default;
}

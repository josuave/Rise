namespace Rise.Domain;

/// <summary>Pure: turns raw search text into a search URL, or null for blank input. No browser involved.</summary>
public static class SearchQuery
{
    public static Uri? Build(string? query) => Build(query, SearchEngines.Default);

    public static Uri? Build(string? query, string? engineId) => Build(query, SearchEngines.FromId(engineId));

    public static Uri? Build(string? query, SearchEngine engine)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        string url = engine.UrlTemplate.Replace("%s", Uri.EscapeDataString(query.Trim()), StringComparison.Ordinal);
        return new Uri(url);
    }
}

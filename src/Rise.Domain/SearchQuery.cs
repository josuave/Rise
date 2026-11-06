using System.Globalization;

namespace Rise.Domain;

/// <summary>Pure: turns raw search text into a search URL, or null for blank input. No browser involved.</summary>
public static class SearchQuery
{
    public static Uri? Build(string? query) => Build(query, SearchEngines.Default.Id);

    public static Uri? Build(string? query, string? engineId)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        SearchEngine engine = SearchEngines.FromId(engineId);
        return new Uri(string.Create(
            CultureInfo.InvariantCulture,
            $"https://{engine.Host}{engine.Path}{Uri.EscapeDataString(query.Trim())}"));
    }
}

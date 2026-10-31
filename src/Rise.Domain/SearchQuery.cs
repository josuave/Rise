namespace Rise.Domain;

/// <summary>Pure: turns raw search text into a search URL, or null for blank input. No browser involved.</summary>
public static class SearchQuery
{
    public static Uri? Build(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return new Uri($"https://www.google.com/search?q={Uri.EscapeDataString(query.Trim())}");
    }
}

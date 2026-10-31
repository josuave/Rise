namespace Rise.Domain;

/// <summary>A start-page link. An entity: it has an identity (Id) and can be edited or removed.</summary>
public sealed record Link(Guid Id, string Name, LinkUrl Url)
{
    public static Link Create(string name, LinkUrl url) => new(Guid.NewGuid(), name.Trim(), url);

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

namespace Rise.Domain;

/// <summary>A validated, normalized web address. A value object: equal by value, immutable.</summary>
public sealed record LinkUrl(string Value)
{
    public static bool TryCreate(string? input, out LinkUrl? url, out string? error)
    {
        url = null;
        error = null;
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            error = "Give the link a name and a URL.";
            return false;
        }

        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "That doesn't look like a valid web address.";
            return false;
        }

        url = new LinkUrl(uri.ToString());
        return true;
    }

    public override string ToString() => Value;
}

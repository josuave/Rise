using System.Diagnostics.CodeAnalysis;

namespace Rise.Slices.Links;

/// <summary>
/// Link favicons come from Google's favicon service: the link's host
/// is sent to google.com, which returns the site's favicon. No icon
/// is ever validated — a stored icon address is used as-is, and a
/// link whose icon cannot be derived (or whose image fails to load)
/// falls back to the letter tile.
/// </summary>
public static class LinkFavicon
{
    // CA1054/CA1055 do not fit here: the parameter is unvalidated user
    // input (a typed URL, possibly without a scheme), and the return is
    // an icon address stored as a string, like Link.IconUrl itself.
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Parameter is unvalidated user input; a Uri parameter could not represent invalid or scheme-less input.")]
    [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "The icon address is stored as a string (Link.IconUrl); callers pass it straight to the service and the img src.")]
    public static string? ForUrl(string urlInput)
    {
        string? host = HostOf(urlInput);
        return host is null ? null : $"https://www.google.com/s2/favicons?domain={host}&sz=64";
    }

    /// <summary>The host of a stored link URL, or null when it cannot be parsed.</summary>
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Parameter is a stored link URL string (LinkUrl.Value), not a Uri the caller holds.")]
    public static string? DomainOf(string urlValue) => HostOf(urlValue);

    private static string? HostOf(string urlInput)
    {
        if (!Uri.TryCreate(urlInput, UriKind.Absolute, out Uri? uri)
            && !Uri.TryCreate("https://" + urlInput, UriKind.Absolute, out uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri.Host;
    }
}

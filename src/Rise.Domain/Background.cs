using System.Diagnostics.CodeAnalysis;

namespace Rise.Domain;

/// <summary>One built-in background: a calm gradient. The custom image is not a preset — it is a URL the user pastes in.</summary>
public sealed record BackgroundPreset(string Id, string Title, string Css);

/// <summary>
/// The built-in background registry: None plus the calm gradients.
/// A preset joins by adding one entry here; the service, the
/// Settings section, and backup all read this list.
/// </summary>
public static class BackgroundPresets
{
    /// <summary>No background: the plain theme background.</summary>
    public const string None = "none";

    /// <summary>A custom image URL, chosen in Settings — not a swatch.</summary>
    public const string Custom = "custom";

    /// <summary>An image file uploaded in Settings — not a swatch.</summary>
    public const string Upload = "upload";

    public static IReadOnlyList<BackgroundPreset> All { get; } = new[]
    {
        new BackgroundPreset(None, "None", string.Empty),
        new BackgroundPreset("dawn", "Dawn", "linear-gradient(160deg, #f8e9d4, #f2d7c9 45%, #e9d8df)"),
        new BackgroundPreset("mist", "Mist", "linear-gradient(160deg, #e9edf0, #dde8e6 50%, #e7e3d8)"),
        new BackgroundPreset("dusk", "Dusk", "linear-gradient(160deg, #e7def1, #d8d3e8 50%, #ccd7e6)"),
        new BackgroundPreset("forest", "Forest", "linear-gradient(160deg, #dfe9dc, #d2e0d0 50%, #e3e1cb)"),
    };
}

/// <summary>
/// The descriptor for an uploaded background image: everything
/// needed to find the file again and to ask for it by name when
/// the bytes are gone. The bytes themselves live in IndexedDB,
/// keyed by the hash — this descriptor is what the choice and the
/// backup carry, so a restore on a fresh device (or a browser
/// eviction) shows up as a named re-upload prompt, not a silent
/// blank background.
/// </summary>
public sealed record UploadedImage(string Hash, string FileName, long Size, string MimeType);

/// <summary>
/// The chosen background: a preset id, "custom" with the image
/// address, or "upload" with the uploaded image's descriptor.
/// One choice covers both themes in v1; the scrim in the shell
/// tints it with the theme's calm background so text stays
/// readable either way.
/// </summary>
[SuppressMessage("Design", "CA1054", Justification = "The address is a user-pasted string, stored and serialized as a string; it is validated as an absolute http(s) Uri at the service and import boundaries.")]
[SuppressMessage("Design", "CA1056", Justification = "The address is a user-pasted string, stored and serialized as a string; it is validated as an absolute http(s) Uri at the service and import boundaries.")]
public sealed record BackgroundChoice(string PresetId, string? CustomUrl, UploadedImage? Upload = null)
{
    public static BackgroundChoice None => new(BackgroundPresets.None, null);

    /// <summary>
    /// The CSS background-image value: a preset gradient, a custom
    /// image URL in single quotes (the style attribute uses double
    /// quotes), or empty for none.
    /// </summary>
    public string Css
    {
        get
        {
            if (PresetId == BackgroundPresets.Custom && !string.IsNullOrWhiteSpace(CustomUrl))
            {
                return $"url('{CustomUrl.Trim()}')";
            }

            BackgroundPreset? preset = BackgroundPresets.All.FirstOrDefault(p => p.Id == PresetId);
            return preset?.Css ?? string.Empty;
        }
    }
}

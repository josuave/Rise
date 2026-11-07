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

/// <summary>The preset case of a <see cref="BackgroundChoice"/>: one built-in gradient by id, including "none".</summary>
public sealed record PresetBackground(string Id);

/// <summary>The custom-URL case of a <see cref="BackgroundChoice"/>: an image address the user pasted in.</summary>
[SuppressMessage("Design", "CA1054", Justification = "The address is a user-pasted string, stored and serialized as a string; it is validated as an absolute http(s) Uri at the service and import boundaries.")]
[SuppressMessage("Design", "CA1056", Justification = "The address is a user-pasted string, stored and serialized as a string; it is validated as an absolute http(s) Uri at the service and import boundaries.")]
public sealed record CustomBackground(string Url);

/// <summary>The upload case of a <see cref="BackgroundChoice"/>: the uploaded image's descriptor. The bytes live in IndexedDB, keyed by the hash.</summary>
public sealed record UploadBackground(UploadedImage Image);

/// <summary>
/// The chosen background: a preset gradient, a custom image
/// address, or an uploaded image's descriptor. One choice covers
/// both themes in v1; the scrim in the shell tints it with the
/// theme's calm background so text stays readable either way.
/// A C# 15 union, so callers switch on it exhaustively: a new case
/// warns (CS8509) at every switch that does not handle it yet. The
/// union lowers to a struct; a default value holds null contents,
/// so switches add an explicit <c>null</c> arm rather than a
/// <c>_</c> arm (which would swallow a future case).
/// Persistence stays string-shaped: the stored JSON and the backup
/// DTO still carry a preset id plus an optional address/descriptor,
/// and <see cref="FromStored"/> / <see cref="ToStored"/> map at
/// those two boundaries only — the union itself lives in memory.
/// </summary>
public union BackgroundChoice(PresetBackground, CustomBackground, UploadBackground) : IEquatable<BackgroundChoice>
{
    public static BackgroundChoice None => new PresetBackground(BackgroundPresets.None);

    /// <summary>
    /// Builds a choice from its stored string shape (the localStorage
    /// JSON, or a backup DTO's fields): "custom" becomes the custom
    /// case, "upload" with a descriptor becomes the upload case, and
    /// anything else — including an unknown preset id, which keeps
    /// its id so it still renders as empty — becomes the preset case.
    /// An upload tag without a descriptor repairs to none, matching
    /// the old record's empty rendering for that shape.
    /// </summary>
    public static BackgroundChoice FromStored(string presetId, string? customAddress, UploadedImage? upload) => presetId switch
    {
        BackgroundPresets.Custom => new CustomBackground(customAddress ?? string.Empty),
        BackgroundPresets.Upload => upload is null ? None : new UploadBackground(upload),
        _ => new PresetBackground(presetId),
    };

    /// <summary>The stored string shape of this choice: the preset id, plus the image address when custom, plus the uploaded image's descriptor when an upload.</summary>
    public (string PresetId, string? CustomUrl, UploadedImage? Upload) ToStored() => this switch
    {
        PresetBackground preset => (preset.Id, null, null),
        CustomBackground custom => (BackgroundPresets.Custom, custom.Url, null),
        UploadBackground upload => (BackgroundPresets.Upload, null, upload.Image),
        null => (BackgroundPresets.None, null, null),
    };

    /// <summary>The uploaded image's descriptor when this choice is an upload, else null.</summary>
    public UploadedImage? UploadImage => this switch
    {
        UploadBackground upload => upload.Image,
        PresetBackground => null,
        CustomBackground => null,
        null => null,
    };

    /// <summary>
    /// The CSS background-image value: a preset gradient, a custom
    /// image URL in single quotes (the style attribute uses double
    /// quotes), or empty for none — and empty for an upload, whose
    /// blob URL the service resolves separately.
    /// </summary>
    public string Css => this switch
    {
        PresetBackground preset => CssForPreset(preset.Id),
        CustomBackground custom => CssForCustom(custom.Url),
        UploadBackground => string.Empty,
        null => string.Empty,
    };

    private static string CssForPreset(string id)
    {
        BackgroundPreset? preset = BackgroundPresets.All.FirstOrDefault(p => p.Id == id);
        return preset?.Css ?? string.Empty;
    }

    private static string CssForCustom(string address) =>
        string.IsNullOrWhiteSpace(address) ? string.Empty : $"url('{address.Trim()}')";

    // The union lowers to a plain struct, so equality is hand-written
    // (CA1815, CA1066): two choices are equal when their cases hold equal values.
    public bool Equals(BackgroundChoice other) => Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is BackgroundChoice other && Equals(other);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    public static bool operator ==(BackgroundChoice left, BackgroundChoice right) => left.Equals(right);

    public static bool operator !=(BackgroundChoice left, BackgroundChoice right) => !left.Equals(right);
}

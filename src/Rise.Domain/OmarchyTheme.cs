namespace Rise.Domain;

/// <summary>
/// One Omarchy theme as a single palette: Omarchy themes declare
/// their own mode (dark or light), so unlike a user-made custom
/// theme a pack theme has no second half — picking it applies this
/// palette whatever the device prefers. The choice is stored as
/// "pack:omarchy:&lt;slug&gt;" in the same preference the built-ins
/// use. Accent is the theme's accent color from colors.toml, kept
/// for the picker's swatch; it is not one of the nine calm variables.
/// </summary>
public sealed record OmarchyTheme(string Id, string Title, bool IsDark, ThemePalette Palette, string Accent)
{
    public const string ChoicePrefix = "pack:omarchy:";

    /// <summary>Id of the pack's module in the shell's catalog.</summary>
    public const string ModuleId = "OmarchyThemes";

    public static string ChoiceFor(string id) => $"{ChoicePrefix}{id}";

    public static bool TryParseChoice(string? choice, out string id)
    {
        id = string.Empty;
        if (choice is null || !choice.StartsWith(ChoicePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        id = choice[ChoicePrefix.Length..];
        return id.Length != 0;
    }
}

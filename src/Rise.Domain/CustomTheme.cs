namespace Rise.Domain;

/// <summary>
/// One palette: the nine calm CSS variables. Values are CSS colors
/// (the editor always writes #rrggbb). The built-in defaults mirror
/// the light and dark palettes in app.css — keep them in sync.
/// </summary>
public sealed record ThemePalette(
    string Background,
    string Text,
    string Muted,
    string Faint,
    string Surface,
    string Border,
    string BorderSoft,
    string TileBackground,
    string Error)
{
    public static ThemePalette DefaultLight => new(
        "#f7f5f0", "#222222", "#6b655c", "#8a8378", "#ffffff",
        "#ddd5c8", "#e2ddd2", "#efe9df", "#a33");

    public static ThemePalette DefaultDark => new(
        "#1b1b1f", "#f0ede6", "#a8a29e", "#8e8a85", "#26262b",
        "#3a3a40", "#33333a", "#2e2e34", "#e88");
}

/// <summary>
/// A user-made theme: a name over a light palette and a dark palette.
/// When active, the light half shows in light conditions and the dark
/// half when the device prefers dark — the same shape as the built-in
/// themes, so a custom theme slots into the existing picker.
/// </summary>
public sealed record CustomTheme(string Id, string Name, ThemePalette Light, ThemePalette Dark);

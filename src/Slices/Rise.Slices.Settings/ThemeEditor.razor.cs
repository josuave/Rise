using Microsoft.AspNetCore.Components;
using Rise.Domain;

namespace Rise.Slices.Settings;

/// <summary>
/// Mutable edit draft for one theme palette. The domain records stay
/// immutable; the editor works on this copy so Cancel discards edits.
/// </summary>
public sealed class EditableThemePalette
{
    public string Background { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Muted { get; set; } = string.Empty;
    public string Faint { get; set; } = string.Empty;
    public string Surface { get; set; } = string.Empty;
    public string Border { get; set; } = string.Empty;
    public string BorderSoft { get; set; } = string.Empty;
    public string TileBackground { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;

    public static EditableThemePalette From(ThemePalette palette) => new()
    {
        Background = palette.Background,
        Text = palette.Text,
        Muted = palette.Muted,
        Faint = palette.Faint,
        Surface = palette.Surface,
        Border = palette.Border,
        BorderSoft = palette.BorderSoft,
        TileBackground = palette.TileBackground,
        Error = palette.Error,
    };

    public ThemePalette ToPalette() => new(
        Background, Text, Muted, Faint, Surface, Border, BorderSoft, TileBackground, Error);
}

/// <summary>Mutable edit draft for one custom theme.</summary>
public sealed class EditableTheme
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EditableThemePalette Light { get; set; } = new();
    public EditableThemePalette Dark { get; set; } = new();

    public static EditableTheme From(CustomTheme theme) => new()
    {
        Id = theme.Id,
        Name = theme.Name,
        Light = EditableThemePalette.From(theme.Light),
        Dark = EditableThemePalette.From(theme.Dark),
    };

    public CustomTheme ToTheme() => new(Id, Name, Light.ToPalette(), Dark.ToPalette());
}

/// <summary>
/// The theme editor: name plus a color picker per calm variable, for
/// the light palette and the dark palette. Edits live in a draft
/// until Save; Cancel discards them.
/// </summary>
public partial class ThemeEditor
{
    [Parameter]
    public CustomTheme? Theme { get; set; }

    [Parameter]
    public EventCallback<CustomTheme> OnSave { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private EditableTheme Draft { get; set; } = new();

    private string? _draftForId;

    private sealed record PaletteField(
        string Label,
        Func<EditableThemePalette, string> Get,
        Action<EditableThemePalette, string> Set);

    private static readonly PaletteField[] PaletteFields = new[]
    {
        new PaletteField("Background", p => p.Background, (p, v) => p.Background = v),
        new PaletteField("Text", p => p.Text, (p, v) => p.Text = v),
        new PaletteField("Muted", p => p.Muted, (p, v) => p.Muted = v),
        new PaletteField("Faint", p => p.Faint, (p, v) => p.Faint = v),
        new PaletteField("Surface", p => p.Surface, (p, v) => p.Surface = v),
        new PaletteField("Border", p => p.Border, (p, v) => p.Border = v),
        new PaletteField("Soft border", p => p.BorderSoft, (p, v) => p.BorderSoft = v),
        new PaletteField("Tile background", p => p.TileBackground, (p, v) => p.TileBackground = v),
        new PaletteField("Error", p => p.Error, (p, v) => p.Error = v),
    };

    protected override void OnParametersSet()
    {
        // Refresh the draft only when a different theme is edited, so
        // a parent re-render mid-edit never wipes unsaved changes.
        if (Theme is not null && Theme.Id != _draftForId)
        {
            Draft = EditableTheme.From(Theme);
            _draftForId = Theme.Id;
        }
    }

    private async Task SaveAsync()
    {
        if (OnSave.HasDelegate)
        {
            await OnSave.InvokeAsync(Draft.ToTheme());
        }
    }

    private async Task CancelAsync()
    {
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }
}

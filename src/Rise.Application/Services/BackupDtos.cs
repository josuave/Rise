using System.Diagnostics.CodeAnalysis;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>One link in a backup: id, name, and URL string.</summary>
// CA1054/CA1056 do not fit here: backup files store URLs as
// strings, and validation happens via LinkUrl.TryCreate on import.
[SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Backup files store URLs as strings; LinkUrl.TryCreate validates on import.")]
[SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Backup files store URLs as strings; LinkUrl.TryCreate validates on import.")]
public sealed record BackupLinkDto(Guid Id, string Name, string Url);

/// <summary>One palette in a backup: the nine calm CSS variables.</summary>
public sealed record BackupPaletteDto(
    string Background,
    string Text,
    string Muted,
    string Faint,
    string Surface,
    string Border,
    string BorderSoft,
    string TileBackground,
    string Error);

/// <summary>One custom theme in a backup: a name over a light and a dark palette.</summary>
public sealed record BackupThemeDto(string Id, string Name, BackupPaletteDto Light, BackupPaletteDto Dark);

/// <summary>One widget's slot in a backup layout: id and visibility. Zones come from the definitions on load.</summary>
public sealed record BackupWidgetDto(string Id, bool Visible);

/// <summary>
/// One saved layout in a backup: a name over widget slots, plus the
/// group's member ids as one layout-level list (absent in files that
/// predate groups, which reads as empty on import).
/// </summary>
public sealed record BackupLayoutDto(string Id, string Name, IReadOnlyList<BackupWidgetDto> Widgets, IReadOnlyList<string>? GroupChildren);

/// <summary>The saved layout set in a backup: the active layout id and every layout.</summary>
public sealed record BackupLayoutsDto(string ActiveId, IReadOnlyList<BackupLayoutDto> Layouts);

/// <summary>One to-do in a backup: id, text, and done flag.</summary>
public sealed record BackupTodoDto(Guid Id, string Text, bool Done);

/// <summary>One note in a backup: id and text.</summary>
public sealed record BackupNoteDto(Guid Id, string Text);

/// <summary>
/// The saved state in a backup: links, theme choice, custom themes,
/// layouts, per-widget config JSON, the to-do and note lists, the
/// explicit module choices, the explicit hotkey bindings, and the
/// explicit preview-feature choices (absent in files that predate
/// them, which read as empty on import — and, for modules, hotkeys,
/// and preview features, leave current choices untouched).
/// </summary>
public sealed record BackupStateDto(
    IReadOnlyList<BackupLinkDto> Links,
    string Theme,
    IReadOnlyList<BackupThemeDto> Themes,
    BackupLayoutsDto Layouts,
    Dictionary<string, string> WidgetConfigs,
    IReadOnlyList<BackupTodoDto>? Todos,
    IReadOnlyList<BackupNoteDto>? Notes,
    Dictionary<string, bool>? Modules,
    Dictionary<string, HotkeyBinding>? Hotkeys,
    Dictionary<string, bool>? PreviewFeatures);

/// <summary>A parsed backup file: its version, its export date (ISO 8601), and its state JSON.</summary>
public sealed record BackupEnvelope(int FormatVersion, string ExportedAt, System.Text.Json.Nodes.JsonObject State);

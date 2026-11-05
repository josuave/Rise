using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Wasm;

/// <summary>
/// The module catalog, owned by the composition root: one entry per
/// slice. Adding a module is one entry here plus the slice itself —
/// never a change to the layout model or the shell's render loops.
/// Widget ids must match the slices' definitions; the Settings module
/// cannot be disabled, because it owns the gear and the dialog the
/// Modules section itself lives in.
/// </summary>
internal sealed class StaticModuleCatalog : IModuleCatalog
{
    private static readonly string[] HeaderWidgets = { "Date", "Clock", "Greeting" };
    private static readonly string[] SearchWidgets = { "Search" };
    private static readonly string[] LinksWidgets = { "Links" };
    private static readonly string[] WeatherWidgets = { "Weather" };
    private static readonly string[] BackupWidgets = { "Backup" };
    private static readonly string[] TodoWidgets = { "Todo" };
    private static readonly string[] NotesWidgets = { "Notes" };
    private static readonly string[] LayoutWidgets = { "Group" };
    private static readonly string[] SettingsWidgets = { "Theme", "Settings" };

    public IReadOnlyList<ModuleInfo> Modules { get; } = new[]
    {
        new ModuleInfo("Header", "Header", HeaderWidgets, "Rise.Slices.Header", IsLazy: false, CanDisable: true),
        new ModuleInfo("Search", "Search", SearchWidgets, "Rise.Slices.Search", IsLazy: false, CanDisable: true),
        new ModuleInfo("Links", "Quick links", LinksWidgets, "Rise.Slices.Links", IsLazy: false, CanDisable: true),
        new ModuleInfo("Weather", "Weather", WeatherWidgets, "Rise.Slices.Weather", IsLazy: false, CanDisable: true),
        new ModuleInfo("Backup", "Backup", BackupWidgets, "Rise.Slices.Backup", IsLazy: false, CanDisable: true),
        new ModuleInfo("Todo", "To-do", TodoWidgets, "Rise.Slices.Todo", IsLazy: false, CanDisable: true),
        new ModuleInfo("Notes", "Notes", NotesWidgets, "Rise.Slices.Notes", IsLazy: true, CanDisable: true),
        new ModuleInfo("Layout", "Groups", LayoutWidgets, "Rise.Slices.Layout", IsLazy: false, CanDisable: true),
        new ModuleInfo("Settings", "Settings", SettingsWidgets, "Rise.Slices.Settings", IsLazy: false, CanDisable: false),
    };
}

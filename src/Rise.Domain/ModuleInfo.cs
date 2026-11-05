namespace Rise.Domain;

/// <summary>
/// One module in the shell's catalog: a slice's widgets, the assembly
/// they live in, and whether the module can be switched off. A module
/// is a slice plus catalog data — the slice still owns its widgets and
/// settings sections; the catalog only maps them so the shell, the
/// layout editor, and the settings dialog can filter by module without
/// referencing any slice. A module's settings sections must live in
/// the module's assembly, so the dialog can filter sections by
/// assembly name.
/// </summary>
public sealed record ModuleInfo(
    string Id,
    string Title,
    IReadOnlyList<string> WidgetIds,
    string AssemblyName,
    bool IsLazy,
    bool CanDisable);

using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>
/// The shell's module catalog: every module, its widgets, and its
/// assembly. Slices depend only on this port, never on the shell's
/// catalog implementation, so the editor and the dialog can filter by
/// module.
/// </summary>
public interface IModuleCatalog
{
    IReadOnlyList<ModuleInfo> Modules { get; }

    /// <summary>The module that owns a widget id, or null when no module claims it. Unclaimed widgets are never filtered.</summary>
    ModuleInfo? ForWidget(string widgetId) =>
        Modules.FirstOrDefault(m => m.WidgetIds.Contains(widgetId));
}

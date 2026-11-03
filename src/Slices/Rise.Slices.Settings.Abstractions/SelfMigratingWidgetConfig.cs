using System.Text.Json.Nodes;

namespace Rise.Slices.Settings.Abstractions;

/// <summary>
/// Base a widget's configuration derives from to manage itself.
/// The widget owns its schema and its evolution: it names its
/// widget, states its current version, and migrates stored JSON
/// from an older version to the current one. Nothing central ever
/// learns a widget's shape. The migration runs on load (see the
/// widget-config service) and the migrated JSON is saved back, so
/// stored configs converge on their own. A missing version field
/// means v1: every config stored before versioning existed is v1
/// by rule. The base carries no implementation — this library is
/// abstractions only — so the load/migrate/save loop lives in the
/// widget-config service, which takes the Migrate method as a
/// delegate and never references a config type.
/// </summary>
public abstract class SelfMigratingWidgetConfig
{
    /// <summary>The widget this config belongs to, matching its definition id.</summary>
    public abstract string WidgetId { get; }

    /// <summary>The config schema's current version.</summary>
    public abstract int CurrentVersion { get; }

    /// <summary>
    /// Brings stored JSON from <paramref name="fromVersion"/> to the
    /// current version. Pure: it transforms the object, it never
    /// touches storage.
    /// </summary>
    public abstract JsonObject Migrate(JsonObject data, int fromVersion);
}

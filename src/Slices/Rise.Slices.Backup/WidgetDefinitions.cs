using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Backup;

/// <summary>
/// The Backup slice's widget definition: import, export, and migrate
/// as a page widget (hidden by default; shown from the layout
/// editor), sharing the controls the Settings section renders.
/// </summary>
public sealed class BackupWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Backup";

    public string Id => WidgetId;

    public string Title => "Backup";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order => 35;

    public Type ComponentType => typeof(BackupControls);
}

/// <summary>The Backup slice's fulfillment of the Settings contract.</summary>
public sealed class BackupSettingsSection : ISettingsSection
{
    public string Title => "Backup";

    public int Order => 200;

    public string Description => "Export, import, or migrate your saved state.";

    public Type ComponentType => typeof(BackupControls);
}

/// <summary>Shell wiring for the Backup slice's widget and Settings section.</summary>
public static class BackupExtensions
{
    public static IServiceCollection AddBackupWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, BackupWidgetDefinition>();
        return services;
    }

    public static IServiceCollection AddBackupSettings(this IServiceCollection services)
    {
        services.AddScoped<ISettingsSection, BackupSettingsSection>();
        return services;
    }
}

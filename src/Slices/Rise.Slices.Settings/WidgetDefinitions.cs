using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Settings;

/// <summary>
/// The Settings slice's widget definitions: the theme switcher and the
/// gear button are separate widgets, so each can move and hide on its
/// own. The shell never references these types — it only sees
/// IWidgetDefinition instances the shell registered.
/// </summary>
public sealed class ThemeWidgetDefinition : IWidgetDefinition
{
    public string Id => "Theme";
    public string Title => "Theme switcher";
    public WidgetZone Zone => WidgetZone.Cluster;
    public bool DefaultVisible => true;
    public int Order => 40;
    public Type ComponentType => typeof(ThemeToggle);
}

public sealed class SettingsWidgetDefinition : IWidgetDefinition
{
    public string Id => "Settings";
    public string Title => "Settings";
    public WidgetZone Zone => WidgetZone.Cluster;
    public bool DefaultVisible => true;
    public int Order => 50;
    public Type ComponentType => typeof(SettingsGear);
}

/// <summary>Shell wiring for the Settings slice's widgets.</summary>
public static class SettingsWidgetExtensions
{
    public static IServiceCollection AddSettingsWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, ThemeWidgetDefinition>();
        services.AddSingleton<IWidgetDefinition, SettingsWidgetDefinition>();
        return services;
    }
}

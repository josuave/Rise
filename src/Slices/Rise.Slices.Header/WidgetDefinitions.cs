using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Header;

/// <summary>
/// The header slice's widget definitions: date, clock, and greeting
/// are separate widgets, so each moves and hides on its own. The
/// clock and greeting carry settings, rendered as sections in the
/// Settings dialog; the date has no settings.
/// </summary>
public sealed class DateWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Date";

    public string Id => WidgetId;

    public string Title => "Date";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => true;

    public int Order => 5;

    public Type ComponentType => typeof(HeaderDate);
}

public sealed class ClockWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Clock";

    public string Id => WidgetId;

    public string Title => "Clock";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => true;

    public int Order => 6;

    public Type ComponentType => typeof(HeaderClock);
}

public sealed class GreetingWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Greeting";

    public string Id => WidgetId;

    public string Title => "Greeting";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => true;

    public int Order => 7;

    public Type ComponentType => typeof(HeaderGreeting);
}

/// <summary>Shell wiring for the header slice's widgets.</summary>
public static class HeaderWidgetExtensions
{
    public static IServiceCollection AddHeaderWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, DateWidgetDefinition>();
        services.AddSingleton<IWidgetDefinition, ClockWidgetDefinition>();
        services.AddSingleton<IWidgetDefinition, GreetingWidgetDefinition>();
        services.AddScoped<ClockConfig>();
        services.AddScoped<GreetingConfig>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// The Links slice's widget definition: the tiles widget. The shell
/// never references this type — it only sees IWidgetDefinition
/// instances the shell registered. Editing lives in the Settings
/// dialog, not in a second widget.
/// </summary>
public sealed class LinksWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Links";

    public string Id => WidgetId;
    public string Title => "Links";
    public WidgetZone Zone => WidgetZone.Main;
    public bool DefaultVisible => true;
    public int Order => 20;
    public Type ComponentType => typeof(QuickLinks);
}

/// <summary>Shell wiring for the Links slice's widgets.</summary>
public static class LinksWidgetExtensions
{
    public static IServiceCollection AddLinksWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, LinksWidgetDefinition>();
        services.AddScoped<LinksConfig>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// The Links slice's widget definitions: the tiles and the editor are
/// separate widgets, so each can move and hide on its own. The shell
/// never references these types — it only sees IWidgetDefinition
/// instances the shell registered.
/// </summary>
public sealed class LinksWidgetDefinition : IWidgetDefinition
{
    public string Id => "Links";
    public string Title => "Links";
    public WidgetZone Zone => WidgetZone.Main;
    public bool DefaultVisible => true;
    public int Order => 20;
    public Type ComponentType => typeof(QuickLinks);
}

public sealed class LinkEditorWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "LinkEditor";

    public string Id => WidgetId;
    public string Title => "Link editor";
    public WidgetZone Zone => WidgetZone.Main;
    public bool DefaultVisible => false;
    public int Order => 30;
    public Type ComponentType => typeof(LinksEditor);
}

/// <summary>Shell wiring for the Links slice's widgets.</summary>
public static class LinksWidgetExtensions
{
    public static IServiceCollection AddLinksWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, LinksWidgetDefinition>();
        services.AddSingleton<IWidgetDefinition, LinkEditorWidgetDefinition>();
        return services;
    }
}

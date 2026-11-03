using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Layout;

/// <summary>
/// The group widget's definition: a Main container, hidden until used.
/// It is the page's one group (v1): its member ids live on the layout
/// (see PageLayout.GroupChildren), and its display name lives in its
/// own config, not in the definition.
/// </summary>
public sealed class GroupWidgetDefinition : IConfigurableWidgetDefinition
{
    public const string WidgetId = "Group";

    public string Id => WidgetId;

    public string Title => "Group";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order => 45;

    public Type ComponentType => typeof(GroupWidget);

    public bool IsGroup => true;

    public Type ConfigComponentType => typeof(GroupConfigEditor);
}

/// <summary>Registers the group widget and its name config.</summary>
public static class LayoutWidgetDefinitions
{
    public static IServiceCollection AddLayoutWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, GroupWidgetDefinition>();
        services.AddScoped<GroupConfig>();
        return services;
    }
}

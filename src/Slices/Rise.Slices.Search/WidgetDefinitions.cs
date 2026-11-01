using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Search;

/// <summary>
/// The Search slice's widget definition, plus the DI registration the
/// shell calls. The shell never references this type — it only sees
/// IWidgetDefinition instances the shell registered.
/// </summary>
public sealed class SearchWidgetDefinition : IWidgetDefinition
{
    public string Id => "Search";
    public string Title => "Search";
    public WidgetZone Zone => WidgetZone.Main;
    public bool DefaultVisible => true;
    public int Order => 10;
    public Type ComponentType => typeof(Search);
}

/// <summary>Shell wiring for the Search slice's widget.</summary>
public static class SearchWidgetExtensions
{
    public static IServiceCollection AddSearchWidget(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, SearchWidgetDefinition>();
        return services;
    }
}

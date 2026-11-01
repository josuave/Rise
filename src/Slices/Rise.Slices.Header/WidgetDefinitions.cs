using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Header;

/// <summary>Widget definition for the header: first in the main column, visible by default.</summary>
public sealed class HeaderWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Header";

    public string Id => WidgetId;

    public string Title => "Header";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => true;

    public int Order => 5;

    public Type ComponentType => typeof(Header);
}

/// <summary>DI extension: registers the header widget definition. Wired once in the shell's Program.cs.</summary>
public static class HeaderWidgetExtensions
{
    public static IServiceCollection AddHeaderWidget(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, HeaderWidgetDefinition>();
        return services;
    }
}

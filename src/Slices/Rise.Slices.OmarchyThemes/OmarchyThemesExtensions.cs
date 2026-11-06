using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;

namespace Rise.Slices.OmarchyThemes;

/// <summary>Registers the Omarchy theme pack: the provider is a singleton over static data.</summary>
public static class OmarchyThemesExtensions
{
    public static IServiceCollection AddOmarchyThemes(this IServiceCollection services)
    {
        services.AddSingleton<IOmarchyThemeProvider, OmarchyThemeProvider>();
        return services;
    }
}

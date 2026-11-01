using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Infrastructure.Adapters;

namespace Rise.Infrastructure;

/// <summary>
/// Composition wiring for the core: application services plus their infrastructure adapters.
/// Called only by the shell (Rise.Wasm); no slice or core project calls this.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRiseCore(this IServiceCollection services)
    {
        services.AddScoped<StartPageService>();
        services.AddScoped<SearchService>();
        services.AddScoped<LayoutService>();
        services.AddScoped<CustomThemeService>();
        services.AddScoped<ICustomThemeStore, LocalStorageCustomThemeStore>();
        services.AddScoped<ILayoutCollectionStore, LocalStorageLayoutCollectionStore>();
        services.AddScoped<ILinkStore, LocalStorageLinkStore>();
        services.AddScoped<ISearchLauncher, BrowserSearchLauncher>();
        services.AddScoped<IThemeStore, ThemeStore>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}

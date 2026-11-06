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
        services.AddScoped<WidgetConfigService>();
        services.AddScoped<BackupService>();
        services.AddScoped<TodoService>();
        services.AddScoped<NotesService>();
        services.AddScoped<ModuleService>();
        services.AddScoped<HotkeyService>();
        services.AddScoped<PreviewFeatureService>();
        services.AddScoped<BackgroundService>();
        services.AddScoped<WeatherService>();
        services.AddScoped<IWeatherClient, OpenMeteoWeatherClient>();
        services.AddScoped<IPlaceGeocoder, OpenMeteoPlaceGeocoder>();
        services.AddScoped<IWidgetConfigStore, LocalStorageWidgetConfigStore>();
        services.AddScoped<IModuleStore, LocalStorageModuleStore>();
        services.AddScoped<IHotkeyStore, LocalStorageHotkeyStore>();
        services.AddScoped<IPreviewFeatureStore, LocalStoragePreviewFeatureStore>();
        services.AddScoped<IBackgroundStore, LocalStorageBackgroundStore>();
        services.AddScoped<IBackgroundImageStore, JsBackgroundImageStore>();
        services.AddScoped<IOmarchyThemeCache, LocalStorageOmarchyThemeCache>();
        services.AddScoped<IFileDownloader, BrowserFileDownloader>();
        services.AddScoped<ICustomThemeStore, LocalStorageCustomThemeStore>();
        services.AddScoped<ILayoutCollectionStore, LocalStorageLayoutCollectionStore>();
        services.AddScoped<ILinkStore, LocalStorageLinkStore>();
        services.AddScoped<ITodoStore, LocalStorageTodoStore>();
        services.AddScoped<INoteStore, LocalStorageNoteStore>();
        services.AddScoped<ISearchLauncher, BrowserSearchLauncher>();
        services.AddScoped<IThemeStore, ThemeStore>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}

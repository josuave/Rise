using Microsoft.Extensions.DependencyInjection;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Weather;

/// <summary>
/// The Weather slice's fulfillment of the Settings contract: its
/// place and forecast section, plus the DI registration the shell
/// calls.
/// </summary>
public sealed class WeatherSettingsSection : ISettingsSection
{
    public string Title => "Weather";
    public int Order => 150;
    public string Description => "Place and forecast for the weather widget.";
    public Type ComponentType => typeof(WeatherSettings);
}

/// <summary>Shell wiring for the Weather slice's Settings contribution.</summary>
public static class WeatherSettingsExtensions
{
    public static IServiceCollection AddWeatherSettings(this IServiceCollection services)
    {
        services.AddScoped<ISettingsSection, WeatherSettingsSection>();
        return services;
    }
}

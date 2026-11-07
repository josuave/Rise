using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Weather;

/// <summary>
/// The weather header line: a single muted line under the calm top
/// ("Queensbury · 71° · Clear sky · H 71° / L 40°"). Renders
/// nothing while loading or when the weather can't be loaded.
/// Refreshes on load and every 30 minutes, and reloads when its
/// own settings save. A place without coordinates yet resolves
/// through the geocoder first, then saves the coordinates back.
/// </summary>
public partial class WeatherHeaderLine : IDisposable
{
    [Inject]
    private WeatherService WeatherService { get; set; } = null!;

    [Inject]
    private WeatherConfig Config { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private WeatherForecast? Forecast { get; set; }

    private System.Threading.Timer? _timer;

    protected override async Task OnInitializedAsync()
    {
        await Config.LoadAsync();
        ConfigService.ConfigChanged += OnConfigChanged;
        await RefreshAsync();
        _timer = new System.Threading.Timer(
            _ => InvokeAsync(RefreshAsync),
            null,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(30));
    }

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != WeatherWidgetDefinition.WidgetId)
        {
            return;
        }

        await Config.LoadAsync();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (Config.Latitude is null || Config.Longitude is null)
        {
            Result<GeocodedPlace> geocoded = await WeatherService.GeocodeAsync(Config.Place);
            switch (geocoded)
            {
                case Ok<GeocodedPlace> ok:
                    Config.Place = ok.Value.Name;
                    Config.Latitude = ok.Value.Latitude;
                    Config.Longitude = ok.Value.Longitude;
                    await Config.SaveAsync();
                    break;
                case Error or null:
                    Forecast = null;
                    await InvokeAsync(StateHasChanged);
                    return;
            }
        }

        Result<WeatherForecast> result = await WeatherService.GetForecastAsync(
            Config.Latitude.Value, Config.Longitude.Value, 1);
        switch (result)
        {
            case Ok<WeatherForecast> ok:
                Forecast = ok.Value.Daily.Count == 0 ? null : ok.Value;
                break;
            case Error or null:
                Forecast = null;
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private string ShortPlace
    {
        get
        {
            string place = Config.Place;
            int comma = place.IndexOf(',', StringComparison.Ordinal);
            return (comma < 0 ? place : place[..comma]).Trim();
        }
    }

    private string HeaderLine(WeatherForecast forecast) =>
        $"{ShortPlace} · {Temp(forecast.Current.TempF)} · {WeatherCode.Describe(forecast.Current.Code)} · H {Temp(forecast.Daily[0].MaxF)} / L {Temp(forecast.Daily[0].MinF)}";

    private static string Temp(double tempF) => tempF.ToString("0", CultureInfo.InvariantCulture) + "°";

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
            ConfigService.ConfigChanged -= OnConfigChanged;
        }
    }
}

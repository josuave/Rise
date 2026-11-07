using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Weather;

/// <summary>
/// The weather widget: current conditions for the configured
/// place, plus daily forecast rows when enabled. Refreshes on load
/// and every 30 minutes, and reloads when its own settings save.
/// A place without coordinates yet resolves through the geocoder
/// first, then saves the coordinates back.
/// </summary>
public partial class WeatherWidget : IDisposable
{
    [Inject]
    private WeatherService WeatherService { get; set; } = null!;

    [Inject]
    private WeatherConfig Config { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private WeatherForecast? Forecast { get; set; }

    private string? Status { get; set; }

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
                case Error error:
                    Status = error.Message;
                    Forecast = null;
                    await InvokeAsync(StateHasChanged);
                    return;
                case null:
                    Status = "Couldn't find that place.";
                    Forecast = null;
                    await InvokeAsync(StateHasChanged);
                    return;
            }
        }

        int days = Config.ShowForecast ? Config.ForecastDays : 1;
        Result<WeatherForecast> result = await WeatherService.GetForecastAsync(
            Config.Latitude.Value, Config.Longitude.Value, days);
        switch (result)
        {
            case Ok<WeatherForecast> ok:
                Forecast = ok.Value;
                Status = null;
                break;
            case Error error:
                Status = error.Message;
                Forecast = null;
                break;
            case null:
                Status = "Couldn't load the weather.";
                Forecast = null;
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private static string Temp(double tempF) => tempF.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "°F";

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

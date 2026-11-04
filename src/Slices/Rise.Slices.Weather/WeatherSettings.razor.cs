using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Weather;

/// <summary>
/// The Weather slice's Settings section content: the typed place
/// (resolved to coordinates on save), the forecast toggle, and the
/// day count. All saved immediately on change.
/// </summary>
public partial class WeatherSettings
{
    [Inject]
    private WeatherConfig Config { get; set; } = null!;

    [Inject]
    private WeatherService WeatherService { get; set; } = null!;

    private string PlaceText { get; set; } = string.Empty;

    private string? FormError { get; set; }

    private bool ShowForecast => Config.ShowForecast;

    private int ForecastDays => Config.ForecastDays;

    protected override async Task OnInitializedAsync()
    {
        await Config.LoadAsync();
        PlaceText = Config.Place;
    }

    private async Task SavePlaceAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaceText))
        {
            FormError = "Enter a place name.";
            return;
        }

        Result<GeocodedPlace> result = await WeatherService.GeocodeAsync(PlaceText);
        if (!result.IsSuccess || result.Value is null)
        {
            FormError = result.Error ?? "Couldn't find that place.";
            return;
        }

        Config.Place = result.Value.Name;
        Config.Latitude = result.Value.Latitude;
        Config.Longitude = result.Value.Longitude;
        await Config.SaveAsync();
        PlaceText = Config.Place;
        FormError = null;
    }

    private async Task SetForecastAsync(bool show)
    {
        Config.ShowForecast = show;
        await Config.SaveAsync();
    }

    private async Task SetDaysAsync(ChangeEventArgs args)
    {
        if (args.Value is string text && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int days))
        {
            Config.ForecastDays = days;
            await Config.SaveAsync();
        }
    }
}

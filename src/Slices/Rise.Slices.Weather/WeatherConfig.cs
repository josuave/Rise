using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Weather;

/// <summary>
/// The weather widget's configuration: a typed place (resolved to
/// coordinates on save), whether the daily forecast shows, and how
/// many days it covers. Self-managing over its JSON (v1, identity
/// Migrate). The place defaults to Queensbury, NY; coordinates
/// resolve from the place name on first load.
/// </summary>
public sealed class WeatherConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => WeatherWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; a missing key reads as the default.</summary>
    public JsonObject Data { get; private set; } = new();

    /// <summary>The place name as shown (resolved display name, or the typed text until resolved). Defaults to Queensbury, NY.</summary>
    public string Place
    {
        get => WidgetConfigJson.GetString(Data, nameof(Place)) ?? "Queensbury, NY";
        set => Data[nameof(Place)] = value;
    }

    /// <summary>Resolved latitude for the place, null until the place is resolved.</summary>
    public double? Latitude
    {
        get => Data[nameof(Latitude)] is JsonValue value && value.TryGetValue<double>(out double latitude) ? latitude : null;
        set
        {
            if (value is null)
            {
                Data.Remove(nameof(Latitude));
            }
            else
            {
                Data[nameof(Latitude)] = JsonValue.Create(value.Value);
            }
        }
    }

    /// <summary>Resolved longitude for the place, null until the place is resolved.</summary>
    public double? Longitude
    {
        get => Data[nameof(Longitude)] is JsonValue value && value.TryGetValue<double>(out double longitude) ? longitude : null;
        set
        {
            if (value is null)
            {
                Data.Remove(nameof(Longitude));
            }
            else
            {
                Data[nameof(Longitude)] = JsonValue.Create(value.Value);
            }
        }
    }

    /// <summary>True when the daily forecast rows show. Default true.</summary>
    public bool ShowForecast
    {
        get
        {
            if (Data[nameof(ShowForecast)] is JsonValue value && value.TryGetValue<bool>(out bool show))
            {
                return show;
            }

            return true;
        }

        set => Data[nameof(ShowForecast)] = value;
    }

    /// <summary>How many days the forecast request covers (today plus the following days). Clamped to 1–7. Default 3.</summary>
    public int ForecastDays
    {
        get => Data[nameof(ForecastDays)] is JsonValue value && value.TryGetValue<int>(out int days) ? days : 3;
        set => Data[nameof(ForecastDays)] = Math.Clamp(value, 1, 7);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        Data = await service.LoadMigratedAsync(WidgetId, CurrentVersion, Migrate, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await service.SetJsonAsync(WidgetId, Data.ToJsonString(), cancellationToken);
}

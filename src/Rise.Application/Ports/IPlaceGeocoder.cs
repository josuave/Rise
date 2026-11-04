using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: resolve a typed place name to coordinates.</summary>
public interface IPlaceGeocoder
{
    Task<Result<GeocodedPlace>> GeocodeAsync(string place, CancellationToken cancellationToken = default);
}

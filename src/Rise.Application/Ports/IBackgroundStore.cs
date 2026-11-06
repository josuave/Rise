using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Port for the chosen background: one choice, absent means none.</summary>
public interface IBackgroundStore
{
    Task<BackgroundChoice?> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BackgroundChoice choice, CancellationToken cancellationToken = default);
}

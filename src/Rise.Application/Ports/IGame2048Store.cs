using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: how a 2048 game is persisted. The core knows nothing about localStorage.</summary>
public interface IGame2048Store
{
    Task<Game2048State?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Game2048State state, CancellationToken cancellationToken = default);
}

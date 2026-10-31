using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: how a start page is persisted. The core knows nothing about localStorage.</summary>
public interface ILinkStore
{
    Task<StartPage?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(StartPage page, CancellationToken cancellationToken = default);
}

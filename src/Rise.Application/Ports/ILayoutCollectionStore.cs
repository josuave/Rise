using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: how the set of saved layouts is persisted. The core knows nothing about localStorage.</summary>
public interface ILayoutCollectionStore
{
    Task<LayoutCollection?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(LayoutCollection collection, CancellationToken cancellationToken = default);
}

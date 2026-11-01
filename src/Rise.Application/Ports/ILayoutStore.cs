using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: how the widget layout is persisted. The core knows nothing about localStorage.</summary>
public interface ILayoutStore
{
    Task<PageLayout?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(PageLayout layout, CancellationToken cancellationToken = default);
}

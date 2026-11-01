using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: how the user's custom themes are persisted. The core knows nothing about localStorage.</summary>
public interface ICustomThemeStore
{
    Task<IReadOnlyList<CustomTheme>?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyList<CustomTheme> themes, CancellationToken cancellationToken = default);
}

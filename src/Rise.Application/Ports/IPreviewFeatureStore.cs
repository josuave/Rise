namespace Rise.Application.Ports;

/// <summary>Port for the explicit preview-feature choices: a map of feature id to enabled flag. Mirrors <see cref="IModuleStore"/>.</summary>
public interface IPreviewFeatureStore
{
    Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SetAsync(string featureId, bool enabled, CancellationToken cancellationToken = default);

    Task SaveAllAsync(Dictionary<string, bool> choices, CancellationToken cancellationToken = default);
}

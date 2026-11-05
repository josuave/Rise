using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// The preview-feature switchboard: which preview features are
/// opted into. A feature with no stored choice is off. The map is
/// cached after the first load — this service is the app's only
/// writer — and every change raises Changed so the shell and the
/// settings dialog can re-render live. Unknown stored ids are
/// ignored: they are harmless and forward-compatible with features
/// added later (or removed since).
/// </summary>
public sealed class PreviewFeatureService(IPreviewFeatureStore store)
{
    /// <summary>Raised after any successful opt-in or opt-out, so renderers can re-render.</summary>
    public event EventHandler? Changed;

    private Dictionary<string, bool>? _cached;

    /// <summary>Warms the cache; call once at startup so later reads never touch storage.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        _ = await GetMapAsync(cancellationToken);

    public async Task<bool> IsEnabledAsync(string featureId, CancellationToken cancellationToken = default)
    {
        Dictionary<string, bool> map = await GetMapAsync(cancellationToken);
        return map.TryGetValue(featureId, out bool enabled) && enabled;
    }

    /// <summary>Every registered feature with its current state, in registry order.</summary>
    public async Task<IReadOnlyList<(PreviewFeature Feature, bool Enabled)>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<string, bool> map = await GetMapAsync(cancellationToken);
        return PreviewFeatures.All
            .Select(feature => (feature, map.TryGetValue(feature.Id, out bool enabled) && enabled))
            .ToList();
    }

    public async Task SetEnabledAsync(string featureId, bool enabled, CancellationToken cancellationToken = default)
    {
        Dictionary<string, bool> map = await GetMapAsync(cancellationToken);
        map[featureId] = enabled;
        await store.SetAsync(featureId, enabled, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task<Dictionary<string, bool>> GetMapAsync(CancellationToken cancellationToken)
    {
        _cached ??= await store.GetAllAsync(cancellationToken);
        return _cached;
    }
}

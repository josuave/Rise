using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// The module switchboard: which modules are enabled. A module with no
/// stored choice is enabled. The map is cached after the first load —
/// this service is the app's only writer — and every change raises
/// Changed so the shell, the layout editor, and the settings dialog
/// can re-render.
/// </summary>
public sealed class ModuleService(IModuleStore store)
{
    /// <summary>Raised after any successful enable/disable, so renderers can re-filter.</summary>
    public event EventHandler? Changed;

    private Dictionary<string, bool>? _cached;

    public async Task<bool> IsEnabledAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        Dictionary<string, bool> map = await GetMapAsync(cancellationToken);
        if (!map.TryGetValue(moduleId, out bool enabled))
        {
            return true;
        }

        return enabled;
    }

    /// <summary>Ids of the given modules that are currently switched off, in one storage read.</summary>
    public async Task<HashSet<string>> DisabledIdsAsync(IReadOnlyList<ModuleInfo> modules, CancellationToken cancellationToken = default)
    {
        Dictionary<string, bool> map = await GetMapAsync(cancellationToken);
        return modules.Where(m => map.TryGetValue(m.Id, out bool enabled) && !enabled).Select(m => m.Id).ToHashSet();
    }

    public async Task SetEnabledAsync(string moduleId, bool enabled, CancellationToken cancellationToken = default)
    {
        Dictionary<string, bool> map = await GetMapAsync(cancellationToken);
        map[moduleId] = enabled;
        await store.SetAsync(moduleId, enabled, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task<Dictionary<string, bool>> GetMapAsync(CancellationToken cancellationToken)
    {
        _cached ??= await store.GetAllAsync(cancellationToken);
        return _cached;
    }
}

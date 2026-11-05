namespace Rise.Application.Ports;

/// <summary>
/// Outbound port: which modules are enabled, keyed by module id. The
/// map holds only explicit choices; a module with no entry is enabled
/// by default. The core never decides what a module is — the shell's
/// catalog owns that — this port just stores the choices.
/// </summary>
public interface IModuleStore
{
    /// <summary>Every explicit module choice, keyed by module id. Empty when nothing is stored or storage is corrupt.</summary>
    Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SetAsync(string moduleId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Replace the whole choice map (used by backup import, whose semantics are replace-all).</summary>
    Task SaveAllAsync(Dictionary<string, bool> choices, CancellationToken cancellationToken = default);
}

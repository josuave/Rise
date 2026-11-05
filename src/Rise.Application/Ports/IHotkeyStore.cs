using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>
/// Outbound port: explicit hotkey bindings, keyed by action id. The map
/// holds only overrides; an action with no entry uses its default. The
/// core never decides what an action is — <see cref="HotkeyActions"/>
/// owns that — this port just stores the overrides.
/// </summary>
public interface IHotkeyStore
{
    /// <summary>Every explicit binding override, keyed by action id. Empty when nothing is stored or storage is corrupt.</summary>
    Task<Dictionary<string, HotkeyBinding>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SetAsync(string actionId, HotkeyBinding binding, CancellationToken cancellationToken = default);

    /// <summary>Replace the whole override map (used by backup import, whose semantics are replace-all).</summary>
    Task SaveAllAsync(Dictionary<string, HotkeyBinding> bindings, CancellationToken cancellationToken = default);
}

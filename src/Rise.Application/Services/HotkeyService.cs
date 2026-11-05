using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for hotkeys: defaults from <see cref="HotkeyActions"/>,
/// overridden by stored bindings. Unknown stored ids are ignored, so a
/// backup from a newer version never breaks an older app. One binding
/// per action, and no two actions may share a binding — a conflicting
/// rebind fails with the action that already owns it, instead of
/// silently shadowing it.
/// </summary>
public sealed class HotkeyService(IHotkeyStore store)
{
    private Dictionary<string, HotkeyBinding>? _bindings;

    /// <summary>Effective bindings: defaults overlaid with stored overrides. Cached after the first load (single-writer page).</summary>
    public async Task<Dictionary<string, HotkeyBinding>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_bindings is not null)
        {
            return _bindings;
        }

        var effective = HotkeyActions.All.ToDictionary(action => action.Id, action => action.Default);
        Dictionary<string, HotkeyBinding> stored = await store.GetAllAsync(cancellationToken);
        foreach (KeyValuePair<string, HotkeyBinding> pair in stored.Where(pair =>
            HotkeyActions.ForId(pair.Key) is not null && !string.IsNullOrWhiteSpace(pair.Value.Key)))
        {
            effective[pair.Key] = pair.Value;
        }

        _bindings = effective;
        return _bindings;
    }

    /// <summary>Every action with its effective binding, in registry order.</summary>
    public async Task<IReadOnlyList<(HotkeyAction Action, HotkeyBinding Binding)>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<string, HotkeyBinding> bindings = await LoadAsync(cancellationToken);
        return HotkeyActions.All.Select(action => (action, bindings[action.Id])).ToList();
    }

    /// <summary>The action id whose binding matches this keystroke, or null. Falls back to defaults before the first load.</summary>
    public string? TryMatch(string key, bool mod, bool alt, bool shift)
    {
        Dictionary<string, HotkeyBinding> bindings = _bindings
            ?? HotkeyActions.All.ToDictionary(action => action.Id, action => action.Default);
        return HotkeyActions.All
            .FirstOrDefault(action => bindings[action.Id].Matches(key, mod, alt, shift))
            ?.Id;
    }

    public async Task<Result<HotkeyBinding>> SetAsync(string actionId, HotkeyBinding binding, CancellationToken cancellationToken = default)
    {
        HotkeyAction? action = HotkeyActions.ForId(actionId);
        if (action is null)
        {
            return Result<HotkeyBinding>.Failure("Unknown shortcut.");
        }

        if (string.IsNullOrWhiteSpace(binding.Key))
        {
            return Result<HotkeyBinding>.Failure("Press a key for the shortcut.");
        }

        Dictionary<string, HotkeyBinding> bindings = await LoadAsync(cancellationToken);
        HotkeyAction? owner = HotkeyActions.All.FirstOrDefault(other =>
            other.Id != actionId && bindings[other.Id].Matches(binding.Key, binding.Mod, binding.Alt, binding.Shift));
        if (owner is not null)
        {
            return Result<HotkeyBinding>.Failure($"Already used by {owner.Title}.");
        }

        bindings[actionId] = binding;
        await store.SetAsync(actionId, binding, cancellationToken);
        return Result<HotkeyBinding>.Success(binding);
    }

    /// <summary>Drop every override: all actions return to their defaults.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _bindings = HotkeyActions.All.ToDictionary(action => action.Id, action => action.Default);
        await store.SaveAllAsync(new Dictionary<string, HotkeyBinding>(), cancellationToken);
    }
}

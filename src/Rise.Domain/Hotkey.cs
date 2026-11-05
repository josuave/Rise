using System.Text.Json.Serialization;

namespace Rise.Domain;

/// <summary>
/// One keyboard shortcut: the key as reported by the browser
/// (<see cref="KeyboardEventArgs.Key"/>-style, e.g. "k", "/", "e") plus
/// its modifiers. Mod means Ctrl on Windows/Linux and Cmd on macOS —
/// the shell treats either as the modifier, so one binding works on
/// both. Matching is case-insensitive on the key but exact on the
/// modifiers, so Shift+E never fires a plain E binding.
/// </summary>
public sealed record HotkeyBinding(string Key, bool Mod, bool Alt, bool Shift)
{
    /// <summary>Human-readable form for the manager and the palette, e.g. "Ctrl+K" or "E".</summary>
    [JsonIgnore]
    public string Display
    {
        get
        {
            List<string> parts = new();
            if (Mod)
            {
                parts.Add("Ctrl");
            }

            if (Alt)
            {
                parts.Add("Alt");
            }

            if (Shift)
            {
                parts.Add("Shift");
            }

            parts.Add(Key == " " ? "Space" : Key.ToUpperInvariant());
            return string.Join("+", parts);
        }
    }

    public bool Matches(string key, bool mod, bool alt, bool shift) =>
        string.Equals(Key, key, StringComparison.OrdinalIgnoreCase)
        && Mod == mod
        && Alt == alt
        && Shift == shift;
}

/// <summary>A named, rebindable action: the id the binding is stored under, its manager title, and its default binding.</summary>
public sealed record HotkeyAction(string Id, string Title, HotkeyBinding Default);

/// <summary>The actions the hotkey system knows. The shell executes them; the manager lists them.</summary>
public static class HotkeyActions
{
    public static readonly IReadOnlyList<HotkeyAction> All = new[]
    {
        new HotkeyAction("open-palette", "Open command palette", new HotkeyBinding("k", Mod: true, Alt: false, Shift: false)),
        new HotkeyAction("toggle-edit-layout", "Toggle edit layout", new HotkeyBinding("e", Mod: false, Alt: false, Shift: false)),
        new HotkeyAction("toggle-theme", "Toggle light/dark theme", new HotkeyBinding("t", Mod: false, Alt: false, Shift: false)),
    };

    public static HotkeyAction? ForId(string id) => All.FirstOrDefault(action => action.Id == id);
}

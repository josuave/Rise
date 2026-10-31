namespace Rise.Slices.Abstractions;

/// <summary>
/// Contract a slice fulfills to inject its own behavior and UI into the
/// Settings dialog. The contract lives here, in Abstractions — not in
/// Application and not in the Settings slice — so a contributing slice
/// never depends on another slice or on the root application library: it
/// implements this interface, registers it in DI, and the shell wires it
/// up. The Settings dialog renders every registered section, ordered by
/// <see cref="Order"/>, via <c>DynamicComponent</c> with
/// <see cref="ComponentType"/> (the section's own Razor component, already
/// loaded as part of the app). That component is where the section's
/// behavior lives: it injects whatever core services it needs, exactly like
/// any other slice component, and the dialog never knows its internals.
/// Contributed sections render between the dialog's built-in Diagnostics
/// and Maintenance sections; <see cref="Order"/> orders them among themselves.
/// </summary>
public interface ISettingsSection
{
    /// <summary>Heading shown for the section in the dialog.</summary>
    string Title { get; }

    /// <summary>Lower values render first among contributed sections.</summary>
    int Order { get; }

    /// <summary>One-line description shown under the heading. May be empty.</summary>
    string Description { get; }

    /// <summary>
    /// The section's Razor component type. It must be a component type from an
    /// assembly already loaded by the app (a slice the shell references);
    /// <c>DynamicComponent</c> cannot load arbitrary plugin assemblies.
    /// </summary>
    Type ComponentType { get; }
}

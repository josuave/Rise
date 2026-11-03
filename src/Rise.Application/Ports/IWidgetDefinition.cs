using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>
/// A widget definition: everything the shell and the layout service
/// need to know about one widget, owned by the slice that renders it.
/// The core knows no concrete widgets — a new widget is a new
/// definition registered by its slice, never a change to this contract,
/// to the layout model, or to the shell. ComponentType is System.Type
/// only, so this contract keeps Application free of any Blazor
/// reference; the shell renders it via DynamicComponent. Group
/// containers are the one layout-level exception: they are a layout
/// concept, so the contract marks them with IsGroup and the layout
/// model carries their membership (see PageLayout.GroupChildren).
/// </summary>
public interface IWidgetDefinition
{
    /// <summary>Stable id, persisted in layouts. Changing it orphans stored layouts that mention the old one.</summary>
    string Id { get; }

    /// <summary>Display name, shown in the layout editor.</summary>
    string Title { get; }

    WidgetZone Zone { get; }

    /// <summary>Whether the widget shows in a fresh (or reset) layout.</summary>
    bool DefaultVisible { get; }

    /// <summary>Position among widgets in a fresh layout; lower comes first.</summary>
    int Order { get; }

    Type ComponentType { get; }

    /// <summary>
    /// Whether this widget is a group container: the shell renders its
    /// member widgets inside it, and the layout model carries its member
    /// ids on the layout rather than on placements. Opt-in; every
    /// existing definition is not a group.
    /// </summary>
    bool IsGroup => false;
}

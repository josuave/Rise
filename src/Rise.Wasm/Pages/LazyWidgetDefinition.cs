using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Wasm.Pages;

/// <summary>
/// Metadata definition for a lazily loaded widget: everything the
/// layout system needs (id, title, zone, order, visibility) without
/// the widget's assembly. The component that renders for it is the lazy
/// container, which loads the real assembly on first show and then
/// renders the real component. One instance per lazy widget — the
/// assembly file and component type are per-widget, not per container.
/// </summary>
internal sealed class LazyWidgetDefinition : IWidgetDefinition
{
    public static LazyWidgetDefinition Notes { get; } = new(
        "Notes", "Notes", 37, "Rise.Slices.Notes.wasm", "Rise.Slices.Notes.NotesWidget");

    public static LazyWidgetDefinition Game2048 { get; } = new(
        "Game2048", "2048", 38, "Rise.Slices.Game2048.wasm", "Rise.Slices.Game2048.Game2048Widget");

    public LazyWidgetDefinition(string id, string title, int order, string assemblyFile, string componentTypeName)
    {
        Id = id;
        Title = title;
        Order = order;
        AssemblyFile = assemblyFile;
        ComponentTypeName = componentTypeName;
    }

    public string Id { get; }

    public string Title { get; }

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order { get; }

    public Type ComponentType => typeof(LazyWidgetContainer);

    /// <summary>The lazy assembly file name, as passed to LazyAssemblyLoader.</summary>
    public string AssemblyFile { get; }

    /// <summary>Full name of the real component type inside the lazy assembly.</summary>
    public string ComponentTypeName { get; }
}

using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Wasm.Pages;

/// <summary>
/// Metadata definition for a lazily loaded widget: everything the
/// layout system needs (id, title, zone, order, visibility) without
/// the widget's assembly. The component that renders for it is the lazy
/// container, which loads the real assembly on first show and then
/// renders the real component.
/// </summary>
internal sealed class LazyWidgetDefinition : IWidgetDefinition
{
    public string Id => "Notes";

    public string Title => "Notes";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order => 37;

    public Type ComponentType => typeof(LazyWidgetContainer);

    /// <summary>The lazy assembly file name, as passed to LazyAssemblyLoader.</summary>
    public static string AssemblyFile => "Rise.Slices.Notes.wasm";

    /// <summary>Full name of the real component type inside the lazy assembly.</summary>
    public static string ComponentTypeName => "Rise.Slices.Notes.NotesWidget";
}

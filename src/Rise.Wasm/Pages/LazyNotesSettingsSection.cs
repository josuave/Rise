using Rise.Slices.Settings.Abstractions;

namespace Rise.Wasm.Pages;

/// <summary>
/// Metadata for the Notes settings section: Notes is a lazy module,
/// so its section cannot be registered from the slice at startup.
/// The dialog renders it through the lazy section container, which
/// loads the Notes assembly and then renders the real section.
/// Title, order, and description mirror the slice's section values.
/// </summary>
internal sealed class LazyNotesSettingsSection : ILazySettingsSection
{
    public string Title => "Notes";

    public int Order => 140;

    public string Description => "List or post-it view.";

    public Type ComponentType => typeof(Rise.Slices.Settings.LazySectionContainer);

    public string AssemblyFile => "Rise.Slices.Notes.wasm";

    public string ComponentTypeName => "Rise.Slices.Notes.NotesViewSettings";
}

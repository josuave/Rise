namespace Rise.Slices.Settings.Abstractions;

/// <summary>
/// A settings section whose component lives in a lazily loaded
/// assembly. The dialog cannot render it through a plain
/// <see cref="ISettingsSection.ComponentType"/> — that type must
/// already be loaded — so it renders the section through the lazy
/// section container instead, passing this metadata along. The
/// container type itself lives in an always-loaded assembly; only the
/// section component is lazy.
/// </summary>
public interface ILazySettingsSection : ISettingsSection
{
    /// <summary>The lazy assembly file name, as passed to LazyAssemblyLoader (for example "Rise.Slices.Notes.wasm").</summary>
    string AssemblyFile { get; }

    /// <summary>Full name of the real section component type inside the lazy assembly.</summary>
    string ComponentTypeName { get; }
}

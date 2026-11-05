using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Settings;

/// <summary>
/// Container for a lazily loaded settings section: on first render it
/// fetches the section's assembly through LazyAssemblyLoader, resolves
/// the real section component type by name, and renders it. Later
/// renders reuse the loaded assembly.
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class LazySectionContainer
{
    [Parameter]
    public ILazySettingsSection? Section { get; set; }

    [Inject]
    private LazyAssemblyLoader AssemblyLoader { get; set; } = null!;

    private Type? ResolvedType { get; set; }

    private string? Error { get; set; }

    private string? _loadedFor;

    protected override async Task OnParametersSetAsync()
    {
        if (ResolvedType is not null && _loadedFor == Section?.AssemblyFile)
        {
            return;
        }

        ResolvedType = null;
        Error = null;
        _loadedFor = null;

        if (Section is null)
        {
            Error = "No lazy section was given.";
            return;
        }

        try
        {
            IEnumerable<System.Reflection.Assembly> assemblies = await AssemblyLoader.LoadAssembliesAsync(new[] { Section.AssemblyFile });
            ResolvedType = assemblies.Select(a => a.GetType(Section.ComponentTypeName)).FirstOrDefault(t => t is not null)
                // A second load of an already-loaded assembly does not
                // return it again, so fall back to the loaded assemblies.
                ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(Section.ComponentTypeName)).FirstOrDefault(t => t is not null);
            if (ResolvedType is null)
            {
                Error = $"Loaded {Section.AssemblyFile} but found no {Section.ComponentTypeName}.";
            }
            else
            {
                _loadedFor = Section.AssemblyFile;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.IO.FileNotFoundException or HttpRequestException or JSException)
        {
            Error = $"Couldn't load the {Section.Title} section: {ex.Message}";
        }
    }
}

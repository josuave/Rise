using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Wasm.Pages;

/// <summary>
/// Container for a lazily loaded widget: on first show it fetches the
/// widget's assembly through LazyAssemblyLoader, resolves the real
/// component type by name, and renders it. Later shows reuse the
/// loaded assembly.
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class LazyWidgetContainer
{
    [Parameter]
    public string WidgetId { get; set; } = string.Empty;

    [Inject]
    private IEnumerable<IWidgetDefinition> Definitions { get; set; } = Array.Empty<IWidgetDefinition>();

    [Inject]
    private LazyAssemblyLoader AssemblyLoader { get; set; } = null!;

    private Type? ResolvedType { get; set; }

    private string? Error { get; set; }

    private string? _loadedFor;

    protected override async Task OnParametersSetAsync()
    {
        if (ResolvedType is not null && _loadedFor == WidgetId)
        {
            return;
        }

        ResolvedType = null;
        Error = null;
        _loadedFor = null;

        LazyWidgetDefinition? definition = Definitions.OfType<LazyWidgetDefinition>().FirstOrDefault(d => d.Id == WidgetId);
        if (definition is null)
        {
            Error = $"No lazy widget '{WidgetId}'.";
            return;
        }

        try
        {
            IEnumerable<System.Reflection.Assembly> assemblies = await AssemblyLoader.LoadAssembliesAsync(new[] { LazyWidgetDefinition.AssemblyFile });
            ResolvedType = assemblies.Select(a => a.GetType(LazyWidgetDefinition.ComponentTypeName)).FirstOrDefault(t => t is not null)
                // A second load of an already-loaded assembly does not
                // return it again, so fall back to the loaded assemblies.
                ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(LazyWidgetDefinition.ComponentTypeName)).FirstOrDefault(t => t is not null);
            if (ResolvedType is null)
            {
                Error = $"Loaded {LazyWidgetDefinition.AssemblyFile} but found no {LazyWidgetDefinition.ComponentTypeName}.";
            }
            else
            {
                _loadedFor = WidgetId;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.IO.FileNotFoundException or HttpRequestException or JSException)
        {
            Error = $"Couldn't load the {definition.Title} widget: {ex.Message}";
        }
    }
}

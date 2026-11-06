using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Search;

/// <summary>
/// Imperative shell for the Search slice: it collects input and calls the application
/// service. Query building is pure core; opening the tab is an infrastructure adapter.
/// The box shows the configured engine's favicon in place of a search button —
/// Enter runs the search — and clicking the favicon opens a quick engine picker
/// that sets the configured engine, the same choice the Settings section makes.
/// The placeholder names the engine and follows engine changes live; the engine
/// resolves through the provider, so the box and the command palette never disagree.
/// </summary>
public partial class Search : IDisposable
{
    [Inject]
    private SearchService? SearchService { get; set; }

    [Inject]
    private ISearchEngineProvider EngineProvider { get; set; } = null!;

    [Inject]
    private SearchConfig Config { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    public string? SearchString { get; set; } = string.Empty;

    private SearchEngine CurrentEngine { get; set; } = SearchEngines.Default;

    private bool MenuOpen { get; set; }

    private HashSet<string> FaviconFailed { get; } = new();

    /// <summary>Built-ins first, then the user-added engines in the order they were added.</summary>
    private IReadOnlyList<SearchEngine> Engines => SearchEngines.All.Concat(Config.CustomEngines).ToList();

    /// <summary>The placeholder names the engine this search will use.</summary>
    private string SearchPlaceholder => $"Search {CurrentEngine.Name}…";

    protected override async Task OnInitializedAsync()
    {
        await Config.LoadAsync();
        await RefreshEngineAsync();
        ConfigService.ConfigChanged += OnConfigChanged;
    }

    public async Task SearchOnEnter(KeyboardEventArgs args)
    {
        if (args.Code == "Enter" || args.Code == "NumpadEnter")
        {
            await RunSearch();
        }
    }

    public async Task RunSearch()
    {
        if (SearchService is not null)
        {
            await SearchService.LaunchSearchAsync(SearchString);
        }
    }

    private void ToggleMenu() => MenuOpen = !MenuOpen;

    private void CloseMenu() => MenuOpen = false;

    private void OnKeyDown(KeyboardEventArgs args)
    {
        if (MenuOpen && args.Code == "Escape")
        {
            MenuOpen = false;
        }
    }

    private void OnFaviconError(string engineId) => FaviconFailed.Add(engineId);

    private bool IsActive(SearchEngine engine) => CurrentEngine.Id == engine.Id;

    private async Task SetEngineAsync(SearchEngine engine)
    {
        Config.EngineId = engine.Id;
        await Config.SaveAsync();
        MenuOpen = false;
    }

    private async Task RefreshEngineAsync() =>
        CurrentEngine = await EngineProvider.GetEngineAsync();

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != SearchWidgetDefinition.WidgetId)
        {
            return;
        }

        await RefreshEngineAsync();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ConfigService.ConfigChanged -= OnConfigChanged;
        }
    }
}

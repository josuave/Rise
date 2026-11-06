using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rise.Application.Ports;
using Rise.Application.Services;

namespace Rise.Slices.Search;

/// <summary>
/// Imperative shell for the Search slice: it collects input and calls the application
/// service. Query building is pure core; opening the tab is an infrastructure adapter.
/// The placeholder names the configured engine and follows engine changes made in
/// Settings live; the engine itself resolves through the provider, so the box and
/// the command palette never disagree.
/// </summary>
public partial class Search : IDisposable
{
    [Inject]
    private SearchService? SearchService { get; set; }

    [Inject]
    private ISearchEngineProvider EngineProvider { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    public string? SearchString { get; set; } = string.Empty;

    private string EngineName { get; set; } = "Google";

    /// <summary>The placeholder names the engine this search will use.</summary>
    private string SearchPlaceholder => $"Search {EngineName}…";

    protected override async Task OnInitializedAsync()
    {
        await RefreshEngineNameAsync();
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

    private async Task RefreshEngineNameAsync() =>
        EngineName = (await EngineProvider.GetEngineAsync()).Name;

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != SearchWidgetDefinition.WidgetId)
        {
            return;
        }

        await RefreshEngineNameAsync();
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

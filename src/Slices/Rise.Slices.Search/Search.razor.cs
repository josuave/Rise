using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Search;

/// <summary>
/// Imperative shell for the Search slice: it collects input and calls the application
/// service. Query building is pure core; opening the tab is an infrastructure adapter.
/// The placeholder names the configured engine and follows engine changes made in
/// Settings live; the search itself resolves the engine through the provider, so the
/// box and the command palette never disagree.
/// </summary>
public partial class Search : IDisposable
{
    [Inject]
    private SearchService? SearchService { get; set; }

    [Inject]
    private SearchConfig Config { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    public string? SearchString { get; set; } = string.Empty;

    /// <summary>The placeholder names the engine this search will use.</summary>
    private string SearchPlaceholder => $"Search {SearchEngines.FromId(Config.EngineId).Name}…";

    protected override async Task OnInitializedAsync()
    {
        await Config.LoadAsync();
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

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != SearchWidgetDefinition.WidgetId)
        {
            return;
        }

        await Config.LoadAsync();
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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rise.Application.Services;

namespace Rise.Slices.Search;

/// <summary>
/// Imperative shell for the Search slice: it collects input and calls the application
/// service. Query building is pure core; opening the tab is an infrastructure adapter.
/// </summary>
public partial class Search
{
    [Inject]
    private SearchService? SearchService { get; set; }

    public string? SearchString { get; set; } = string.Empty;

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
}

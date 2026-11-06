using Microsoft.AspNetCore.Components;
using Rise.Domain;

namespace Rise.Slices.Search;

/// <summary>
/// The Search slice's Settings section content: one button per
/// engine; the pick saves immediately.
/// </summary>
public partial class SearchSettings
{
    [Inject]
    private SearchConfig Config { get; set; } = null!;

    protected override async Task OnInitializedAsync() => await Config.LoadAsync();

    private bool IsActive(SearchEngine engine) => Config.EngineId == engine.Id;

    private async Task SetEngineAsync(SearchEngine engine)
    {
        Config.EngineId = engine.Id;
        await Config.SaveAsync();
    }
}

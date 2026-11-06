using Microsoft.AspNetCore.Components;
using Rise.Domain;

namespace Rise.Slices.Search;

/// <summary>
/// The Search slice's Settings section content: one button per
/// engine, showing the engine's own favicon (a letter tile when the
/// favicon cannot load), plus the form to add a custom engine and a
/// delete button on each custom row. Picks, adds, and deletes save
/// immediately.
/// </summary>
public partial class SearchSettings
{
    [Inject]
    private SearchConfig Config { get; set; } = null!;

    private string NewEngineName { get; set; } = string.Empty;

    private string NewEngineUrl { get; set; } = string.Empty;

    private string? FormError { get; set; }

    private HashSet<string> FaviconFailed { get; } = new();

    /// <summary>Built-ins first, then the user-added engines in the order they were added.</summary>
    private IReadOnlyList<SearchEngine> Engines => SearchEngines.All.Concat(Config.CustomEngines).ToList();

    protected override async Task OnInitializedAsync() => await Config.LoadAsync();

    private bool IsActive(SearchEngine engine) => Config.EngineId == engine.Id;

    private static bool IsCustom(SearchEngine engine) =>
        engine.Id.StartsWith(SearchEngine.CustomPrefix, StringComparison.Ordinal);

    private void OnFaviconError(string engineId) => FaviconFailed.Add(engineId);

    private async Task SetEngineAsync(SearchEngine engine)
    {
        Config.EngineId = engine.Id;
        await Config.SaveAsync();
    }

    private async Task AddEngineAsync()
    {
        string url = NewEngineUrl.Trim();
        if (string.IsNullOrWhiteSpace(NewEngineName))
        {
            FormError = "Give the engine a name.";
            return;
        }

        if (!SearchEngine.IsValidTemplate(url))
        {
            FormError = "Enter a search URL with %s where the query goes.";
            return;
        }

        string id = SearchEngine.NewCustomId(
            NewEngineName,
            Config.CustomEngines.Select(engine => engine.Id).Concat(SearchEngines.All.Select(engine => engine.Id)));
        var engine = new SearchEngine(id, NewEngineName.Trim(), url);
        Config.CustomEngines = Config.CustomEngines.Append(engine).ToList();
        Config.EngineId = id;
        await Config.SaveAsync();
        NewEngineName = string.Empty;
        NewEngineUrl = string.Empty;
        FormError = null;
    }

    private async Task DeleteEngineAsync(SearchEngine engine)
    {
        Config.CustomEngines = Config.CustomEngines.Where(candidate => candidate.Id != engine.Id).ToList();
        if (Config.EngineId == engine.Id)
        {
            Config.EngineId = SearchEngines.Default.Id;
        }

        await Config.SaveAsync();
    }
}

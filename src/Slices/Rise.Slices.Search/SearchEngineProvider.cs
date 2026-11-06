using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Search;

/// <summary>The Search slice's fulfillment of the engine-provider port: the configured engine — a user-added one when the choice names it, else a built-in — from the search widget's own config.</summary>
public sealed class SearchEngineProvider(SearchConfig config) : ISearchEngineProvider
{
    public async Task<SearchEngine> GetEngineAsync(CancellationToken cancellationToken = default)
    {
        await config.LoadAsync(cancellationToken);
        string id = config.EngineId;
        if (id.StartsWith(SearchEngine.CustomPrefix, StringComparison.Ordinal))
        {
            SearchEngine? custom = config.CustomEngines.FirstOrDefault(engine => engine.Id == id);
            if (custom is not null)
            {
                return custom;
            }
        }

        return SearchEngines.FromId(id);
    }
}

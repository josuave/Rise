using Rise.Application.Ports;

namespace Rise.Slices.Search;

/// <summary>The Search slice's fulfillment of the engine-provider port: the configured engine id, from the search widget's own config.</summary>
public sealed class SearchEngineProvider(SearchConfig config) : ISearchEngineProvider
{
    public async Task<string> GetEngineIdAsync(CancellationToken cancellationToken = default)
    {
        await config.LoadAsync(cancellationToken);
        return config.EngineId;
    }
}

using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Supplies the configured search engine, built-in or user-added. The Search slice implements it from its own widget config; the search service reads it so the search box and the command palette share one resolution.</summary>
public interface ISearchEngineProvider
{
    Task<SearchEngine> GetEngineAsync(CancellationToken cancellationToken = default);
}

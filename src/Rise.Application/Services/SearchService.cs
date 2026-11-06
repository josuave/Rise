using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>Imperative shell for search: build the URL purely, then hand it to the launcher port. The engine comes from the configured provider when one is registered, else Google.</summary>
public sealed class SearchService(ISearchLauncher launcher, ISearchEngineProvider? engineProvider = null)
{
    public async Task<bool> LaunchSearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        SearchEngine engine = engineProvider is null
            ? SearchEngines.Default
            : await engineProvider.GetEngineAsync(cancellationToken);
        Uri? url = SearchQuery.Build(query, engine);
        if (url is null)
        {
            return false;
        }

        await launcher.LaunchAsync(url, cancellationToken);
        return true;
    }
}

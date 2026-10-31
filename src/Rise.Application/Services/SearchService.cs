using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>Imperative shell for search: build the URL purely, then hand it to the launcher port.</summary>
public sealed class SearchService(ISearchLauncher launcher)
{
    public async Task<bool> LaunchSearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var url = SearchQuery.Build(query);
        if (url is null)
        {
            return false;
        }

        await launcher.LaunchAsync(url, cancellationToken);
        return true;
    }
}

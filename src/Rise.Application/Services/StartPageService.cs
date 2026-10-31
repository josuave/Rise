using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for links: the only place that talks to the store.
/// It loads a page, hands it to the pure core (StartPage.AddLink/RemoveLink),
/// and persists whatever the core returns. It contains no link rules itself.
/// </summary>
public sealed class StartPageService(ILinkStore store)
{
    public async Task<StartPage> LoadAsync(CancellationToken cancellationToken = default) =>
        await store.LoadAsync(cancellationToken) ?? StartPage.Default();

    public async Task<Result<StartPage>> AddLinkAsync(StartPage page, string name, string url, CancellationToken cancellationToken = default)
    {
        var result = page.AddLink(name, url);
        if (result.IsSuccess && result.Value is not null)
        {
            await store.SaveAsync(result.Value, cancellationToken);
        }
        return result;
    }

    public async Task<StartPage> RemoveLinkAsync(StartPage page, Guid id, CancellationToken cancellationToken = default)
    {
        var next = page.RemoveLink(id);
        await store.SaveAsync(next, cancellationToken);
        return next;
    }
}

using System.Diagnostics.CodeAnalysis;
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
    /// <summary>
    /// Raised after any successful save, so tiles and editor widgets that
    /// render the same page can reload instead of going stale.
    /// </summary>
    public event EventHandler? Changed;

    public async Task<StartPage> LoadAsync(CancellationToken cancellationToken = default) =>
        await store.LoadAsync(cancellationToken) ?? StartPage.Default();

    // CA1054 does not fit here either: this only forwards unvalidated user
    // input to StartPage.AddLink, which validates it (see its suppression).
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Forwards unvalidated user input to StartPage.AddLink for validation.")]
    public async Task<Result<StartPage>> AddLinkAsync(StartPage page, string name, string url, CancellationToken cancellationToken = default)
    {
        Result<StartPage> result = page.AddLink(name, url);
        if (result.IsSuccess && result.Value is not null)
        {
            await store.SaveAsync(result.Value, cancellationToken);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        return result;
    }

    /// <summary>Saves the default page, discarding any saved links.</summary>
    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(StartPage.Default(), cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<StartPage> RemoveLinkAsync(StartPage page, Guid id, CancellationToken cancellationToken = default)
    {
        StartPage next = page.RemoveLink(id);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

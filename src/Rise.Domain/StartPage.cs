using System.Diagnostics.CodeAnalysis;

namespace Rise.Domain;

/// <summary>
/// The start page aggregate. Pure functional core: every operation takes a page
/// and returns a new page (or a failure) — it never touches storage, the browser, or the clock.
/// The imperative shell (application services) is what persists the returned page.
/// </summary>
public sealed record StartPage(IReadOnlyList<Link> Links)
{
    public static StartPage Default() => new(new[]
    {
        Link.Create("Google", new LinkUrl("https://www.google.com/")),
        Link.Create("GitHub", new LinkUrl("https://github.com/")),
        Link.Create("YouTube", new LinkUrl("https://www.youtube.com/")),
        Link.Create("Wikipedia", new LinkUrl("https://www.wikipedia.org/")),
    });

    // CA1054 does not fit here: the parameter is unvalidated user input.
    // LinkUrl.TryCreate validates it and reports failures via Result;
    // a Uri parameter could not represent invalid input at all.
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Parameter is unvalidated user input validated by LinkUrl.TryCreate.")]
    public Result<StartPage> AddLink(string name, string urlInput)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<StartPage>.Failure("Give the link a name and a URL.");
        }

        if (!LinkUrl.TryCreate(urlInput, out LinkUrl? url, out string? error) || url is null)
        {
            return Result<StartPage>.Failure(error ?? "That doesn't look like a valid web address.");
        }

        if (Links.Any(l => string.Equals(l.Url.Value, url.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<StartPage>.Failure("That link is already on the page.");
        }

        return Result<StartPage>.Success(new StartPage([..Links, Link.Create(name, url)]));
    }

    public StartPage RemoveLink(Guid id) =>
        new(Links.Where(l => l.Id != id).ToList());
}

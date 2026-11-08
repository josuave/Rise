using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// The Links slice's injected Settings section (see <see cref="LinksSettingsSection"/>).
/// Behavior lives here, in the slice, not in the Settings dialog: it loads the
/// page through the core service, adds, edits, and removes links, and resets to defaults.
/// This is the links' only editor. Tags are entered as one comma-separated
/// text input; the sort choice lives in the Links widget config, so it
/// rides backup with the rest of the widget configs. Adding a link (or
/// changing its URL while editing) derives its icon from the URL's host
/// via Google's favicon service; an edit that leaves the URL unchanged
/// keeps the stored icon. The widget config is resolved optionally:
/// the settings dialog can render this section without the widget
/// registration (some hosts only register the section), in which case
/// the sort reads as Custom and changing it is a no-op. The same
/// optional pattern covers the file downloader: the dialog can render
/// this section without it, in which case the Export button is hidden
/// and export reports that the download could not be started.
/// Transfer lives here too: import/export of Netscape bookmark files,
/// and the bookmarklet receive — a <c>?add=&lt;url&gt;&amp;title=…&amp;notes=…</c>
/// query the bookmarklet opens, which pre-fills a pending-add card
/// (the Settings dialog cannot be opened pre-filled from here; that
/// needs a SettingsGear change, held for after PR #59 merges).
/// </summary>
public partial class LinksSettings
{
    private const long MaxImportBytes = 1_000_000;

    private const string ExportFilename = "rise-bookmarks.html";

    /// <summary>Re-key for the import <see cref="InputFile"/>: bumped per import so the element is fresh afterwards.</summary>
    private int ImportCount;

    /// <summary>
    /// Drag the link to the bookmarks bar; clicking it on any page opens
    /// Rise with that page's address, title, and selected text. It opens
    /// in a new tab so the page stays open, and hardcodes the live origin
    /// — Rise has no backend, so there is no other address to receive it at.
    /// </summary>
    private const string BookmarkletHref = "javascript:(function(){var u=location.href;var t=document.title;var s='';try{s=window.getSelection?window.getSelection().toString():'';}catch(e){}window.open('https://josuave.github.io/Rise/?add='+encodeURIComponent(u)+'&title='+encodeURIComponent(t)+'&notes='+encodeURIComponent(s),'_blank');})();";

    [Inject]
    private StartPageService? StartPageService { get; set; }

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private LinksConfig? Config { get; set; }

    private IFileDownloader? FileDownloader { get; set; }

    private StartPage Page { get; set; } = StartPage.Default();
    private string? Message { get; set; }

    private string NewName { get; set; } = string.Empty;
    private string NewUrl { get; set; } = string.Empty;
    private string NewDescription { get; set; } = string.Empty;
    private string NewTags { get; set; } = string.Empty;
    private bool NewPinned { get; set; }
    private string? FormError { get; set; }

    private Guid? EditingId { get; set; }
    private string EditName { get; set; } = string.Empty;
    private string EditUrl { get; set; } = string.Empty;
    private string EditDescription { get; set; } = string.Empty;
    private string EditTags { get; set; } = string.Empty;
    private bool EditPinned { get; set; }
    private string EditOriginalUrl { get; set; } = string.Empty;

    /// <summary>Whether a bookmarklet pending-add card is showing (a <c>?add=</c> query arrived).</summary>
    private bool HasPendingAdd { get; set; }

    private string PendingName { get; set; } = string.Empty;
    private string PendingUrl { get; set; } = string.Empty;
    private string PendingNotes { get; set; } = string.Empty;
    private string PendingTags { get; set; } = string.Empty;
    private string? PendingError { get; set; }

    /// <summary>The sort shown in the sort picker; stored as the enum name in the widget config. Custom when the config is absent.</summary>
    private string SortName => Config?.Sort.ToString() ?? LinkSort.Custom.ToString();

    protected override async Task OnInitializedAsync()
    {
        Config = ServiceProvider.GetService<LinksConfig>();
        FileDownloader = ServiceProvider.GetService<IFileDownloader>();
        if (Config is not null)
        {
            await Config.LoadAsync();
        }

        if (StartPageService is not null)
        {
            Page = await StartPageService.LoadAsync();
        }

        ReadPendingAddFromQuery();
    }

    /// <summary>
    /// Reads the bookmarklet query (<c>?add=&lt;url&gt;&amp;title=…&amp;notes=…</c>)
    /// into the pending-add card. The query is cleared only when the
    /// user saves or dismisses the card, so closing Settings without
    /// deciding keeps the pending add — and a reload before deciding
    /// shows it again.
    /// </summary>
    private void ReadPendingAddFromQuery()
    {
        string? add = QueryValue("add");
        if (string.IsNullOrWhiteSpace(add))
        {
            return;
        }

        HasPendingAdd = true;
        PendingUrl = add.Trim();
        string? title = QueryValue("title");
        PendingName = string.IsNullOrWhiteSpace(title) ? PendingUrl : title.Trim();
        PendingNotes = QueryValue("notes") ?? string.Empty;
        PendingTags = string.Empty;
        PendingError = null;
    }

    private string? QueryValue(string key)
    {
        string query;
        try
        {
            query = new Uri(Navigation.Uri).Query;
        }
        catch (UriFormatException)
        {
            return null;
        }

        if (query.Length <= 1)
        {
            return null;
        }

        foreach (string pair in query[1..].Split('&'))
        {
            int equals = pair.IndexOf('=', StringComparison.Ordinal);
            string name = equals < 0 ? pair : pair[..equals];
            if (!string.Equals(DecodeQuery(name), key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = equals < 0 ? string.Empty : pair[(equals + 1)..];
            return DecodeQuery(value);
        }

        return null;
    }

    private static string? DecodeQuery(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    // The app's own base, not "/": the deployed base is /Rise/, so
    // "/" would navigate away from the app to the site root.
    private void ClearAddQuery()
    {
        try
        {
            Navigation.NavigateTo(Navigation.BaseUri, replace: true);
        }
        catch (InvalidOperationException)
        {
            // The page is already navigating away; the query goes with it.
        }
    }

    private async Task SavePendingAddAsync()
    {
        if (StartPageService is null || !HasPendingAdd)
        {
            return;
        }

        Result<StartPage> result = await StartPageService.AddLinkAsync(
            Page, PendingName, PendingUrl, PendingNotes, ParseTags(PendingTags), false, LinkFavicon.ForUrl(PendingUrl));
        switch (result)
        {
            case Ok<StartPage> ok:
                Page = ok.Value;
                HasPendingAdd = false;
                PendingError = null;
                Message = "Link added.";
                ClearAddQuery();
                break;
            case Error error:
                PendingError = error.Message;
                break;
            case null:
                PendingError = null;
                break;
        }
    }

    private void DismissPendingAdd()
    {
        HasPendingAdd = false;
        PendingError = null;
        ClearAddQuery();
    }

    /// <summary>
    /// Imports a Netscape bookmark file: links already saved are
    /// skipped (case-insensitive on the address), the rest are merged
    /// with their parsed tags, notes, and creation dates, and the
    /// result line says how many landed and how many were skipped.
    /// </summary>
    private async Task ImportAsync(InputFileChangeEventArgs args)
    {
        if (StartPageService is null)
        {
            return;
        }

        // A change event with no file (e.g. the same file re-picked)
        // is a no-op, not an error: args.File throws when empty.
        if (args.FileCount == 0)
        {
            return;
        }

        // A fresh input element after each import, so picking the same
        // file again fires a change event instead of being a no-op
        // (the browser only fires when the input's value changes).
        ImportCount++;

        string? html = await ReadFileAsync(args.File);
        if (html is null)
        {
            Message = "That file couldn't be read.";
            return;
        }

        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(html, DateTimeOffset.UtcNow);
        var links = parsed.Select(ToLink).ToList();
        (StartPage next, int added, int skipped) = await StartPageService.ImportLinksAsync(Page, links);
        Page = next;
        Message = $"Imported {added} links, skipped {skipped} already saved.";
    }

    private static Link ToLink(ParsedBookmark bookmark) =>
        new(
            Guid.NewGuid(),
            bookmark.Name,
            new LinkUrl(bookmark.Url),
            bookmark.Description,
            bookmark.Tags,
            LinkFavicon.ForUrl(bookmark.Url),
            false,
            0,
            bookmark.CreatedAt);

    private async Task ExportAsync()
    {
        if (FileDownloader is null)
        {
            Message = "The download couldn't be started.";
            return;
        }

        try
        {
            await FileDownloader.DownloadAsync(ExportFilename, NetscapeBookmarks.Export(Page.Links));
            Message = "Bookmarks exported.";
        }
        catch (InvalidOperationException)
        {
            Message = "The download couldn't be started.";
        }
    }

    private static async Task<string?> ReadFileAsync(IBrowserFile file)
    {
        if (file.Size > MaxImportBytes)
        {
            return null;
        }

        try
        {
            await using Stream stream = file.OpenReadStream(MaxImportBytes);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return null;
        }
    }

    private static List<string> ParseTags(string input) =>
        input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string SortLabel(string name) => name switch
    {
        "Custom" => "Custom order",
        "DateAdded" => "Date added",
        _ => name,
    };

    private bool IsEditing(Link link) => EditingId == link.Id;

    private void StartEdit(Link link)
    {
        EditingId = link.Id;
        EditName = link.Name;
        EditUrl = link.Url.Value;
        EditOriginalUrl = link.Url.Value;
        EditDescription = link.Description ?? string.Empty;
        EditTags = string.Join(", ", link.Tags);
        EditPinned = link.IsPinned;
        FormError = null;
    }

    private void CancelEdit() => EditingId = null;

    private async Task OnSortChangedAsync(ChangeEventArgs args)
    {
        if (Config is null)
        {
            return;
        }

        if (args.Value is string name && Enum.TryParse(name, out LinkSort sort))
        {
            Config.Sort = sort;
            await Config.SaveAsync();
        }
    }

    private async Task AddAsync()
    {
        if (StartPageService is null)
        {
            return;
        }

        Result<StartPage> result = await StartPageService.AddLinkAsync(
            Page, NewName, NewUrl, NewDescription, ParseTags(NewTags), NewPinned, LinkFavicon.ForUrl(NewUrl));
        switch (result)
        {
            case Ok<StartPage> ok:
                Page = ok.Value;
                NewName = string.Empty;
                NewUrl = string.Empty;
                NewDescription = string.Empty;
                NewTags = string.Empty;
                NewPinned = false;
                FormError = null;
                break;
            case Error error:
                FormError = error.Message;
                break;
            case null:
                FormError = null;
                break;
        }
    }

    private async Task SaveEditAsync()
    {
        if (StartPageService is null || EditingId is null)
        {
            return;
        }

        // A changed URL gets a fresh icon; an unchanged URL keeps the stored one.
        string? iconAddress = string.Equals(EditUrl, EditOriginalUrl, StringComparison.OrdinalIgnoreCase)
            ? null
            : LinkFavicon.ForUrl(EditUrl);
        Result<StartPage> result = await StartPageService.UpdateLinkAsync(
            Page, EditingId.Value, EditName, EditUrl, EditDescription, ParseTags(EditTags), EditPinned, iconAddress);
        switch (result)
        {
            case Ok<StartPage> ok:
                Page = ok.Value;
                EditingId = null;
                FormError = null;
                break;
            case Error error:
                FormError = error.Message;
                break;
            case null:
                FormError = null;
                break;
        }
    }

    private async Task RemoveAsync(Link link)
    {
        if (StartPageService is null)
        {
            return;
        }

        Page = await StartPageService.RemoveLinkAsync(Page, link.Id);
        Message = null;
    }

    private async Task ResetAsync()
    {
        if (StartPageService is null)
        {
            return;
        }

        await StartPageService.ResetToDefaultsAsync();
        Page = await StartPageService.LoadAsync();
        Message = "Links reset to defaults.";
    }
}

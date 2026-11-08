using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Links;

/// <summary>
/// The Links widget: tiles (Compact, the default) or cards
/// (Detailed), with a live quick filter, a tag filter bar, tag
/// pills, note previews, favicons with a letter-tile fallback, and
/// click tracking. The view mode and sort live in the widget config
/// store; the sort choice itself is made in the Links settings
/// section. Editing lives in the Settings dialog, not here. Tiles
/// stay fresh because the component reloads whenever the link
/// service saves or its own config changes.
/// </summary>
public partial class QuickLinks : IDisposable
{
    [Inject]
    private StartPageService? StartPageService { get; set; }

    [Inject]
    private LinksConfig Config { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private StartPage Page { get; set; } = StartPage.Default();

    private string SearchQuery { get; set; } = string.Empty;

    private string? SelectedTag { get; set; }

    private HashSet<Guid> ExpandedNotes { get; } = new();

    private HashSet<Guid> FaviconFailed { get; } = new();

    private bool IsDetailed => Config.IsDetailed;

    /// <summary>The links in the configured sort, filtered by the selected tag and the quick filter (both must match).</summary>
    private List<Link> VisibleLinks()
    {
        IEnumerable<Link> links = Page.Sorted(Config.Sort);
        if (SelectedTag is not null)
        {
            links = links.Where(l => l.Tags.Contains(SelectedTag, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            string query = SearchQuery.Trim();
            links = links.Where(l => MatchesQuery(l, query));
        }

        return links.ToList();
    }

    /// <summary>Every tag in use across the sorted links, in first-seen order.</summary>
    private List<string> TagsInUse() =>
        Page.Sorted(Config.Sort).SelectMany(l => l.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    protected override async Task OnInitializedAsync()
    {
        await Config.LoadAsync();
        if (StartPageService is not null)
        {
            Page = await StartPageService.LoadAsync();
            StartPageService.Changed += OnPageChanged;
        }

        ConfigService.ConfigChanged += OnConfigChanged;
    }

    private static bool MatchesQuery(Link link, string query) =>
        link.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || link.Url.Value.Contains(query, StringComparison.OrdinalIgnoreCase)
        || link.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))
        || link.Description is not null && link.Description.Contains(query, StringComparison.OrdinalIgnoreCase);

    private string NoteToggleLabel(Link link) =>
        (ExpandedNotes.Contains(link.Id) ? "Hide full note for " : "Show full note for ") + link.Name;

    private static string? DomainOf(Link link) => LinkFavicon.DomainOf(link.Url.Value);

    private void SelectTag(string? tag) => SelectedTag = tag;

    private void ToggleNote(Guid id)
    {
        if (!ExpandedNotes.Remove(id))
        {
            ExpandedNotes.Add(id);
        }
    }

    private void OnFaviconError(Guid id) => FaviconFailed.Add(id);

    private async Task ToggleViewAsync()
    {
        Config.ViewMode = Config.IsDetailed ? "Compact" : "Detailed";
        await Config.SaveAsync();
    }

    /// <summary>
    /// Records the click (persisted) without breaking navigation:
    /// the anchor keeps its href and target, so the link still opens
    /// — the record is fire-and-forget.
    /// </summary>
    private void OnLinkClick(Link link)
    {
        if (StartPageService is not null)
        {
            _ = StartPageService.RecordClickAsync(Page, link.Id);
        }
    }

    private async void OnPageChanged(object? sender, EventArgs args)
    {
        if (StartPageService is null)
        {
            return;
        }

        Page = await StartPageService.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != LinksWidgetDefinition.WidgetId)
        {
            return;
        }

        await Config.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StartPageService?.Changed -= OnPageChanged;
            ConfigService.ConfigChanged -= OnConfigChanged;
        }
    }
}

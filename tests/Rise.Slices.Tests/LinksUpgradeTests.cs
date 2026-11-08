using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Links;
using Xunit;

namespace Rise.Slices.Tests;

/// <summary>
/// Tests for the Links widget upgrade: tag filter bar, quick filter,
/// dual views, favicons, click tracking, and the extended editor
/// (notes, tags, pin, edit, sort).
/// </summary>
public class QuickLinksUpgradeTests : BunitContext
{
    private static readonly string[] WorkTag = { "work" };
    private static readonly string[] FunTag = { "fun" };

    private static readonly Link[] SeededLinks =
    {
        Link.Create(
            "Work Docs",
            new LinkUrl("https://docs.example.com/"),
            "Quarterly planning notes",
            WorkTag,
            false,
            "https://www.google.com/s2/favicons?domain=docs.example.com&sz=64"),
        Link.Create("Fun Video", new LinkUrl("https://video.example.com/"), "Cat videos", FunTag),
        Link.Create("Pinned Site", new LinkUrl("https://pinned.example.com/"), null, WorkTag, true),
    };

    private readonly FakeLinkStore _store;
    private readonly FakeWidgetConfigStore _configs = new();

    public QuickLinksUpgradeTests()
    {
        _store = new FakeLinkStore(SeededPage());
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<IWidgetConfigStore>(_ => _configs);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<LinksConfig>();
    }

    private static StartPage SeededPage() => new(SeededLinks);

    [Fact]
    public async Task TagPill_FiltersToTaggedLinks()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Fun Video", cut.Markup, StringComparison.Ordinal));

        await cut.FindAll(".calm-tag-pill").Single(b => b.TextContent.Trim() == "work").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.DoesNotContain("Fun Video", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pinned Site", cut.Markup, StringComparison.Ordinal);

        await cut.FindAll(".calm-tag-pill").Single(b => b.TextContent.Trim() == "All").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Contains("Fun Video", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_FiltersAcrossNotes()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Fun Video", cut.Markup, StringComparison.Ordinal));

        await cut.Find("[aria-label=\"Filter links\"]").InputAsync(new ChangeEventArgs { Value = "quarterly" });

        await cut.WaitForAssertionAsync(() => Assert.DoesNotContain("Fun Video", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_FiltersAcrossTitleUrlAndTags()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Fun Video", cut.Markup, StringComparison.Ordinal));

        await cut.Find("[aria-label=\"Filter links\"]").InputAsync(new ChangeEventArgs { Value = "fun" });

        await cut.WaitForAssertionAsync(() => Assert.DoesNotContain("Work Docs", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Fun Video", cut.Markup, StringComparison.Ordinal);

        await cut.Find("[aria-label=\"Filter links\"]").InputAsync(new ChangeEventArgs { Value = "video.example" });

        await cut.WaitForAssertionAsync(() => Assert.Contains("Fun Video", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("Pinned Site", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewToggle_SwitchesToDetailed_AndPersists()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal));

        await cut.Find("[aria-label=\"Switch to detailed view\"]").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Contains("calm-links detailed", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("docs.example.com", cut.Markup, StringComparison.Ordinal);
        Assert.True(_configs.Stored.TryGetValue("Links", out string? json) && json.Contains("Detailed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FaviconImg_RenderedWithS2Url_AndLetterTileFallbackPresent()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal));

        IElement icon = cut.Find("img.calm-link-icon");
        Assert.Contains("google.com/s2/favicons?domain=docs.example.com", icon.GetAttribute("src"), StringComparison.Ordinal);
        // The links without a stored icon fall back to the letter tile.
        Assert.Contains("calm-link-initials", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClickTile_RecordsClick()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal));

        IElement workDocs = cut.FindAll(".calm-link-target")
            .Single(a => a.TextContent.Contains("Work Docs", StringComparison.Ordinal));
        await workDocs.ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            1,
            _store.StoredPage!.Links.Single(l => l.Name == "Work Docs").ClickCount));
    }

    [Fact]
    public async Task PinnedLink_ShowsPinMarker()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Pinned Site", cut.Markup, StringComparison.Ordinal));

        Assert.Contains("Pinned link", cut.Markup, StringComparison.Ordinal);
    }
}

public class LinksSettingsUpgradeTests : BunitContext
{
    private static readonly string[] WorkTag = { "work" };
    private static readonly string[] ReadLaterTags = { "read", "later" };
    private static readonly string[] WorkUpdatedTags = { "work", "updated" };

    private static readonly Link[] SeededLinks =
    {
        Link.Create("Work Docs", new LinkUrl("https://docs.example.com/"), "Old note", WorkTag),
    };

    private readonly FakeLinkStore _store;
    private readonly FakeWidgetConfigStore _configs = new();

    public LinksSettingsUpgradeTests()
    {
        _store = new FakeLinkStore(new StartPage(SeededLinks));
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<IWidgetConfigStore>(_ => _configs);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<LinksConfig>();
    }

    [Fact]
    public async Task AddLink_WithNotesTagsAndPin_SavesAllPlusFavicon()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal));

        await cut.Find("[aria-label=\"Link name\"]").ChangeAsync(new ChangeEventArgs { Value = "Example" });
        await cut.Find("[aria-label=\"Link URL\"]").ChangeAsync(new ChangeEventArgs { Value = "https://example.com/" });
        await cut.Find("[aria-label=\"Link notes\"]").ChangeAsync(new ChangeEventArgs { Value = "A useful note" });
        await cut.Find("[aria-label=\"Link tags\"]").ChangeAsync(new ChangeEventArgs { Value = "read, later" });
        await cut.Find("[aria-label=\"Pin link\"]").ChangeAsync(new ChangeEventArgs { Value = true });
        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add link").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Contains("Example", cut.Markup, StringComparison.Ordinal));
        Link added = _store.StoredPage!.Links.Single(l => l.Name == "Example");
        Assert.Equal("A useful note", added.Description);
        Assert.Equal(ReadLaterTags, added.Tags);
        Assert.True(added.IsPinned);
        Assert.Contains("google.com/s2/favicons?domain=example.com", added.IconUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EditLink_SavesTagsAndNotes()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal));

        await cut.Find("[aria-label=\"Edit Work Docs\"]").ClickAsync();
        await cut.Find("[aria-label=\"Edit link notes\"]").ChangeAsync(new ChangeEventArgs { Value = "New note" });
        await cut.Find("[aria-label=\"Edit link tags\"]").ChangeAsync(new ChangeEventArgs { Value = "work, updated" });
        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Save").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "New note",
            _store.StoredPage!.Links.Single(l => l.Name == "Work Docs").Description));
        Link edited = _store.StoredPage!.Links.Single(l => l.Name == "Work Docs");
        Assert.Equal(WorkUpdatedTags, edited.Tags);
    }

    [Fact]
    public async Task EditLink_ChangingUrl_RecomputesIcon()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal));

        await cut.Find("[aria-label=\"Edit Work Docs\"]").ClickAsync();
        await cut.Find("[aria-label=\"Edit link URL\"]").ChangeAsync(new ChangeEventArgs { Value = "https://other.example.com/" });
        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Save").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Contains("other.example.com", cut.Markup, StringComparison.Ordinal));
        Link edited = _store.StoredPage!.Links.Single(l => l.Name == "Work Docs");
        Assert.Contains("google.com/s2/favicons?domain=other.example.com", edited.IconUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SortOption_PersistsToWidgetConfig()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("Work Docs", cut.Markup, StringComparison.Ordinal));

        await cut.Find("[aria-label=\"Sort links\"]").ChangeAsync(new ChangeEventArgs { Value = "Alphabetical" });

        await cut.WaitForAssertionAsync(() => Assert.True(
            _configs.Stored.TryGetValue("Links", out string? json) && json.Contains("Alphabetical", StringComparison.Ordinal)));
    }
}

using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Links;
using Xunit;

namespace Rise.Slices.Tests;

/// <summary>
/// Transfer UI in the Links settings section: export downloads
/// Netscape HTML, the bookmarklet link is present, and a
/// <c>?add=</c> query pre-fills the pending-add card. The file-input
/// pick itself cannot be simulated in bUnit — the browser harness
/// covers the real import pick.
/// </summary>
public class LinksTransferTests : BunitContext
{
    private readonly FakeLinkStore _store = new();
    private readonly FakeWidgetConfigStore _configs = new();
    private readonly FakeFileDownloader _downloader = new();

    public LinksTransferTests()
    {
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<IWidgetConfigStore>(_ => _configs);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<LinksConfig>();
        Services.AddScoped<IFileDownloader>(_ => _downloader);
    }

    private void NavigateWithAddQuery()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("http://localhost/?add=https%3A%2F%2Fexample.com%2Fpage&title=Example%20Page&notes=Selected%20text");
    }

    [Fact]
    public void TransferUi_RendersImportExportAndBookmarklet()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        Assert.NotNull(cut.Find("[aria-label=\"Import bookmarks file\"]"));
        Assert.Contains("Export bookmarks", cut.Markup, StringComparison.Ordinal);
        IElement bookmarklet = cut.Find(".calm-settings-bookmarklet a");
        Assert.Equal("Add to Rise", bookmarklet.TextContent.Trim());
        string? href = bookmarklet.GetAttribute("href");
        Assert.StartsWith("javascript:", href, StringComparison.Ordinal);
        Assert.Contains("josuave.github.io/Rise/?add=", href, StringComparison.Ordinal);
        Assert.Contains("encodeURIComponent", href, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Export_DownloadsNetscapeBookmarksFile()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Export bookmarks").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Single(_downloader.Downloads));
        Assert.Equal("rise-bookmarks.html", _downloader.Downloads[0].Filename);
        Assert.Contains("<!DOCTYPE NETSCAPE-Bookmark-file-1>", _downloader.Downloads[0].Content, StringComparison.Ordinal);
        Assert.Contains("https://www.google.com/", _downloader.Downloads[0].Content, StringComparison.Ordinal);
        Assert.Contains("Bookmarks exported.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PendingAdd_FromQuery_IsPreFilled()
    {
        NavigateWithAddQuery();

        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();

        await cut.WaitForAssertionAsync(() => Assert.Contains("From your bookmarklet", cut.Markup, StringComparison.Ordinal));
        Assert.Equal("Example Page", cut.Find("[aria-label=\"Pending link name\"]").GetAttribute("value"));
        Assert.Equal("https://example.com/page", cut.Find("[aria-label=\"Pending link URL\"]").GetAttribute("value"));
        Assert.Contains("Selected text", cut.Find("[aria-label=\"Pending link notes\"]").OuterHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PendingAdd_Save_AddsLinkAndClearsQuery()
    {
        NavigateWithAddQuery();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();

        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("From your bookmarklet", cut.Markup, StringComparison.Ordinal));

        await cut.FindAll(".calm-settings-pending button").First(b => b.TextContent.Trim() == "Add link").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Contains("Link added.", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(_store.StoredPage);
        Assert.Contains(_store.StoredPage.Links, link => link.Name == "Example Page" && link.Url.Value == "https://example.com/page");
        Assert.Equal("Selected text", _store.StoredPage.Links.Single(link => link.Name == "Example Page").Description);
        Assert.Equal(navigation.BaseUri, navigation.Uri);
        Assert.DoesNotContain("From your bookmarklet", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PendingAdd_Dismiss_DiscardsAndClearsQuery()
    {
        NavigateWithAddQuery();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();

        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("From your bookmarklet", cut.Markup, StringComparison.Ordinal));

        await cut.FindAll(".calm-settings-pending button").Single(b => b.TextContent.Trim() == "Dismiss").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.DoesNotContain("From your bookmarklet", cut.Markup, StringComparison.Ordinal));
        Assert.Equal(navigation.BaseUri, navigation.Uri);
        Assert.DoesNotContain("Example Page", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingAdd_Absent_WithoutQuery()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        Assert.DoesNotContain("From your bookmarklet", cut.Markup, StringComparison.Ordinal);
    }
}

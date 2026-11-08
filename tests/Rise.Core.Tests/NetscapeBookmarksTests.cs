using Rise.Application;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

/// <summary>Netscape bookmark import/export: folders, tags, notes, dates, malformed input, and the round-trip.</summary>
public class NetscapeBookmarksTests
{
    private sealed class FakeLinkStore : ILinkStore
    {
        public StartPage? Stored { get; private set; }

        public Task<StartPage?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(StartPage page, CancellationToken cancellationToken = default)
        {
            Stored = page;
            return Task.CompletedTask;
        }
    }

    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    private const string Sample =
        "<!DOCTYPE NETSCAPE-Bookmark-file-1>\n"
        + "<DL><p>\n"
        + "    <DT><H3 ADD_DATE=\"1700000000\">Work</H3>\n"
        + "    <DL><p>\n"
        + "        <DT><A HREF=\"https://docs.example.com/\" ADD_DATE=\"1700000000\" TAGS=\"docs,reference\">Work Docs</A>\n"
        + "        <DD>Quarterly planning notes</DD>\n"
        + "        <DT><H3>Deep</H3>\n"
        + "        <DL><p>\n"
        + "            <DT><A HREF=\"https://deep.example.com/\" ADD_DATE=\"1700000001\">Deep Link</A>\n"
        + "        </DL><p>\n"
        + "    </DL><p>\n"
        + "    <DT><A HREF=\"https://plain.example.com/\">Plain Link</A>\n"
        + "</DL><p>\n";

    [Fact]
    public void Parse_SampleWithFoldersTagsDdAndAddDate()
    {
        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(Sample, Now);

        Assert.Equal(3, parsed.Count);
        ParsedBookmark docs = parsed[0];
        Assert.Equal("Work Docs", docs.Name);
        Assert.Equal("https://docs.example.com/", docs.Url);
        Assert.Equal("Quarterly planning notes", docs.Description);
        Assert.Equal(new List<string> { "Work", "docs", "reference" }, docs.Tags);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), docs.CreatedAt);
        ParsedBookmark deep = parsed[1];
        Assert.Equal("Deep Link", deep.Name);
        Assert.Equal(new List<string> { "Work", "Deep" }, deep.Tags);
        Assert.Null(deep.Description);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000001), deep.CreatedAt);
        ParsedBookmark plain = parsed[2];
        Assert.Equal("Plain Link", plain.Name);
        Assert.Empty(plain.Tags);
        Assert.Equal(Now, plain.CreatedAt);
    }

    [Fact]
    public void Parse_UnclosedDd_NoteStillAttaches()
    {
        // Browsers export "<DD>note" with no closing tag; the note
        // still belongs to the link directly before it, whether the
        // next thing is another entry or the list close.
        string html = "<DL><p>\n"
            + "    <DT><A HREF=\"https://one.example.com/\">One</A>\n"
            + "    <DD>First note\n"
            + "    <DT><A HREF=\"https://two.example.com/\">Two</A>\n"
            + "    <DD>Second note\n"
            + "</DL><p>\n";

        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(html, Now);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("First note", parsed[0].Description);
        Assert.Equal("Second note", parsed[1].Description);
    }

    [Fact]
    public void Parse_MalformedLinesSkipped()
    {
        string html = "<DL><p>\n"
            + "    <DT><A ADD_DATE=\"1700000000\">No address at all</A>\n"
            + "    <DT><A HREF=\"not a url at all\">Bad address</A>\n"
            + "    <DT><A HREF=\"https://good.example.com/\" ADD_DATE=\"not-a-number\">Good Link</A>\n"
            + "    <DT><A HREF=\"ftp://files.example.com/\">Wrong scheme</A>\n"
            + "</DL><p>\n";

        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(html, Now);

        ParsedBookmark single = Assert.Single(parsed);
        Assert.Equal("Good Link", single.Name);
        Assert.Equal("https://good.example.com/", single.Url);
        Assert.Equal(Now, single.CreatedAt);
    }

    [Fact]
    public void Parse_MissingAddDate_UsesNow()
    {
        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(
            "<DT><A HREF=\"https://example.com/\">Example</A>", Now);

        Assert.Equal(Now, Assert.Single(parsed).CreatedAt);
    }

    [Fact]
    public void Parse_HtmlEntitiesDecoded()
    {
        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(
            "<DT><A HREF=\"https://example.com/?a=1&amp;b=2\" TAGS=\"rock &amp; roll\">Tom &amp; Jerry</A>\n<DD>Fish &amp; chips</DD>",
            Now);

        ParsedBookmark single = Assert.Single(parsed);
        Assert.Equal("Tom & Jerry", single.Name);
        Assert.Equal("https://example.com/?a=1&b=2", single.Url);
        Assert.Equal(new List<string> { "rock & roll" }, single.Tags);
        Assert.Equal("Fish & chips", single.Description);
    }

    [Fact]
    public void Parse_EmptyTitle_FallsBackToUrl()
    {
        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(
            "<DT><A HREF=\"https://example.com/\">   </A>", Now);

        Assert.Equal("https://example.com/", Assert.Single(parsed).Name);
    }

    [Fact]
    public void Export_RoundTrip_PreservesLinks()
    {
        List<Link> links = new()
        {
            new(
                Guid.NewGuid(),
                "Work Docs",
                new LinkUrl("https://docs.example.com/"),
                "Quarterly planning notes",
                new List<string> { "Work", "docs" },
                null,
                false,
                0,
                DateTimeOffset.FromUnixTimeSeconds(1700000000)),
            new(
                Guid.NewGuid(),
                "Plain",
                new LinkUrl("https://plain.example.com/"),
                null,
                null,
                null,
                false,
                0,
                DateTimeOffset.FromUnixTimeSeconds(1700000001)),
        };

        string exported = NetscapeBookmarks.Export(links);
        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(exported, Now);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("Work Docs", parsed[0].Name);
        Assert.Equal("https://docs.example.com/", parsed[0].Url);
        Assert.Equal("Quarterly planning notes", parsed[0].Description);
        Assert.Equal(new List<string> { "Work", "docs" }, parsed[0].Tags);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), parsed[0].CreatedAt);
        Assert.Equal("Plain", parsed[1].Name);
        Assert.Null(parsed[1].Description);
        Assert.Empty(parsed[1].Tags);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000001), parsed[1].CreatedAt);
    }

    [Fact]
    public void Export_EscapesHtml()
    {
        List<Link> links = new()
        {
            Link.Create(
                "Tom & <Jerry>",
                new LinkUrl("https://example.com/?a=1&b=2"),
                "Note with <b>markup</b> & \"quotes\"",
                new List<string> { "a<b" }),
        };

        string exported = NetscapeBookmarks.Export(links);

        Assert.DoesNotContain("<Jerry>", exported, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>markup</b>", exported, StringComparison.Ordinal);
        Assert.Contains("Tom &amp; &lt;Jerry&gt;", exported, StringComparison.Ordinal);
        IReadOnlyList<ParsedBookmark> parsed = NetscapeBookmarks.Parse(exported, Now);
        ParsedBookmark single = Assert.Single(parsed);
        Assert.Equal("Tom & <Jerry>", single.Name);
        Assert.Equal("Note with <b>markup</b> & \"quotes\"", single.Description);
        Assert.Equal(new List<string> { "a<b" }, single.Tags);
    }

    [Fact]
    public void Export_EmptyPage_HasHeaderOnly()
    {
        string exported = NetscapeBookmarks.Export(Array.Empty<Link>());

        Assert.Contains("<!DOCTYPE NETSCAPE-Bookmark-file-1>", exported, StringComparison.Ordinal);
        Assert.DoesNotContain("<A HREF", exported, StringComparison.Ordinal);
        Assert.Empty(NetscapeBookmarks.Parse(exported, Now));
    }

    [Fact]
    public async Task ImportLinks_PreservesCreatedDates_AndSkipsDuplicates()
    {
        FakeLinkStore store = new();
        StartPageService service = new(store);
        var page = StartPage.Default();
        var incoming = new List<Link>
        {
            new(
                Guid.NewGuid(),
                "New One",
                new LinkUrl("https://new.example.com/"),
                "kept note",
                new List<string> { "imported" },
                null,
                false,
                0,
                DateTimeOffset.FromUnixTimeSeconds(1700000000)),
            new(Guid.NewGuid(), "Dupe", new LinkUrl("https://WWW.GOOGLE.COM/"), null, null, null, false, 0, Now),
            new(Guid.NewGuid(), "Dupe Again", new LinkUrl("https://new.example.com/"), null, null, null, false, 0, Now),
        };

        (StartPage next, int added, int skipped) = await service.ImportLinksAsync(page, incoming);

        Assert.Equal(1, added);
        Assert.Equal(2, skipped);
        Link imported = Assert.Single(next.Links, l => l.Name == "New One");
        Assert.Equal("kept note", imported.Description);
        Assert.Equal(new List<string> { "imported" }, imported.Tags);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000), imported.CreatedAt);
        Assert.Equal(next, store.Stored);
    }

    [Fact]
    public async Task ImportLinks_NothingNew_DoesNotSave()
    {
        FakeLinkStore store = new();
        StartPageService service = new(store);
        var page = StartPage.Default();

        (StartPage next, int added, int skipped) = await service.ImportLinksAsync(
            page, new List<Link> { new(Guid.NewGuid(), "Dupe", new LinkUrl("https://www.google.com/")) });

        Assert.Equal(0, added);
        Assert.Equal(1, skipped);
        Assert.Same(page, next);
        Assert.Null(store.Stored);
    }
}

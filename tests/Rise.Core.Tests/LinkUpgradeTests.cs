using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

/// <summary>Links upgrade tests: the extended Link shape, UpdateLink, RecordClick, sorting, and the service shell.</summary>
public class LinkUpgradeTests
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

    private static readonly DateTimeOffset First = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Second = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Third = new(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fourth = new(2026, 1, 4, 0, 0, 0, TimeSpan.Zero);

    private static Link Made(string name, long clicks = 0, bool pinned = false, DateTimeOffset? createdAt = null) =>
        new(Guid.NewGuid(), name, new LinkUrl("https://example.com/" + name.ToUpperInvariant() + "/"), null, null, null, pinned, clicks, createdAt ?? First);

    [Fact]
    public void Link_Create_SetsDefaults()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-5);

        var link = Link.Create("Example", new LinkUrl("https://example.com/"));

        Assert.Null(link.Description);
        Assert.Empty(link.Tags);
        Assert.Null(link.IconUrl);
        Assert.False(link.IsPinned);
        Assert.Equal(0, link.ClickCount);
        Assert.True(link.CreatedAt >= before && link.CreatedAt <= DateTimeOffset.UtcNow.AddSeconds(5));
        Assert.Null(link.LastAccessedAt);
    }

    [Fact]
    public void Link_Create_NormalizesTags_KeepingFirstSpelling()
    {
        var link = Link.Create("Example", new LinkUrl("https://example.com/"), null, new List<string> { "  Work ", "", "work", "WORK", "   ", "Personal" });

        Assert.Equal(new List<string> { "Work", "Personal" }, link.Tags);
    }

    [Fact]
    public void Link_Create_TrimsDescriptionAndIcon_BlankReadsAsNull()
    {
        var link = Link.Create("Example", new LinkUrl("https://example.com/"), "  a note  ", null, false, " https://example.com/icon.png ");

        Assert.Equal("a note", link.Description);
        Assert.Equal("https://example.com/icon.png", link.IconUrl);

        var blank = Link.Create("Example", new LinkUrl("https://example.com/"), "   ", null, false, "  ");

        Assert.Null(blank.Description);
        Assert.Null(blank.IconUrl);
    }

    [Fact]
    public void Link_Create_NegativeClickCount_ReadsAsZero()
    {
        var link = new Link(Guid.NewGuid(), "Example", new LinkUrl("https://example.com/"), null, null, null, false, -3);

        Assert.Equal(0, link.ClickCount);
    }

    [Fact]
    public void Link_Create_KeepsExplicitCreatedAt()
    {
        var link = new Link(Guid.NewGuid(), "Example", new LinkUrl("https://example.com/"), null, null, null, false, 0, Second);

        Assert.Equal(Second, link.CreatedAt);
    }

    [Fact]
    public void StartPage_AddLink_WithDetails_SetsDescriptionTagsAndPin()
    {
        Result<StartPage> result = StartPage.Default().AddLink(
            "Example", "example.com", " my note ", new List<string> { " Work ", "" }, true, "https://example.com/icon.png");

        StartPage updated = ResultAssertions.RequireOk(result);
        Link added = updated.Links[^1];
        Assert.Equal("Example", added.Name);
        Assert.Equal("my note", added.Description);
        Assert.Equal(new List<string> { "Work" }, added.Tags);
        Assert.True(added.IsPinned);
        Assert.Equal("https://example.com/icon.png", added.IconUrl);
        Assert.Equal(0, added.ClickCount);
    }

    [Fact]
    public void StartPage_AddLink_WithDetails_RejectsDuplicateUrl_IgnoringCase()
    {
        string existing = StartPage.Default().Links[0].Url.Value.ToUpperInvariant();

        Result<StartPage> result = StartPage.Default().AddLink("Again", existing, "note", new List<string> { "x" }, true);

        Assert.NotEmpty(ResultAssertions.RequireError(result));
    }

    [Theory]
    [InlineData("", "example.com")]
    [InlineData("Example", "ftp://example.com")]
    public void StartPage_AddLink_WithDetails_RejectsBadInput(string name, string input) =>
        Assert.NotEmpty(ResultAssertions.RequireError(StartPage.Default().AddLink(name, input, null, null, false)));

    [Fact]
    public void StartPage_UpdateLink_EditsInPlace_PreservingIdentityAndStats()
    {
        StartPage page = new(new[] { Made("Example", clicks: 3, createdAt: Second) });
        Guid id = page.Links[0].Id;
        StartPage clicked = page.RecordClick(id);
        DateTimeOffset? lastAccessed = clicked.Links[0].LastAccessedAt;
        Assert.NotNull(lastAccessed);

        Result<StartPage> result = clicked.UpdateLink(id, " Renamed ", "example.org", "new note", new List<string> { " New " }, true, "https://example.com/icon.png");

        StartPage updated = ResultAssertions.RequireOk(result);
        Assert.Single(updated.Links);
        Link link = updated.Links[0];
        Assert.Equal(id, link.Id);
        Assert.Equal("Renamed", link.Name);
        Assert.Equal("https://example.org/", link.Url.Value);
        Assert.Equal("new note", link.Description);
        Assert.Equal(new List<string> { "New" }, link.Tags);
        Assert.True(link.IsPinned);
        Assert.Equal("https://example.com/icon.png", link.IconUrl);
        Assert.Equal(4, link.ClickCount);
        Assert.Equal(Second, link.CreatedAt);
        Assert.Equal(lastAccessed, link.LastAccessedAt);
        // The original page is untouched (pure core).
        Assert.Equal("Example", clicked.Links[0].Name);
    }

    [Fact]
    public void StartPage_UpdateLink_NullIcon_KeepsIcon_BlankIcon_ClearsIt()
    {
        StartPage page = new(new[] { Made("Example") });
        Guid id = page.Links[0].Id;
        StartPage withIcon = ResultAssertions.RequireOk(page.UpdateLink(id, "Example", "example.com", null, null, false, "https://example.com/icon.png"));

        StartPage kept = ResultAssertions.RequireOk(withIcon.UpdateLink(id, "Example", "example.com", null, null, false));
        Assert.Equal("https://example.com/icon.png", kept.Links[0].IconUrl);

        StartPage cleared = ResultAssertions.RequireOk(kept.UpdateLink(id, "Example", "example.com", null, null, false, "   "));
        Assert.Null(cleared.Links[0].IconUrl);
    }

    [Fact]
    public void StartPage_UpdateLink_RejectsAnotherLinksUrl_AllowsKeepingItsOwn()
    {
        StartPage page = new(new[] { Made("First"), Made("Second") });
        Guid firstId = page.Links[0].Id;
        string secondUrl = page.Links[1].Url.Value;

        Assert.NotEmpty(ResultAssertions.RequireError(page.UpdateLink(firstId, "First", secondUrl, null, null, false)));

        StartPage kept = ResultAssertions.RequireOk(page.UpdateLink(firstId, "First", page.Links[0].Url.Value, null, null, false));
        Assert.Equal(page.Links[0].Url.Value, kept.Links[0].Url.Value);
    }

    [Fact]
    public void StartPage_UpdateLink_UnknownId_Fails()
    {
        Result<StartPage> result = StartPage.Default().UpdateLink(Guid.NewGuid(), "Example", "example.com", null, null, false);

        Assert.NotEmpty(ResultAssertions.RequireError(result));
    }

    [Theory]
    [InlineData("", "example.com")]
    [InlineData("Example", "ftp://example.com")]
    public void StartPage_UpdateLink_RejectsBadInput(string name, string input)
    {
        var page = StartPage.Default();
        Guid id = page.Links[0].Id;

        Assert.NotEmpty(ResultAssertions.RequireError(page.UpdateLink(id, name, input, null, null, false)));
    }

    [Fact]
    public void StartPage_RecordClick_IncrementsCount_AndStampsTime()
    {
        StartPage page = new(new[] { Made("First"), Made("Second") });
        Guid id = page.Links[0].Id;
        DateTimeOffset before = DateTimeOffset.UtcNow;

        StartPage clicked = page.RecordClick(id);
        StartPage again = clicked.RecordClick(id);

        Assert.Equal(1, clicked.Links[0].ClickCount);
        Assert.NotNull(clicked.Links[0].LastAccessedAt);
        Assert.True(clicked.Links[0].LastAccessedAt >= before);
        Assert.Equal(2, again.Links[0].ClickCount);
        Assert.True(again.Links[0].LastAccessedAt >= clicked.Links[0].LastAccessedAt);
        Assert.Equal(0, again.Links[1].ClickCount);
        Assert.Null(again.Links[1].LastAccessedAt);
        // The original page is untouched (pure core).
        Assert.Equal(0, page.Links[0].ClickCount);
        Assert.Null(page.Links[0].LastAccessedAt);
    }

    [Fact]
    public void StartPage_RecordClick_UnknownId_LeavesPageUnchanged()
    {
        var page = StartPage.Default();

        StartPage next = page.RecordClick(Guid.NewGuid());

        Assert.Equal(page.Links.Count, next.Links.Count);
        Assert.All(next.Links, link => Assert.Equal(0, link.ClickCount));
    }

    [Fact]
    public void StartPage_Sorted_Custom_KeepsStoredOrder_PinnedFirst()
    {
        StartPage page = new(new[]
        {
            Made("banana", createdAt: Third),
            Made("apple", pinned: true, createdAt: First),
            Made("cherry", createdAt: Second),
            Made("date", pinned: true, createdAt: Fourth),
        });

        Assert.Equal(new List<string> { "apple", "date", "banana", "cherry" }, page.Sorted(LinkSort.Custom).Select(l => l.Name));
        // Sorting never rewrites the stored order.
        Assert.Equal(new List<string> { "banana", "apple", "cherry", "date" }, page.Links.Select(l => l.Name));
    }

    [Fact]
    public void StartPage_Sorted_Frequency_ClicksDesc_PinnedFirst_TiesKeepStoredOrder()
    {
        StartPage page = new(new[]
        {
            Made("banana", clicks: 5, createdAt: Third),
            Made("apple", clicks: 10, pinned: true, createdAt: First),
            Made("cherry", clicks: 10, createdAt: Second),
            Made("date", clicks: 0, pinned: true, createdAt: Fourth),
            Made("elderberry", clicks: 10, createdAt: Fourth),
        });

        Assert.Equal(new List<string> { "apple", "date", "cherry", "elderberry", "banana" }, page.Sorted(LinkSort.Frequency).Select(l => l.Name));
    }

    [Fact]
    public void StartPage_Sorted_Alphabetical_IgnoresCase_PinnedFirst()
    {
        StartPage page = new(new[]
        {
            Made("banana", createdAt: Third),
            Made("apple", pinned: true, createdAt: First),
            Made("Cherry", createdAt: Second),
            Made("date", pinned: true, createdAt: Fourth),
        });

        Assert.Equal(new List<string> { "apple", "date", "banana", "Cherry" }, page.Sorted(LinkSort.Alphabetical).Select(l => l.Name));
    }

    [Fact]
    public void StartPage_Sorted_DateAdded_NewestFirst_PinnedFirst_TiesKeepStoredOrder()
    {
        StartPage page = new(new[]
        {
            Made("banana", createdAt: Third),
            Made("apple", pinned: true, createdAt: First),
            Made("cherry", createdAt: Second),
            Made("date", pinned: true, createdAt: Fourth),
            Made("elderberry", createdAt: Third),
        });

        Assert.Equal(new List<string> { "date", "apple", "banana", "elderberry", "cherry" }, page.Sorted(LinkSort.DateAdded).Select(l => l.Name));
    }

    [Fact]
    public async Task StartPageService_UpdateLink_Persists_AndRaisesChanged()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        int changed = 0;
        service.Changed += (_, _) => changed++;
        StartPage page = new(new[] { Made("Example") });
        Guid id = page.Links[0].Id;

        Result<StartPage> result = await service.UpdateLinkAsync(page, id, "Renamed", "example.com", "note", new List<string> { "x" }, true);

        StartPage updated = ResultAssertions.RequireOk(result);
        Assert.Equal("Renamed", updated.Links[0].Name);
        Assert.Equal("Renamed", store.Stored!.Links[0].Name);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task StartPageService_UpdateLink_DoesNotPersistOnFailure()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        int changed = 0;
        service.Changed += (_, _) => changed++;
        var page = StartPage.Default();

        Result<StartPage> result = await service.UpdateLinkAsync(page, Guid.NewGuid(), "Example", "example.com", null, null, false);

        Assert.NotEmpty(ResultAssertions.RequireError(result));
        Assert.Null(store.Stored);
        Assert.Equal(0, changed);
    }

    [Fact]
    public async Task StartPageService_RecordClick_Persists_AndRaisesChanged()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        int changed = 0;
        service.Changed += (_, _) => changed++;
        StartPage page = new(new[] { Made("Example") });
        Guid id = page.Links[0].Id;

        StartPage next = await service.RecordClickAsync(page, id);

        Assert.Equal(1, next.Links[0].ClickCount);
        Assert.NotNull(next.Links[0].LastAccessedAt);
        Assert.Equal(1, store.Stored!.Links[0].ClickCount);
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task StartPageService_AddLink_WithDetails_PersistsThroughCore()
    {
        var store = new FakeLinkStore();
        var service = new StartPageService(store);
        StartPage page = await service.LoadAsync();

        Result<StartPage> result = await service.AddLinkAsync(page, "Example", "example.com", "note", new List<string> { "Work" }, true);

        StartPage updated = ResultAssertions.RequireOk(result);
        Assert.Equal("note", updated.Links[^1].Description);
        Assert.Equal(new List<string> { "Work" }, store.Stored!.Links[^1].Tags);
        Assert.True(store.Stored.Links[^1].IsPinned);
    }
}

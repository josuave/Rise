using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class DomainTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com/")]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("  https://example.com/path  ", "https://example.com/path")]
    public void LinkUrl_NormalizesValidInput(string input, string expected)
    {
        Assert.True(LinkUrl.TryCreate(input, out var url, out var error));
        Assert.Null(error);
        Assert.Equal(expected, url!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://example.com")]
    [InlineData("not a url with spaces://x")]
    public void LinkUrl_RejectsInvalidInput(string input)
    {
        Assert.False(LinkUrl.TryCreate(input, out var url, out var error));
        Assert.Null(url);
        Assert.NotNull(error);
    }

    [Fact]
    public void StartPage_AddLink_AppendsAndReturnsNewPage()
    {
        var page = StartPage.Default();
        var result = page.AddLink("Example", "example.com");

        Assert.True(result.IsSuccess);
        Assert.Equal(page.Links.Count + 1, result.Value!.Links.Count);
        Assert.Equal("Example", result.Value.Links[^1].Name);
        // Pure core: the original page is untouched.
        Assert.Equal(page.Links.Count, StartPage.Default().Links.Count);
    }

    [Fact]
    public void StartPage_AddLink_RejectsDuplicates()
    {
        var page = StartPage.Default();
        var existing = page.Links[0].Url.Value;

        var result = page.AddLink("Again", existing);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData("", "example.com")]
    [InlineData("   ", "example.com")]
    [InlineData("Example", "")]
    [InlineData("Example", "ftp://example.com")]
    public void StartPage_AddLink_RejectsBadInput(string name, string url)
    {
        Assert.False(StartPage.Default().AddLink(name, url).IsSuccess);
    }

    [Fact]
    public void StartPage_RemoveLink_RemovesOnlyThatLink()
    {
        var page = StartPage.Default();
        var id = page.Links[0].Id;

        var next = page.RemoveLink(id);

        Assert.Equal(page.Links.Count - 1, next.Links.Count);
        Assert.DoesNotContain(next.Links, l => l.Id == id);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void SearchQuery_BlankInput_ReturnsNull(string? query, string? _)
    {
        Assert.Null(SearchQuery.Build(query));
    }

    [Fact]
    public void SearchQuery_BuildsEncodedGoogleUrl()
    {
        var url = SearchQuery.Build("  hello world & friends ");

        Assert.Equal("https://www.google.com/search?q=hello%20world%20%26%20friends", url!.ToString());
    }

    [Theory]
    [InlineData(0, "Good morning.")]
    [InlineData(11, "Good morning.")]
    [InlineData(12, "Good afternoon.")]
    [InlineData(17, "Good afternoon.")]
    [InlineData(18, "Good evening.")]
    [InlineData(23, "Good evening.")]
    public void Greeting_FollowsTimeOfDay(int hour, string expected)
    {
        Assert.Equal(expected, Greeting.For(new DateTime(2026, 10, 31, hour, 0, 0)));
    }

    [Theory]
    [InlineData("GitHub", "GI")]
    [InlineData("Google", "GO")]
    [InlineData("My Cool Site", "MC")]
    public void Link_Initials(string name, string expected)
    {
        var link = Link.Create(name, new LinkUrl("https://example.com/"));
        Assert.Equal(expected, link.Initials);
    }
}

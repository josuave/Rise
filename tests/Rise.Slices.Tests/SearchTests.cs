using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using SearchComponent = Rise.Slices.Search.Search;
using Xunit;

namespace Rise.Slices.Tests;

public class SearchTests : BunitContext
{
    private readonly FakeSearchLauncher _launcher = new();

    public SearchTests()
    {
        Services.AddScoped<ISearchLauncher>(_ => _launcher);
        Services.AddScoped<SearchService>();
    }

    [Fact]
    public void SearchButton_LaunchesEncodedGoogleUrl()
    {
        var cut = Render<SearchComponent>();

        cut.Find(".calm-search-input").Input("hello world");
        cut.Find(".calm-search-button").Click();

        Assert.Single(_launcher.LaunchedUrls);
        Assert.Equal(
            "https://www.google.com/search?q=hello%20world",
            _launcher.LaunchedUrls[0].AbsoluteUri);
    }

    [Fact]
    public void EnterKey_LaunchesSearch()
    {
        var cut = Render<SearchComponent>();

        var input = cut.Find(".calm-search-input");
        input.Input("from enter");
        input.KeyDown(new KeyboardEventArgs { Code = "Enter" });

        Assert.Single(_launcher.LaunchedUrls);
        Assert.Contains("from%20enter", _launcher.LaunchedUrls[0].AbsoluteUri);
    }

    [Fact]
    public void BlankQuery_LaunchesNothing()
    {
        var cut = Render<SearchComponent>();

        cut.Find(".calm-search-button").Click();

        Assert.Empty(_launcher.LaunchedUrls);
    }
}

using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Search;
using SearchComponent = Rise.Slices.Search.Search;
using Xunit;

namespace Rise.Slices.Tests;

public class SearchTests : BunitContext
{
    private readonly FakeSearchLauncher _launcher = new();
    private readonly FakeWidgetConfigStore _configs = new();

    public SearchTests()
    {
        Services.AddScoped<ISearchLauncher>(_ => _launcher);
        Services.AddScoped<IWidgetConfigStore>(_ => _configs);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<SearchConfig>();
        Services.AddScoped<ISearchEngineProvider, SearchEngineProvider>();
        Services.AddScoped<SearchService>();
    }

    [Fact]
    public void SearchButton_LaunchesEncodedGoogleUrl()
    {
        IRenderedComponent<SearchComponent> cut = Render<SearchComponent>();

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
        IRenderedComponent<SearchComponent> cut = Render<SearchComponent>();

        IElement input = cut.Find(".calm-search-input");
        input.Input("from enter");
        input.KeyDown(new KeyboardEventArgs { Code = "Enter" });

        Assert.Single(_launcher.LaunchedUrls);
        Assert.Contains("from%20enter", _launcher.LaunchedUrls[0].AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public void BlankQuery_LaunchesNothing()
    {
        IRenderedComponent<SearchComponent> cut = Render<SearchComponent>();

        cut.Find(".calm-search-button").Click();

        Assert.Empty(_launcher.LaunchedUrls);
    }

    [Fact]
    public async Task SearchButton_UsesConfiguredEngine()
    {
        _configs.Stored["Search"] = "{\"EngineId\":\"brave\"}";

        IRenderedComponent<SearchComponent> cut = Render<SearchComponent>();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "Search Brave…",
            cut.Find(".calm-search-input").GetAttribute("placeholder")));
        await cut.Find(".calm-search-input").InputAsync(new ChangeEventArgs { Value = "hello world" });
        await cut.Find(".calm-search-button").ClickAsync();

        Assert.Single(_launcher.LaunchedUrls);
        Assert.Equal(
            "https://search.brave.com/search?q=hello%20world",
            _launcher.LaunchedUrls[0].AbsoluteUri);
    }

    [Fact]
    public async Task Placeholder_UpdatesWhenEngineChanges()
    {
        IRenderedComponent<SearchComponent> cut = Render<SearchComponent>();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "Search Google…",
            cut.Find(".calm-search-input").GetAttribute("placeholder")));

        WidgetConfigService service = Services.GetRequiredService<WidgetConfigService>();
        await service.SetJsonAsync("Search", "{\"EngineId\":\"duckduckgo\"}");

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "Search DuckDuckGo…",
            cut.Find(".calm-search-input").GetAttribute("placeholder")));
    }

    [Fact]
    public async Task SearchButton_UsesCustomEngine()
    {
        _configs.Stored["Search"] = "{\"EngineId\":\"custom:mojeek\",\"CustomEngines\":[{\"Id\":\"custom:mojeek\",\"Name\":\"Mojeek\",\"UrlTemplate\":\"https://www.mojeek.com/search?q=%s\"}]}";

        IRenderedComponent<SearchComponent> cut = Render<SearchComponent>();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "Search Mojeek…",
            cut.Find(".calm-search-input").GetAttribute("placeholder")));
        await cut.Find(".calm-search-input").InputAsync(new ChangeEventArgs { Value = "hello world" });
        await cut.Find(".calm-search-button").ClickAsync();

        Assert.Single(_launcher.LaunchedUrls);
        Assert.Equal(
            "https://www.mojeek.com/search?q=hello%20world",
            _launcher.LaunchedUrls[0].AbsoluteUri);
    }

    [Fact]
    public async Task UnknownStoredEngine_RepairsToGoogle()
    {
        _configs.Stored["Search"] = "{\"EngineId\":\"ghost\"}";

        IRenderedComponent<SearchComponent> cut = Render<SearchComponent>();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "Search Google…",
            cut.Find(".calm-search-input").GetAttribute("placeholder")));
        await cut.Find(".calm-search-input").InputAsync(new ChangeEventArgs { Value = "hello world" });
        await cut.Find(".calm-search-button").ClickAsync();

        Assert.Single(_launcher.LaunchedUrls);
        Assert.Equal(
            "https://www.google.com/search?q=hello%20world",
            _launcher.LaunchedUrls[0].AbsoluteUri);
    }
}

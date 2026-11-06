using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Search;
using Xunit;

namespace Rise.Slices.Tests;

public class SearchSettingsTests : BunitContext
{
    private readonly FakeWidgetConfigStore _configs = new();

    public SearchSettingsTests()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => _configs);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<SearchConfig>();
    }

    [Fact]
    public async Task PickingBrave_PersistsEngineAndMarksItSelected()
    {
        IRenderedComponent<SearchSettings> cut = Render<SearchSettings>();

        await cut.Find("[aria-label=\"Use Brave search\"]").ClickAsync();

        await cut.WaitForAssertionAsync(() =>
        {
            Assert.Contains("\"EngineId\":\"brave\"", _configs.Stored["Search"], StringComparison.Ordinal);
            Assert.Equal("true", cut.Find("[aria-label=\"Use Brave search\"]").GetAttribute("aria-pressed"));
            Assert.Equal("false", cut.Find("[aria-label=\"Use Google search\"]").GetAttribute("aria-pressed"));
        });
    }

    [Fact]
    public void PickerListsAllFourEngines()
    {
        IRenderedComponent<SearchSettings> cut = Render<SearchSettings>();

        Assert.Equal(4, cut.FindAll(".calm-search-engine-option").Count);
    }

    [Fact]
    public async Task UnknownStoredEngine_ShowsGoogleSelected()
    {
        _configs.Stored["Search"] = "{\"EngineId\":\"ghost\"}";

        IRenderedComponent<SearchSettings> cut = Render<SearchSettings>();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "true",
            cut.Find("[aria-label=\"Use Google search\"]").GetAttribute("aria-pressed")));
    }

    [Fact]
    public void PickerShowsEngineFavicons()
    {
        IRenderedComponent<SearchSettings> cut = Render<SearchSettings>();

        Assert.Equal(
            "https://www.google.com/favicon.ico",
            cut.Find("[aria-label=\"Use Google search\"] img").GetAttribute("src"));
        Assert.Equal(
            "https://search.brave.com/favicon.ico",
            cut.Find("[aria-label=\"Use Brave search\"] img").GetAttribute("src"));
    }

    [Fact]
    public async Task AddingCustomEngine_PersistsItSelectsItAndListsIt()
    {
        IRenderedComponent<SearchSettings> cut = Render<SearchSettings>();

        await cut.Find(".calm-search-engine-name").ChangeAsync(new ChangeEventArgs { Value = "Mojeek" });
        await cut.Find(".calm-search-engine-url").ChangeAsync(new ChangeEventArgs { Value = "https://www.mojeek.com/search?q=%s" });
        await cut.Find(".calm-search-engine-form button").ClickAsync();

        await cut.WaitForAssertionAsync(() =>
        {
            Assert.Equal(5, cut.FindAll(".calm-search-engine-option").Count);
            Assert.Equal("true", cut.Find("[aria-label=\"Use Mojeek search\"]").GetAttribute("aria-pressed"));
            Assert.Contains("custom:mojeek", _configs.Stored["Search"], StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task AddingEngineWithBadUrl_ShowsErrorAndAddsNothing()
    {
        IRenderedComponent<SearchSettings> cut = Render<SearchSettings>();

        await cut.Find(".calm-search-engine-name").ChangeAsync(new ChangeEventArgs { Value = "Broken" });
        await cut.Find(".calm-search-engine-url").ChangeAsync(new ChangeEventArgs { Value = "https://example.com/search" });
        await cut.Find(".calm-search-engine-form button").ClickAsync();

        await cut.WaitForAssertionAsync(() => Assert.Equal(
            "Enter a search URL with %s where the query goes.",
            cut.Find(".calm-search-form-error").TextContent));
        Assert.Equal(4, cut.FindAll(".calm-search-engine-option").Count);
    }

    [Fact]
    public async Task DeletingActiveCustomEngine_RepairsToGoogle()
    {
        _configs.Stored["Search"] = "{\"EngineId\":\"custom:mojeek\",\"CustomEngines\":[{\"Id\":\"custom:mojeek\",\"Name\":\"Mojeek\",\"UrlTemplate\":\"https://www.mojeek.com/search?q=%s\"}]}";

        IRenderedComponent<SearchSettings> cut = Render<SearchSettings>();

        await cut.WaitForAssertionAsync(() => Assert.Equal(5, cut.FindAll(".calm-search-engine-option").Count));
        await cut.Find("[aria-label=\"Delete Mojeek search engine\"]").ClickAsync();

        await cut.WaitForAssertionAsync(() =>
        {
            Assert.Equal(4, cut.FindAll(".calm-search-engine-option").Count);
            Assert.Equal("true", cut.Find("[aria-label=\"Use Google search\"]").GetAttribute("aria-pressed"));
        });
        Assert.DoesNotContain("mojeek", _configs.Stored["Search"], StringComparison.Ordinal);
    }
}

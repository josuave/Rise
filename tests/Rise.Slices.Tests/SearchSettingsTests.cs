using AngleSharp.Dom;
using Bunit;
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
}

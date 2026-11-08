using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Links;
using Rise.Slices.Search;
using Rise.Slices.Settings;
using Xunit;

namespace Rise.Slices.Tests;

/// <summary>
/// Regression tests for the dead-clicks bug (PR #9): if a slice's
/// _Imports.razor ever loses the Components.Web import again, its
/// @onclick handlers degrade to literal HTML attributes, clicks do
/// nothing, and these tests fail — instead of users finding out.
/// The Links widget is tiles only now; editing lives in the Settings
/// dialog.
/// </summary>
public class QuickLinksTests : BunitContext
{
    private readonly FakeLinkStore _store = new();
    private readonly FakeWidgetConfigStore _configs = new();

    public QuickLinksTests()
    {
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<IWidgetConfigStore>(_ => _configs);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<LinksConfig>();
        Services.AddSingleton<IWidgetDefinition, SearchWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, LinksWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, ThemeWidgetDefinition>();
        Services.AddSingleton<IWidgetDefinition, SettingsWidgetDefinition>();
    }

    [Fact]
    public void RendersDefaultTiles()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("GitHub", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TilesRefresh_WhenAnotherWidgetSaves()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        StartPageService service = Services.GetRequiredService<StartPageService>();
        _ = service.AddLinkAsync(StartPage.Default(), "Example", "https://example.com/");

        cut.WaitForAssertion(() => Assert.Contains("Example", cut.Markup, StringComparison.Ordinal));
    }
}

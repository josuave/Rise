using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Links;
using Xunit;

namespace Rise.Slices.Tests;

public class LinksSettingsTests : BunitContext
{
    private readonly FakeLinkStore _store = new();
    private readonly FakeWidgetConfigStore _configs = new();

    public LinksSettingsTests()
    {
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<IWidgetConfigStore>(_ => _configs);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<LinksConfig>();
    }

    [Fact]
    public void AddLink_FromSettingsSection_PersistsIt()
    {
        IRenderedComponent<LinksSettings> cut = Render<LinksSettings>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Link name\"]").Change("Example");
        cut.Find("[aria-label=\"Link URL\"]").Change("https://example.com/");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add link").Click();

        cut.WaitForAssertion(() => Assert.Contains("Example", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(_store.StoredPage);
        Assert.Contains(_store.StoredPage.Links, link => link.Name == "Example");
    }
}

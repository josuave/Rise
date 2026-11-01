using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Links;
using Xunit;

namespace Rise.Slices.Tests;

/// <summary>
/// Regression tests for the dead-clicks bug (PR #9): if a slice's
/// _Imports.razor ever loses the Components.Web import again, its
/// @onclick handlers degrade to literal HTML attributes, clicks do
/// nothing, and these tests fail — instead of users finding out.
/// The Links widget is tiles only now; editing moved to LinksEditor.
/// </summary>
public class QuickLinksTests : BunitContext
{
    private readonly FakeLinkStore _store = new();
    private readonly FakeLayoutStore _layoutStore = new();

    public QuickLinksTests()
    {
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<ILayoutStore>(_ => _layoutStore);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<LayoutService>();
    }

    [Fact]
    public void RendersDefaultTiles()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("GitHub", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EditLinksButton_ShowsLinkEditorWidget()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        cut.Find(".calm-edit-toggle").Click();

        Assert.NotNull(_layoutStore.StoredLayout);
        Assert.True(_layoutStore.StoredLayout.Widgets.Single(w => w.Kind == WidgetKind.LinkEditor).Visible);
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

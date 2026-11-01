using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Links;
using Xunit;

namespace Rise.Slices.Tests;

/// <summary>Tests for the Link editor widget, split out of QuickLinks in the widget phase.</summary>
public class LinksEditorTests : BunitContext
{
    private readonly FakeLinkStore _store = new();
    private readonly FakeLayoutStore _layoutStore = new();

    public LinksEditorTests()
    {
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<ILayoutStore>(_ => _layoutStore);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<LayoutService>();
    }

    [Fact]
    public void AddLink_ShowsInList_AndPersistsIt()
    {
        IRenderedComponent<LinksEditor> cut = Render<LinksEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Link name\"]").Change("Example");
        cut.Find("[aria-label=\"Link URL\"]").Change("https://example.com/");
        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Example", cut.Markup, StringComparison.Ordinal));
        Assert.Contains(_store.StoredPage!.Links, l => l.Name == "Example");
    }

    [Fact]
    public void RemoveLink_RemovesOnlyThatRow()
    {
        IRenderedComponent<LinksEditor> cut = Render<LinksEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Remove Google\"]").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Google", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("GitHub", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDuplicate_ShowsError_AndDoesNotPersist()
    {
        IRenderedComponent<LinksEditor> cut = Render<LinksEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Link name\"]").Change("Again");
        cut.Find("[aria-label=\"Link URL\"]").Change("https://www.google.com/");
        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("already on the page", cut.Markup, StringComparison.Ordinal));
        // Nothing is ever persisted on a failed add, so the store may
        // still be empty (defaults come from the service, not the store).
        Assert.DoesNotContain(_store.StoredPage?.Links ?? [], l => l.Name == "Again");
    }

    [Fact]
    public void DoneButton_HidesEditorWidget()
    {
        IRenderedComponent<LinksEditor> cut = Render<LinksEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        cut.Find(".calm-edit-toggle").Click();

        Assert.NotNull(_layoutStore.StoredLayout);
        Assert.False(_layoutStore.StoredLayout.Widgets.Single(w => w.Kind == WidgetKind.LinkEditor).Visible);
    }
}

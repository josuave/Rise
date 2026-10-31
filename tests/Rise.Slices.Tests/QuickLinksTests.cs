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
/// </summary>
public class QuickLinksTests : BunitContext
{
    private readonly FakeLinkStore _store = new();

    public QuickLinksTests()
    {
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<StartPageService>();
    }

    [Fact]
    public void EditToggle_FlipsToDone_AndShowsAddForm()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));

        cut.Find(".calm-edit-toggle").Click();

        Assert.Equal("Done", cut.Find(".calm-edit-toggle").TextContent.Trim());
        Assert.NotNull(cut.Find(".calm-add-form"));
    }

    [Fact]
    public void AddLink_ShowsNewTile_AndPersistsIt()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));
        cut.Find(".calm-edit-toggle").Click();

        cut.Find("[aria-label=\"Link name\"]").Change("Example");
        cut.Find("[aria-label=\"Link URL\"]").Change("https://example.com/");
        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Example", cut.Markup, StringComparison.Ordinal));
        Assert.Contains(_store.StoredPage!.Links, l => l.Name == "Example");
    }

    [Fact]
    public void RemoveLink_RemovesOnlyThatTile()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));
        cut.Find(".calm-edit-toggle").Click();

        cut.Find("[aria-label=\"Remove Google\"]").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Google", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("GitHub", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDuplicate_ShowsError_AndDoesNotPersist()
    {
        IRenderedComponent<QuickLinks> cut = Render<QuickLinks>();
        cut.WaitForAssertion(() => Assert.Contains("Google", cut.Markup, StringComparison.Ordinal));
        cut.Find(".calm-edit-toggle").Click();

        cut.Find("[aria-label=\"Link name\"]").Change("Again");
        cut.Find("[aria-label=\"Link URL\"]").Change("https://www.google.com/");
        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("already on the page", cut.Markup, StringComparison.Ordinal));
        // Nothing is ever persisted on a failed add, so the store may
        // still be empty (defaults come from the service, not the store).
        // Either way, "Again" must not be in what was saved.
        Assert.DoesNotContain(_store.StoredPage?.Links ?? [], l => l.Name == "Again");
    }
}

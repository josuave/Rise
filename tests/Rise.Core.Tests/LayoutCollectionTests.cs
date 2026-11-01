using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class LayoutCollectionTests
{
    private static readonly string[] SingleSearch = new[] { "Search" };
    private static readonly string[] SearchLinks = new[] { "Search", "Links" };

    private static PageLayout Placements(params string[] ids) => new(ids
        .Select(id => new WidgetPlacement(id, WidgetZone.Main, true))
        .ToList());

    private static LayoutCollection TwoLayouts() => new("work", new[]
    {
        new NamedLayout(LayoutCollection.DefaultId, LayoutCollection.DefaultName, Placements("Search", "Links")),
        new NamedLayout("work", "Work", Placements("Links", "Search")),
    });

    [Fact]
    public void SetActive_UnknownId_IsNoOp()
    {
        LayoutCollection collection = TwoLayouts();

        Assert.Same(collection, collection.SetActive("nope"));
        Assert.Equal(LayoutCollection.DefaultId, collection.SetActive(LayoutCollection.DefaultId).ActiveId);
        Assert.Equal("work", collection.ActiveId);
    }

    [Fact]
    public void SaveAs_UniquifiesNames_AndActivatesTheCopy()
    {
        LayoutCollection collection = TwoLayouts();

        LayoutCollection saved = collection.SaveAs("Work", Placements("Search"));
        Assert.Equal("Work 2", saved.Active?.Name);
        Assert.Equal(3, saved.Layouts.Count);

        LayoutCollection savedAgain = saved.SaveAs("Work", Placements("Search"));
        Assert.Equal("Work 3", savedAgain.Active?.Name);
    }

    [Fact]
    public void SaveAs_BlankName_IsNoOp()
    {
        LayoutCollection collection = TwoLayouts();

        Assert.Same(collection, collection.SaveAs("   ", Placements("Search")));
    }

    [Fact]
    public void Rename_UniquifiesExcludingSelf()
    {
        LayoutCollection collection = TwoLayouts();

        Assert.Equal("Default 2", collection.Rename("work", "Default").Layouts.Single(l => l.Id == "work").Name);
        Assert.Equal("Work", collection.Rename("work", "Work").Layouts.Single(l => l.Id == "work").Name);
        Assert.Same(collection, collection.Rename("work", "   "));
    }

    [Fact]
    public void Delete_DefaultOrUnknown_IsNoOp_AndDeletingActiveFallsBack()
    {
        LayoutCollection collection = TwoLayouts();

        Assert.Same(collection, collection.Delete(LayoutCollection.DefaultId));
        Assert.Same(collection, collection.Delete("nope"));

        LayoutCollection deleted = collection.Delete("work");
        Assert.Single(deleted.Layouts);
        Assert.Equal(LayoutCollection.DefaultId, deleted.ActiveId);
    }

    [Fact]
    public void UpdateActive_ReplacesOnlyActiveLayout()
    {
        LayoutCollection collection = TwoLayouts();

        LayoutCollection updated = collection.UpdateActive(Placements("Search"));

        Assert.Equal(SingleSearch, updated.Active?.Layout.Widgets.Select(w => w.Id));
        Assert.Equal(
            SearchLinks,
            updated.Layouts.Single(l => l.Id == LayoutCollection.DefaultId).Layout.Widgets.Select(w => w.Id));
    }
}

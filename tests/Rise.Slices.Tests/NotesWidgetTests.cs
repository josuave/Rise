using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Notes;
using Xunit;

namespace Rise.Slices.Tests;

public class NotesWidgetTests : BunitContext
{
    private readonly FakeNoteStore _store = new();

    private readonly FakeWidgetConfigStore _configStore = new();

    public NotesWidgetTests()
    {
        Services.AddScoped<INoteStore>(_ => _store);
        Services.AddScoped<NotesService>();
        Services.AddScoped<IWidgetConfigStore>(_ => _configStore);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<NotesViewConfig>();
    }

    [Fact]
    public void ToggleView_SwitchesToPostItView_AndPersistsIt()
    {
        IRenderedComponent<NotesWidget> cut = Render<NotesWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Notes", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Switch to post-it view\"]").Click();

        cut.WaitForAssertion(() => Assert.Contains("calm-notes-postits", cut.Markup, StringComparison.Ordinal));
        Assert.True(_configStore.Stored.TryGetValue("Notes", out string? json));
        Assert.Contains("PostItView\":true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AddNote_ShowsInList_AndPersistsIt()
    {
        IRenderedComponent<NotesWidget> cut = Render<NotesWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Notes", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"New note\"]").Change("First note");
        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("First note", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(_store.StoredList);
        Assert.Single(_store.StoredList.Items);
        Assert.Equal("First note", _store.StoredList.Items[0].Text);
    }

    [Fact]
    public void AddBlank_ShowsError_AndDoesNotPersist()
    {
        IRenderedComponent<NotesWidget> cut = Render<NotesWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Notes", cut.Markup, StringComparison.Ordinal));

        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Write a note first.", cut.Markup, StringComparison.Ordinal));
        Assert.Null(_store.StoredList);
    }

    [Fact]
    public void EditNote_UpdatesText_AndPersists()
    {
        var seeded = new FakeNoteStore(new NoteList(new[]
        {
            new Note(Guid.NewGuid(), "First note"),
        }));
        Services.AddScoped<INoteStore>(_ => seeded);

        IRenderedComponent<NotesWidget> cut = Render<NotesWidget>();
        cut.WaitForAssertion(() => Assert.Contains("First note", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Edit First note\"]").Click();
        cut.Find("[aria-label=\"Edit note\"]").Change("Edited note");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Save").Click();

        cut.WaitForAssertion(() => Assert.Contains("Edited note", cut.Markup, StringComparison.Ordinal));
        cut.WaitForAssertion(() => Assert.DoesNotContain("First note", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(seeded.StoredList);
        Assert.Equal("Edited note", seeded.StoredList.Items[0].Text);
    }

    [Fact]
    public void EditBlank_ShowsError_AndKeepsEditing()
    {
        var seeded = new FakeNoteStore(new NoteList(new[]
        {
            new Note(Guid.NewGuid(), "First note"),
        }));
        Services.AddScoped<INoteStore>(_ => seeded);

        IRenderedComponent<NotesWidget> cut = Render<NotesWidget>();
        cut.WaitForAssertion(() => Assert.Contains("First note", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Edit First note\"]").Click();
        cut.Find("[aria-label=\"Edit note\"]").Change("   ");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Save").Click();

        cut.WaitForAssertion(() => Assert.Contains("A note can't be blank.", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Edit note", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(seeded.StoredList);
        Assert.Equal("First note", seeded.StoredList.Items[0].Text);
    }

    [Fact]
    public void DeleteNote_RemovesRow_AndPersists()
    {
        var seeded = new FakeNoteStore(new NoteList(new[]
        {
            new Note(Guid.NewGuid(), "First note"),
            new Note(Guid.NewGuid(), "Second note"),
        }));
        Services.AddScoped<INoteStore>(_ => seeded);

        IRenderedComponent<NotesWidget> cut = Render<NotesWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Second note", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Delete First note\"]").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("First note", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Second note", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(seeded.StoredList);
        Assert.Single(seeded.StoredList.Items);
        Assert.Equal("Second note", seeded.StoredList.Items[0].Text);
    }
}

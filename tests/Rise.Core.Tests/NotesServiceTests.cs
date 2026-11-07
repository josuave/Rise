using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class NotesServiceTests
{
    private sealed class FakeNoteStore : INoteStore
    {
        public NoteList? Stored { get; private set; }

        public FakeNoteStore(NoteList? initial = null) => Stored = initial;

        public Task<NoteList?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(NoteList list, CancellationToken cancellationToken = default)
        {
            Stored = list;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Load_ReturnsEmpty_WhenStoreIsEmpty()
    {
        var service = new NotesService(new FakeNoteStore());

        NoteList list = await service.LoadAsync();

        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task AddNote_SavesAndReturnsNote()
    {
        var store = new FakeNoteStore();
        var service = new NotesService(store);

        Result<NoteList> result = await service.AddNoteAsync(NoteList.Empty(), "First note");

        NoteList added = ResultAssertions.RequireOk(result);
        Assert.Single(added.Items);
        Assert.Equal("First note", added.Items[0].Text);
        Assert.NotNull(store.Stored);
        Assert.Single(store.Stored.Items);
    }

    [Fact]
    public async Task AddNote_BlankText_FailsWithoutSaving()
    {
        var store = new FakeNoteStore();
        var service = new NotesService(store);

        Result<NoteList> result = await service.AddNoteAsync(NoteList.Empty(), "   ");

        Assert.NotEmpty(ResultAssertions.RequireError(result));
        Assert.Null(store.Stored);
    }

    [Fact]
    public async Task UpdateNote_ChangesText_AndPersists()
    {
        var store = new FakeNoteStore();
        var service = new NotesService(store);
        Result<NoteList> added = await service.AddNoteAsync(NoteList.Empty(), "First note");
        NoteList addedList = ResultAssertions.RequireOk(added);

        Result<NoteList> updated = await service.UpdateNoteAsync(addedList, addedList.Items[0].Id, "  Edited note  ");

        NoteList updatedList = ResultAssertions.RequireOk(updated);
        Assert.Equal("Edited note", updatedList.Items[0].Text);
        Assert.NotNull(store.Stored);
        Assert.Equal("Edited note", store.Stored.Items[0].Text);
    }

    [Fact]
    public async Task UpdateNote_BlankText_FailsWithoutSaving()
    {
        var store = new FakeNoteStore();
        var service = new NotesService(store);
        Result<NoteList> added = await service.AddNoteAsync(NoteList.Empty(), "First note");
        NoteList addedList = ResultAssertions.RequireOk(added);

        Result<NoteList> result = await service.UpdateNoteAsync(addedList, addedList.Items[0].Id, "   ");

        Assert.NotEmpty(ResultAssertions.RequireError(result));
        Assert.NotNull(store.Stored);
        Assert.Equal("First note", store.Stored.Items[0].Text);
    }

    [Fact]
    public async Task DeleteNote_RemovesOnlyThatNote()
    {
        var store = new FakeNoteStore();
        var service = new NotesService(store);
        Result<NoteList> first = await service.AddNoteAsync(NoteList.Empty(), "First note");
        NoteList firstList = ResultAssertions.RequireOk(first);
        Result<NoteList> second = await service.AddNoteAsync(firstList, "Second note");
        NoteList secondList = ResultAssertions.RequireOk(second);

        NoteList deleted = await service.DeleteNoteAsync(secondList, secondList.Items[0].Id);

        Assert.Single(deleted.Items);
        Assert.Equal("Second note", deleted.Items[0].Text);
        Assert.NotNull(store.Stored);
        Assert.Single(store.Stored.Items);
    }
}

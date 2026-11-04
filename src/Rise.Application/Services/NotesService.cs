using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for notes: the only place that talks to the store.
/// It loads a list, hands it to the pure core (NoteList.AddNote and
/// friends), and persists whatever the core returns. It contains no
/// note rules itself.
/// </summary>
public sealed class NotesService(INoteStore store)
{
    /// <summary>
    /// Raised after any successful save, so widgets that render the
    /// same list can reload instead of going stale.
    /// </summary>
    public event EventHandler? Changed;

    public async Task<NoteList> LoadAsync(CancellationToken cancellationToken = default) =>
        await store.LoadAsync(cancellationToken) ?? NoteList.Empty();

    public async Task<Result<NoteList>> AddNoteAsync(NoteList list, string text, CancellationToken cancellationToken = default)
    {
        Result<NoteList> result = list.AddNote(text);
        if (result.IsSuccess && result.Value is not null)
        {
            await store.SaveAsync(result.Value, cancellationToken);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    public async Task<Result<NoteList>> UpdateNoteAsync(NoteList list, Guid id, string text, CancellationToken cancellationToken = default)
    {
        Result<NoteList> result = list.UpdateNote(id, text);
        if (result.IsSuccess && result.Value is not null)
        {
            await store.SaveAsync(result.Value, cancellationToken);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return result;
    }

    public async Task<NoteList> DeleteNoteAsync(NoteList list, Guid id, CancellationToken cancellationToken = default)
    {
        NoteList next = list.DeleteNote(id);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

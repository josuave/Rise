namespace Rise.Domain;

/// <summary>
/// The note list: pure functional core. Every operation takes a list
/// and returns a new list (or a failure) — it never touches storage.
/// The imperative shell (NotesService) is what persists the returned list.
/// </summary>
public sealed record NoteList(IReadOnlyList<Note> Items)
{
    public static NoteList Empty() => new([]);

    public Result<NoteList> AddNote(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<NoteList>.Failure("Write a note first.");
        }

        return Result<NoteList>.Success(new NoteList([..Items, Note.Create(text)]));
    }

    public Result<NoteList> UpdateNote(Guid id, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<NoteList>.Failure("A note can't be blank.");
        }

        return Result<NoteList>.Success(new NoteList(Items
            .Select(note => note.Id == id ? note with { Text = text.Trim() } : note)
            .ToList()));
    }

    public NoteList DeleteNote(Guid id) =>
        new(Items.Where(note => note.Id != id).ToList());
}

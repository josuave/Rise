using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>Outbound adapter: persists a note list in the browser's localStorage.</summary>
public sealed class LocalStorageNoteStore(IJSRuntime js) : INoteStore
{
    private const string StorageKey = "rise.notes.v1";

    // Stored shape: flat records, so the domain types never leak into the JSON.
    private sealed record StoredNote(Guid Id, string Text);

    public async Task<NoteList?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            List<StoredNote>? parsed = JsonSerializer.Deserialize<List<StoredNote>>(stored);
            if (parsed is null)
            {
                return null;
            }

            var notes = new List<Note>();
            foreach (StoredNote item in parsed)
            {
                if (string.IsNullOrWhiteSpace(item.Text))
                {
                    continue;
                }

                notes.Add(new Note(item.Id == Guid.Empty ? Guid.NewGuid() : item.Id, item.Text.Trim()));
            }

            return new NoteList(notes);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Corrupt or unavailable storage: the caller falls back to an empty list rather than breaking the page.
            return null;
        }
    }

    public async Task SaveAsync(NoteList list, CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = list.Items.Select(note => new StoredNote(note.Id, note.Text)).ToList();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Notes;

/// <summary>
/// The Notes widget: a list of plain-text notes (add, edit, delete).
/// It shares its list with no other widget, but reloads whenever the
/// notes service saves, so it never goes stale.
/// </summary>
public partial class NotesWidget : IDisposable
{
    [Inject]
    private NotesService NotesService { get; set; } = null!;

    private NoteList List { get; set; } = NoteList.Empty();

    private string NewText { get; set; } = string.Empty;

    private string? FormError { get; set; }

    private Guid? EditingId { get; set; }

    private string EditText { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        List = await NotesService.LoadAsync();
        NotesService.Changed += OnListChanged;
    }

    private async void OnListChanged(object? sender, EventArgs args)
    {
        List = await NotesService.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddAsync()
    {
        Result<NoteList> result = await NotesService.AddNoteAsync(List, NewText);
        if (result.IsSuccess && result.Value is not null)
        {
            List = result.Value;
            NewText = string.Empty;
            FormError = null;
        }
        else
        {
            FormError = result.Error;
        }
    }

    private void StartEdit(Note note)
    {
        EditingId = note.Id;
        EditText = note.Text;
        FormError = null;
    }

    private void CancelEdit() => EditingId = null;

    private async Task SaveEditAsync()
    {
        if (EditingId is null)
        {
            return;
        }

        Result<NoteList> result = await NotesService.UpdateNoteAsync(List, EditingId.Value, EditText);
        if (result.IsSuccess && result.Value is not null)
        {
            List = result.Value;
            EditingId = null;
            FormError = null;
        }
        else
        {
            FormError = result.Error;
        }
    }

    private async Task DeleteAsync(Note note) =>
        List = await NotesService.DeleteNoteAsync(List, note.Id);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            NotesService.Changed -= OnListChanged;
        }
    }
}

using System.Text.Json.Nodes;
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

    // Notes is a lazy module: no typed config is registered at
    // startup, so the widget reads its view setting through the
    // generic widget config service instead of a NotesViewConfig class.
    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    /// <summary>
    /// Null when the preview service isn't registered: the board
    /// default is off then, and the stored choice still rules. A
    /// nullable [Inject] still throws at activation when unregistered,
    /// so tests must register the service.
    /// </summary>
    [Inject]
    private PreviewFeatureService? PreviewFeatureService { get; set; }

    private NoteList List { get; set; } = NoteList.Empty();

    private JsonObject ViewData { get; set; } = new();

    private const string PostItViewKey = "PostItView";

    /// <summary>Whether the full-board flag is on; the post-it default when no choice is stored.</summary>
    private bool CalmBoardFull { get; set; }

    /// <summary>
    /// The stored choice always wins; with no stored key the full
    /// board defaults to post-its, and the plain board to the list.
    /// The toggle still persists, so toggling once stores a choice.
    /// </summary>
    private bool PostItView =>
        ViewData[PostItViewKey] is JsonValue value && value.TryGetValue<bool>(out bool stored)
            ? stored
            : CalmBoardFull;

    private string NewText { get; set; } = string.Empty;

    private string? FormError { get; set; }

    private Guid? EditingId { get; set; }

    private string EditText { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        List = await NotesService.LoadAsync();
        ViewData = await ConfigService.LoadMigratedAsync("Notes", 1, static (data, _) => data);
        await RefreshBoardFlagAsync();
        PreviewFeatureService?.Changed += OnPreviewFeaturesChanged;
        NotesService.Changed += OnListChanged;
    }

    private async void OnPreviewFeaturesChanged(object? sender, EventArgs args)
    {
        await RefreshBoardFlagAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshBoardFlagAsync() =>
        CalmBoardFull = PreviewFeatureService is not null
            && await PreviewFeatureService.IsEnabledAsync(PreviewFeatures.CalmBoardFull);

    private async void OnListChanged(object? sender, EventArgs args)
    {
        List = await NotesService.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddAsync()
    {
        Result<NoteList> result = await NotesService.AddNoteAsync(List, NewText);
        switch (result)
        {
            case Ok<NoteList> ok:
                List = ok.Value;
                NewText = string.Empty;
                FormError = null;
                break;
            case Error error:
                FormError = error.Message;
                break;
            case null:
                FormError = null;
                break;
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
        switch (result)
        {
            case Ok<NoteList> ok:
                List = ok.Value;
                EditingId = null;
                FormError = null;
                break;
            case Error error:
                FormError = error.Message;
                break;
            case null:
                FormError = null;
                break;
        }
    }

    private async Task DeleteAsync(Note note) =>
        List = await NotesService.DeleteNoteAsync(List, note.Id);

    private async Task ToggleViewAsync()
    {
        ViewData[PostItViewKey] = !PostItView;
        await ConfigService.SetJsonAsync("Notes", ViewData.ToJsonString());
    }

    /// <summary>
    /// A note's pastel by its id, stable across reloads and reorders:
    /// the id hash cycles through the five pastel classes.
    /// </summary>
    private static string PastelClass(Note note)
    {
        int index = (note.Id.GetHashCode() % 5 + 5) % 5;
        return $"calm-pastel-{index}";
    }

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
            PreviewFeatureService?.Changed -= OnPreviewFeaturesChanged;
        }
    }
}

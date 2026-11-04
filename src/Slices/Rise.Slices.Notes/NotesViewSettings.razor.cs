using Microsoft.AspNetCore.Components;

namespace Rise.Slices.Notes;

/// <summary>
/// The Notes slice's Settings section content: list or post-it view,
/// saved immediately on change. The widget's own toggle stays as the
/// one-tap shortcut (the theme pattern: dialog setting plus quick
/// toggle); both read the same config.
/// </summary>
public partial class NotesViewSettings
{
    [Inject]
    private NotesViewConfig ViewConfig { get; set; } = null!;

    private bool PostItView => ViewConfig.PostItView;

    protected override async Task OnInitializedAsync() =>
        await ViewConfig.LoadAsync();

    private async Task SetViewAsync(bool postIt)
    {
        ViewConfig.PostItView = postIt;
        await ViewConfig.SaveAsync();
    }
}

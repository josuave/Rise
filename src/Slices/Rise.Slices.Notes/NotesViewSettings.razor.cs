using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Rise.Application.Services;

namespace Rise.Slices.Notes;

/// <summary>
/// The Notes slice's Settings section content: list or post-it view,
/// saved immediately on change. The widget's own toggle stays as the
/// one-tap shortcut (the theme pattern: dialog setting plus quick
/// toggle); both read the same config. Notes is a lazy module, so
/// this reads through the generic widget config service rather than
/// a typed config class, which cannot be registered at startup.
/// </summary>
public partial class NotesViewSettings
{
    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private JsonObject ViewData { get; set; } = new();

    private const string PostItViewKey = "PostItView";

    private bool PostItView => WidgetConfigJson.GetBool(ViewData, PostItViewKey);

    protected override async Task OnInitializedAsync() =>
        ViewData = await ConfigService.LoadMigratedAsync("Notes", 1, static (data, _) => data);

    private async Task SetViewAsync(bool postIt)
    {
        ViewData[PostItViewKey] = postIt;
        await ConfigService.SetJsonAsync("Notes", ViewData.ToJsonString());
    }
}

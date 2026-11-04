using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Notes;

/// <summary>
/// The notes widget's view configuration: list or post-it view, a
/// self-managing object over its JSON (currently v1: no migrations
/// yet, so Migrate is an identity). The widget's own toggle button
/// flips it — no config editor — so switching views stays one click.
/// A missing key reads as the list view.
/// </summary>
public sealed class NotesViewConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => NotesWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; a missing key reads as the default.</summary>
    public JsonObject Data { get; private set; } = new();

    /// <summary>True for the post-it view, false for the plain list. Default false.</summary>
    public bool PostItView
    {
        get => WidgetConfigJson.GetBool(Data, nameof(PostItView));
        set => Data[nameof(PostItView)] = value;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        Data = await service.LoadMigratedAsync(WidgetId, CurrentVersion, Migrate, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await service.SetJsonAsync(WidgetId, Data.ToJsonString(), cancellationToken);
}

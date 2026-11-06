using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Search;

/// <summary>
/// The search widget's configuration: which engine the search box
/// and the command palette use, plus the user-added engines. Self-managing
/// over its JSON (v1, identity Migrate). An unknown stored engine id
/// reads as Google; stored custom engines with a blank name or an
/// unusable template are skipped on load.
/// </summary>
public sealed class SearchConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => SearchWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; a missing key reads as the default.</summary>
    public JsonObject Data { get; private set; } = new();

    /// <summary>The configured engine id; a custom id passes through as-is, unknown or missing reads as Google.</summary>
    public string EngineId
    {
        get
        {
            string? stored = WidgetConfigJson.GetString(Data, nameof(EngineId));
            if (stored is not null && stored.StartsWith(SearchEngine.CustomPrefix, StringComparison.Ordinal))
            {
                return stored;
            }

            return SearchEngines.FromId(stored).Id;
        }

        set => Data[nameof(EngineId)] = value;
    }

    /// <summary>The user-added engines, in the order they were added. Stored as an array of { Id, Name, UrlTemplate } objects.</summary>
    public IReadOnlyList<SearchEngine> CustomEngines
    {
        get
        {
            if (Data[nameof(CustomEngines)] is not JsonArray array)
            {
                return Array.Empty<SearchEngine>();
            }

            var engines = new List<SearchEngine>();
            foreach (JsonNode? node in array)
            {
                if (node is not JsonObject obj)
                {
                    continue;
                }

                string? id = WidgetConfigJson.GetString(obj, nameof(SearchEngine.Id));
                string? name = WidgetConfigJson.GetString(obj, nameof(SearchEngine.Name));
                string? template = WidgetConfigJson.GetString(obj, nameof(SearchEngine.UrlTemplate));
                if (string.IsNullOrWhiteSpace(id)
                    || string.IsNullOrWhiteSpace(name)
                    || !SearchEngine.IsValidTemplate(template))
                {
                    continue;
                }

                engines.Add(new SearchEngine(id, name, template!));
            }

            return engines;
        }

        set
        {
            var array = new JsonArray();
            foreach (SearchEngine engine in value)
            {
                array.Add(new JsonObject
                {
                    [nameof(SearchEngine.Id)] = engine.Id,
                    [nameof(SearchEngine.Name)] = engine.Name,
                    [nameof(SearchEngine.UrlTemplate)] = engine.UrlTemplate,
                });
            }

            Data[nameof(CustomEngines)] = array;
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        Data = await service.LoadMigratedAsync(WidgetId, CurrentVersion, Migrate, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await service.SetJsonAsync(WidgetId, Data.ToJsonString(), cancellationToken);
}

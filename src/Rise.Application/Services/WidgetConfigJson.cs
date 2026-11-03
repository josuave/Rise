using System.Text.Json.Nodes;

namespace Rise.Application.Services;

/// <summary>
/// Small JSON readers for widget configs: typed getters with
/// defaults, so a missing or mistyped key reads as the default
/// instead of throwing. Config classes use these for their typed
/// properties over their JsonObject data.
/// </summary>
public static class WidgetConfigJson
{
    public static bool GetBool(JsonObject data, string key) =>
        data[key] is JsonValue value && value.TryGetValue<bool>(out bool result) && result;

    public static string? GetString(JsonObject data, string key) =>
        data[key] is JsonValue value && value.TryGetValue<string>(out string? text) ? text : null;
}

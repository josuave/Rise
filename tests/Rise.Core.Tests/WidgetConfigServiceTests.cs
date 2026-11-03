using System.Text.Json.Nodes;
using Rise.Application.Ports;
using Rise.Application.Services;
using Xunit;

namespace Rise.Core.Tests;

public class WidgetConfigServiceTests
{
    private sealed class FakeWidgetConfigStore : IWidgetConfigStore
    {
        public Dictionary<string, string> Stored { get; }

        public FakeWidgetConfigStore(Dictionary<string, string>? stored = null) =>
            Stored = stored ?? new Dictionary<string, string>();

        public Task<string?> GetAsync(string widgetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.TryGetValue(widgetId, out string? json) ? json : null);

        public Task SetAsync(string widgetId, string json, CancellationToken cancellationToken = default)
        {
            Stored[widgetId] = json;
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private static JsonObject RenameOldKey(JsonObject data, int fromVersion)
    {
        if (data.TryGetPropertyValue("OldKey", out JsonNode? old))
        {
            data.Remove("OldKey");
            data["NewKey"] = old;
        }

        return data;
    }

    [Fact]
    public async Task LoadMigrated_Absent_ReturnsEmpty_WithoutSaving()
    {
        var store = new FakeWidgetConfigStore();
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync("Clock", 1, (data, _) => data);

        Assert.Empty(result);
        Assert.Empty(store.Stored);
    }

    [Fact]
    public async Task LoadMigrated_CurrentVersion_ReturnsAsIs_WithoutSaving()
    {
        const string stored = "{\"Version\":1,\"Use24Hour\":true}";
        var store = new FakeWidgetConfigStore(new Dictionary<string, string> { ["Clock"] = stored });
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync("Clock", 1, (data, _) => data);

        Assert.True(result["Use24Hour"]?.GetValue<bool>() ?? false);
        Assert.Equal(stored, store.Stored["Clock"]);
    }

    [Fact]
    public async Task LoadMigrated_MissingVersion_TreatedAsV1()
    {
        const string stored = "{\"Use24Hour\":true}";
        var store = new FakeWidgetConfigStore(new Dictionary<string, string> { ["Clock"] = stored });
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync("Clock", 1, (data, _) => data);

        Assert.True(result["Use24Hour"]?.GetValue<bool>() ?? false);
        Assert.Equal(stored, store.Stored["Clock"]);
    }

    [Fact]
    public async Task LoadMigrated_OlderVersion_MigratesAndSavesBack()
    {
        var store = new FakeWidgetConfigStore(new Dictionary<string, string>
        {
            ["Clock"] = "{\"Version\":1,\"OldKey\":true}",
        });
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync("Clock", 2, RenameOldKey);

        Assert.True(result["NewKey"]?.GetValue<bool>() ?? false);
        Assert.Null(result["OldKey"]);
        Assert.Equal(2, result["Version"]?.GetValue<int>());
        JsonObject saved = JsonNode.Parse(store.Stored["Clock"])!.AsObject();
        Assert.True(saved["NewKey"]?.GetValue<bool>() ?? false);
        Assert.Equal(2, saved["Version"]?.GetValue<int>());
    }

    [Fact]
    public async Task LoadMigrated_NewerVersion_PassesThrough_WithoutSaving()
    {
        const string stored = "{\"Version\":5,\"Use24Hour\":true}";
        var store = new FakeWidgetConfigStore(new Dictionary<string, string> { ["Clock"] = stored });
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync("Clock", 2, (data, _) => data);

        Assert.True(result["Use24Hour"]?.GetValue<bool>() ?? false);
        Assert.Equal(stored, store.Stored["Clock"]);
    }

    [Fact]
    public async Task LoadMigrated_Corrupt_ReturnsEmpty()
    {
        var store = new FakeWidgetConfigStore(new Dictionary<string, string> { ["Clock"] = "not json" });
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync("Clock", 1, (data, _) => data);

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadMigrated_NonObject_ReturnsEmpty()
    {
        var store = new FakeWidgetConfigStore(new Dictionary<string, string> { ["Clock"] = "[1,2]" });
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync("Clock", 1, (data, _) => data);

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadMigrated_FailingMigration_ReturnsOriginal_WithoutSaving()
    {
        const string stored = "{\"Version\":1,\"Use24Hour\":true}";
        var store = new FakeWidgetConfigStore(new Dictionary<string, string> { ["Clock"] = stored });
        var service = new WidgetConfigService(store);

        JsonObject result = await service.LoadMigratedAsync(
            "Clock",
            2,
            (data, _) => throw new InvalidOperationException("boom"));

        Assert.True(result["Use24Hour"]?.GetValue<bool>() ?? false);
        Assert.Equal(stored, store.Stored["Clock"]);
    }
}

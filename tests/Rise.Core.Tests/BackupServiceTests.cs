using System.Text.Json.Nodes;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class BackupServiceTests
{
    private sealed record TestDefinition(
        string Id,
        string Title,
        WidgetZone Zone,
        bool DefaultVisible,
        int Order,
        Type ComponentType) : IWidgetDefinition;

    private sealed class FakeLinkStore : ILinkStore
    {
        public StartPage? Stored { get; private set; }

        public Task<StartPage?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(StartPage page, CancellationToken cancellationToken = default)
        {
            Stored = page;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeThemeStore : IThemeStore
    {
        public string? Stored { get; private set; }

        public Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default)
        {
            Stored = theme;
            return Task.CompletedTask;
        }

        public Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ApplyCustomThemeAsync(ThemePalette palette, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeCustomThemeStore : ICustomThemeStore
    {
        public IReadOnlyList<CustomTheme>? Stored { get; private set; }

        public Task<IReadOnlyList<CustomTheme>?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(IReadOnlyList<CustomTheme> themes, CancellationToken cancellationToken = default)
        {
            Stored = themes;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLayoutCollectionStore : ILayoutCollectionStore
    {
        public LayoutCollection? Stored { get; private set; }

        public Task<LayoutCollection?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(LayoutCollection collection, CancellationToken cancellationToken = default)
        {
            Stored = collection;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWidgetConfigStore : IWidgetConfigStore
    {
        public Dictionary<string, string> Stored { get; } = new();

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

    private sealed class FakeTodoStore : ITodoStore
    {
        public TodoList? Stored { get; private set; }

        public Task<TodoList?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(TodoList list, CancellationToken cancellationToken = default)
        {
            Stored = list;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNoteStore : INoteStore
    {
        public NoteList? Stored { get; private set; }

        public Task<NoteList?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(NoteList list, CancellationToken cancellationToken = default)
        {
            Stored = list;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeModuleStore : IModuleStore
    {
        public Dictionary<string, bool> Stored { get; } = new();

        public Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, bool>(Stored));

        public Task SetAsync(string moduleId, bool enabled, CancellationToken cancellationToken = default)
        {
            Stored[moduleId] = enabled;
            return Task.CompletedTask;
        }

        public Task SaveAllAsync(Dictionary<string, bool> choices, CancellationToken cancellationToken = default)
        {
            Stored.Clear();
            foreach (KeyValuePair<string, bool> pair in choices)
            {
                Stored[pair.Key] = pair.Value;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeBackgroundStore : IBackgroundStore
    {
        public BackgroundChoice? Stored { get; private set; }

        public Task<BackgroundChoice?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(BackgroundChoice choice, CancellationToken cancellationToken = default)
        {
            Stored = choice;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePreviewFeatureStore : IPreviewFeatureStore
    {
        public Dictionary<string, bool> Stored { get; } = new();

        public Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, bool>(Stored));

        public Task SetAsync(string featureId, bool enabled, CancellationToken cancellationToken = default)
        {
            Stored[featureId] = enabled;
            return Task.CompletedTask;
        }

        public Task SaveAllAsync(Dictionary<string, bool> choices, CancellationToken cancellationToken = default)
        {
            Stored.Clear();
            foreach (KeyValuePair<string, bool> pair in choices)
            {
                Stored[pair.Key] = pair.Value;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeHotkeyStore : IHotkeyStore
    {
        public Dictionary<string, HotkeyBinding> Stored { get; } = new();

        public Task<Dictionary<string, HotkeyBinding>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, HotkeyBinding>(Stored));

        public Task SetAsync(string actionId, HotkeyBinding binding, CancellationToken cancellationToken = default)
        {
            Stored[actionId] = binding;
            return Task.CompletedTask;
        }

        public Task SaveAllAsync(Dictionary<string, HotkeyBinding> bindings, CancellationToken cancellationToken = default)
        {
            Stored.Clear();
            foreach (KeyValuePair<string, HotkeyBinding> pair in bindings)
            {
                Stored[pair.Key] = pair.Value;
            }

            return Task.CompletedTask;
        }
    }

    private sealed record TestGroupDefinition(
        string Id,
        string Title,
        WidgetZone Zone,
        bool DefaultVisible,
        int Order,
        Type ComponentType) : IWidgetDefinition
    {
        public bool IsGroup => true;
    }

    private static readonly string[] GroupedLinks = new[] { "Links" };

    private sealed class Fixture
    {
        public FakeLinkStore Links { get; } = new();

        public FakeThemeStore Theme { get; } = new();

        public FakeCustomThemeStore Themes { get; } = new();

        public FakeLayoutCollectionStore Layouts { get; } = new();

        public FakeWidgetConfigStore Widgets { get; } = new();

        public FakeTodoStore Todos { get; } = new();

        public FakeNoteStore Notes { get; } = new();

        public FakeModuleStore Modules { get; } = new();

        public FakeHotkeyStore Hotkeys { get; } = new();

        public FakePreviewFeatureStore PreviewFeatures { get; } = new();

        public FakeBackgroundStore Background { get; } = new();

        public BackupService Service { get; }

        public Fixture(IWidgetDefinition[]? definitions = null)
        {
            var pageService = new StartPageService(Links);
            var themeService = new CustomThemeService(Themes, Theme);
            var layoutService = new LayoutService(Layouts, definitions ?? new IWidgetDefinition[]
            {
                new TestDefinition("Search", "Search", WidgetZone.Main, true, 10, typeof(object)),
                new TestDefinition("Links", "Links", WidgetZone.Main, true, 20, typeof(object)),
            });
            var todoService = new TodoService(Todos);
            var notesService = new NotesService(Notes);
            Service = new BackupService(pageService, themeService, layoutService, todoService, notesService, Links, Theme, Themes, Layouts, Widgets, Todos, Notes, Modules, Hotkeys, PreviewFeatures, Background);
        }
    }

    private static async Task SeedAsync(Fixture fixture)
    {
        await fixture.Links.SaveAsync(new StartPage(new[]
        {
            new Link(Guid.NewGuid(), "Example", new LinkUrl("https://example.com/")),
        }));
        await fixture.Theme.SetStoredThemeAsync("dark");
        await fixture.Themes.SaveAsync(new[]
        {
            new CustomTheme("t1", "Mine", ThemePalette.DefaultLight, ThemePalette.DefaultDark),
        });
        await fixture.Layouts.SaveAsync(new LayoutCollection("default", new[]
        {
            new NamedLayout("default", "Default", new PageLayout(new[]
            {
                new WidgetPlacement("Search", WidgetZone.Main, true),
            })),
        }));
        await fixture.Widgets.SetAsync("Clock", "{\"Use24Hour\":true,\"ShowSeconds\":false}");
        await fixture.Todos.SaveAsync(new TodoList(new[]
        {
            new TodoItem(Guid.NewGuid(), "Buy milk", false),
        }));
        await fixture.Notes.SaveAsync(new NoteList(new[]
        {
            new Note(Guid.NewGuid(), "First note"),
        }));
    }

    [Fact]
    public async Task Export_WritesCurrentVersionEnvelope_WithEffectiveState()
    {
        var fixture = new Fixture();
        await SeedAsync(fixture);

        string json = await fixture.Service.ExportAsync();

        Result<BackupEnvelope> parsed = BackupFormat.Parse(json);
        Assert.True(parsed.IsSuccess);
        Assert.NotNull(parsed.Value);
        Assert.Equal(BackupFormat.CurrentVersion, parsed.Value.FormatVersion);
        BackupStateDto state = System.Text.Json.JsonSerializer.Deserialize<BackupStateDto>(
            parsed.Value.State.ToJsonString())!;
        Assert.Single(state.Links);
        Assert.Equal("Example", state.Links[0].Name);
        Assert.Equal("dark", state.Theme);
        Assert.Single(state.Themes);
        Assert.Equal("Mine", state.Themes[0].Name);
        Assert.Equal("default", state.Layouts.ActiveId);
        Assert.Equal(2, state.Layouts.Layouts[0].Widgets.Count);
        Assert.Empty(state.Layouts.Layouts[0].GroupChildren);
        Assert.Equal("{\"Use24Hour\":true,\"ShowSeconds\":false}", state.WidgetConfigs["Clock"]);
        Assert.NotNull(state.Todos);
        Assert.Single(state.Todos);
        Assert.Equal("Buy milk", state.Todos[0].Text);
        Assert.False(state.Todos[0].Done);
        Assert.NotNull(state.Notes);
        Assert.Single(state.Notes);
        Assert.Equal("First note", state.Notes[0].Text);
    }

    [Fact]
    public async Task Import_RoundTrip_RestoresExportedState()
    {
        var source = new Fixture();
        await SeedAsync(source);
        string json = await source.Service.ExportAsync();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(json);

        Assert.True(result.IsSuccess);
        Assert.NotNull(target.Links.Stored);
        Assert.Single(target.Links.Stored.Links);
        Assert.Equal("Example", target.Links.Stored.Links[0].Name);
        Assert.Equal("dark", target.Theme.Stored);
        Assert.NotNull(target.Themes.Stored);
        Assert.Single(target.Themes.Stored);
        Assert.Equal("Mine", target.Themes.Stored[0].Name);
        Assert.NotNull(target.Layouts.Stored);
        Assert.Equal("default", target.Layouts.Stored.ActiveId);
        Assert.Equal(2, target.Layouts.Stored.Layouts[0].Layout.Widgets.Count);
        Assert.Equal("{\"Use24Hour\":true,\"ShowSeconds\":false}", target.Widgets.Stored["Clock"]);
        Assert.NotNull(target.Todos.Stored);
        Assert.Single(target.Todos.Stored.Items);
        Assert.Equal("Buy milk", target.Todos.Stored.Items[0].Text);
        Assert.NotNull(target.Notes.Stored);
        Assert.Single(target.Notes.Stored.Items);
        Assert.Equal("First note", target.Notes.Stored.Items[0].Text);
    }

    [Fact]
    public async Task Hotkeys_RoundTrip_RestoresOverrides()
    {
        var source = new Fixture();
        source.Hotkeys.Stored["toggle-theme"] = new HotkeyBinding("y", Mod: false, Alt: false, Shift: false);
        string json = await source.Service.ExportAsync();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(json);

        Assert.True(result.IsSuccess);
        Assert.True(target.Hotkeys.Stored.TryGetValue("toggle-theme", out HotkeyBinding? restored));
        Assert.Equal("y", restored.Key);
    }

    [Fact]
    public async Task PreviewFeatures_RoundTrip_RestoresChoices()
    {
        var source = new Fixture();
        source.PreviewFeatures.Stored["calm-board"] = true;
        string json = await source.Service.ExportAsync();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(json);

        Assert.True(result.IsSuccess);
        Assert.True(target.PreviewFeatures.Stored.TryGetValue("calm-board", out bool restored));
        Assert.True(restored);
    }

    [Fact]
    public async Task Background_RoundTrip_RestoresCustomUrl()
    {
        var source = new Fixture();
        await source.Background.SaveAsync(new BackgroundChoice(BackgroundPresets.Custom, "https://example.com/photo.jpg"));
        string json = await source.Service.ExportAsync();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundPresets.Custom, target.Background.Stored?.PresetId);
        Assert.Equal("https://example.com/photo.jpg", target.Background.Stored?.CustomUrl);
    }

    [Fact]
    public async Task Background_RoundTrip_RestoresUploadDescriptor()
    {
        var source = new Fixture();
        await source.Background.SaveAsync(new BackgroundChoice(BackgroundPresets.Upload, null, new UploadedImage(new string('A', 64), "sunset.jpg", 12, "image/jpeg")));
        string json = await source.Service.ExportAsync();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundPresets.Upload, target.Background.Stored?.PresetId);
        Assert.Equal(new string('A', 64), target.Background.Stored?.Upload?.Hash);
        Assert.Equal("sunset.jpg", target.Background.Stored?.Upload?.FileName);
    }

    [Fact]
    public async Task Import_RejectsNewerVersion_AndSavesNothing()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["FormatVersion"] = 2;
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.False(result.IsSuccess);
        Assert.Null(target.Links.Stored);
        Assert.Null(target.Theme.Stored);
        Assert.Null(target.Themes.Stored);
        Assert.Null(target.Layouts.Stored);
        Assert.Empty(target.Widgets.Stored);
        Assert.Null(target.Todos.Stored);
        Assert.Null(target.Notes.Stored);
    }

    [Fact]
    public async Task Import_RejectsCorruptJson_AndSavesNothing()
    {
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync("not json");

        Assert.False(result.IsSuccess);
        Assert.Null(target.Links.Stored);
    }

    [Fact]
    public async Task Import_RejectsMissingSection_AndSavesNothing()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["State"]!.AsObject().Remove("Themes");
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.False(result.IsSuccess);
        Assert.Null(target.Links.Stored);
        Assert.Null(target.Theme.Stored);
    }

    [Fact]
    public async Task Import_RoundTrip_PreservesGroupMembership()
    {
        var definitions = new IWidgetDefinition[]
        {
            new TestDefinition("Search", "Search", WidgetZone.Main, true, 10, typeof(object)),
            new TestDefinition("Links", "Links", WidgetZone.Main, true, 20, typeof(object)),
            new TestGroupDefinition("Group", "Group", WidgetZone.Main, false, 45, typeof(object)),
        };
        var source = new Fixture(definitions);
        await source.Layouts.SaveAsync(new LayoutCollection("default", new[]
        {
            new NamedLayout("default", "Default", new PageLayout(
                new[]
                {
                    new WidgetPlacement("Search", WidgetZone.Main, true),
                    new WidgetPlacement("Links", WidgetZone.Main, true),
                    new WidgetPlacement("Group", WidgetZone.Main, false),
                },
                GroupedLinks)),
        }));
        string json = await source.Service.ExportAsync();
        var target = new Fixture(definitions);

        Result<BackupEnvelope> result = await target.Service.ImportAsync(json);

        Assert.True(result.IsSuccess);
        Assert.NotNull(target.Layouts.Stored);
        PageLayout imported = target.Layouts.Stored.Layouts[0].Layout;
        Assert.Equal(GroupedLinks, imported.GroupChildren);
        Assert.Equal(3, imported.Widgets.Count);
    }

    [Fact]
    public async Task Import_FileWithoutGroupChildren_ReadsAsEmpty()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["State"]!.AsObject()["Layouts"]!.AsObject()["Layouts"]!.AsArray()[0]!.AsObject().Remove("GroupChildren");
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.True(result.IsSuccess);
        Assert.NotNull(target.Layouts.Stored);
        Assert.Empty(target.Layouts.Stored.Layouts[0].Layout.GroupChildren);
    }

    [Fact]
    public async Task Import_FileWithoutTodosAndNotes_ReadsAsEmpty()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["State"]!.AsObject().Remove("Todos");
        root["State"]!.AsObject().Remove("Notes");
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.True(result.IsSuccess);
        Assert.NotNull(target.Todos.Stored);
        Assert.Empty(target.Todos.Stored.Items);
        Assert.NotNull(target.Notes.Stored);
        Assert.Empty(target.Notes.Stored.Items);
    }

    [Fact]
    public async Task Import_RejectsBlankTodoText_AndSavesNothing()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["State"]!.AsObject()["Todos"]!.AsArray()[0]!.AsObject()["Text"] = "   ";
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.False(result.IsSuccess);
        Assert.Null(target.Links.Stored);
        Assert.Null(target.Todos.Stored);
        Assert.Null(target.Notes.Stored);
    }

    [Fact]
    public async Task Import_RepairsOrphanedCustomTheme_ToSystem()
    {
        var source = new Fixture();
        await SeedAsync(source);
        JsonObject root = JsonNode.Parse(await source.Service.ExportAsync())!.AsObject();
        root["State"]!.AsObject()["Theme"] = "custom:missing";
        root["State"]!.AsObject()["Themes"] = new JsonArray();
        var target = new Fixture();

        Result<BackupEnvelope> result = await target.Service.ImportAsync(root.ToJsonString());

        Assert.True(result.IsSuccess);
        Assert.Equal("system", target.Theme.Stored);
    }
}

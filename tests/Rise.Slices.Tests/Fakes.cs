using System.Collections.ObjectModel;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Tests;

/// <summary>In-memory link store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeLinkStore : ILinkStore
{
    public StartPage? StoredPage { get; private set; }

    public FakeLinkStore(StartPage? initial = null) => StoredPage = initial;

    public Task<StartPage?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredPage);

    public Task SaveAsync(StartPage page, CancellationToken cancellationToken = default)
    {
        StoredPage = page;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory to-do store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeTodoStore : ITodoStore
{
    public TodoList? StoredList { get; private set; }

    public FakeTodoStore(TodoList? initial = null) => StoredList = initial;

    public Task<TodoList?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredList);

    public Task SaveAsync(TodoList list, CancellationToken cancellationToken = default)
    {
        StoredList = list;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory note store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeNoteStore : INoteStore
{
    public NoteList? StoredList { get; private set; }

    public FakeNoteStore(NoteList? initial = null) => StoredList = initial;

    public Task<NoteList?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredList);

    public Task SaveAsync(NoteList list, CancellationToken cancellationToken = default)
    {
        StoredList = list;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory 2048 store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeGame2048Store : IGame2048Store
{
    public Game2048State? StoredState { get; private set; }

    public FakeGame2048Store(Game2048State? initial = null) => StoredState = initial;

    public Task<Game2048State?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredState);

    public Task SaveAsync(Game2048State state, CancellationToken cancellationToken = default)
    {
        StoredState = state;
        return Task.CompletedTask;
    }
}

/// <summary>Search launcher that records URLs instead of opening tabs.</summary>
internal sealed class FakeSearchLauncher : ISearchLauncher
{
    public Collection<Uri> LaunchedUrls { get; } = new();

    public Task LaunchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        LaunchedUrls.Add(url);
        return Task.CompletedTask;
    }
}

/// <summary>Clock that always reports the same moment, for header tests.</summary>
internal sealed class FakeClock(DateTime now) : IClock
{
    public DateTime Now => now;
}

/// <summary>In-memory custom theme store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeCustomThemeStore : ICustomThemeStore
{
    public IReadOnlyList<CustomTheme>? StoredThemes { get; set; }

    public FakeCustomThemeStore(IReadOnlyList<CustomTheme>? initial = null) => StoredThemes = initial;

    public Task<IReadOnlyList<CustomTheme>?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredThemes);

    public Task SaveAsync(IReadOnlyList<CustomTheme> themes, CancellationToken cancellationToken = default)
    {
        StoredThemes = themes;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory layout collection store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeLayoutCollectionStore : ILayoutCollectionStore
{
    public LayoutCollection? StoredCollection { get; private set; }

    public FakeLayoutCollectionStore(LayoutCollection? initial = null) => StoredCollection = initial;

    public Task<LayoutCollection?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoredCollection);

    public Task SaveAsync(LayoutCollection collection, CancellationToken cancellationToken = default)
    {
        StoredCollection = collection;
        return Task.CompletedTask;
    }
}

/// <summary>Theme store that records preference/apply calls instead of touching the DOM.</summary>
internal sealed class FakeThemeStore : IThemeStore
{
    public string? Stored { get; private set; }
    public string? Applied { get; private set; }
    public ThemePalette? AppliedPalette { get; private set; }
    public bool SystemPrefersDark { get; set; }

    public event EventHandler? Changed;

    public FakeThemeStore(string? stored = null, bool systemPrefersDark = false)
    {
        Stored = stored;
        SystemPrefersDark = systemPrefersDark;
    }

    public Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Stored);

    public Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        Stored = theme;
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        Applied = theme;
        return Task.CompletedTask;
    }

    public Task ApplyCustomThemeAsync(ThemePalette palette, CancellationToken cancellationToken = default)
    {
        AppliedPalette = palette;
        return Task.CompletedTask;
    }

    public Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(SystemPrefersDark);
}

/// <summary>In-memory widget config store for slice tests: no localStorage, no JS.</summary>
internal sealed class FakeWidgetConfigStore : IWidgetConfigStore
{
    public Dictionary<string, string> Stored { get; } = new();

    public FakeWidgetConfigStore(Dictionary<string, string>? initial = null)
    {
        if (initial is not null)
        {
            foreach (KeyValuePair<string, string> pair in initial)
            {
                Stored[pair.Key] = pair.Value;
            }
        }
    }

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

/// <summary>In-memory module store for slice tests: explicit choices only, like the real store.</summary>
internal sealed class FakeModuleStore : IModuleStore
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

/// <summary>Module catalog for slice tests: empty by default, so nothing is filtered unless a test fills it.</summary>
internal sealed class FakeModuleCatalog : IModuleCatalog
{
    public IReadOnlyList<ModuleInfo> Modules { get; set; } = Array.Empty<ModuleInfo>();
}

/// <summary>Omarchy theme pack for slice tests: empty by default; a test fills Themes to list pack themes.</summary>
internal sealed class FakeOmarchyThemeProvider : IOmarchyThemeProvider
{
    public IReadOnlyList<OmarchyTheme> Themes { get; set; } = Array.Empty<OmarchyTheme>();
}

/// <summary>Omarchy theme cache for slice tests: records the last written pack list.</summary>
internal sealed class FakeOmarchyThemeCache : IOmarchyThemeCache
{
    public IReadOnlyList<OmarchyTheme>? Written { get; private set; }

    public Task WriteCacheAsync(IReadOnlyList<OmarchyTheme> themes, CancellationToken cancellationToken = default)
    {
        Written = themes;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory hotkey store for slice tests.</summary>
internal sealed class FakeBackgroundStore : IBackgroundStore
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

internal sealed class FakePreviewFeatureStore : IPreviewFeatureStore
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

internal sealed class FakeHotkeyStore : IHotkeyStore
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

/// <summary>In-memory file downloader for slice tests: records downloads instead of saving files.</summary>
internal sealed class FakeFileDownloader : IFileDownloader
{
    public List<(string Filename, string Content)> Downloads { get; } = new();

    public Task DownloadAsync(string filename, string content, CancellationToken cancellationToken = default)
    {
        Downloads.Add((filename, content));
        return Task.CompletedTask;
    }
}

/// <summary>Weather client that returns a fixed forecast: no network.</summary>
internal sealed class FakeWeatherClient : IWeatherClient
{
    public WeatherForecast Forecast { get; set; } = new(
        new CurrentConditions(70.6, 0),
        new[]
        {
            new DailyForecast(new DateOnly(2026, 11, 4), 71, 40, 3),
            new DailyForecast(new DateOnly(2026, 11, 5), 66, 49, 63),
            new DailyForecast(new DateOnly(2026, 11, 6), 66, 48, 3),
        });

    public Task<Result<WeatherForecast>> GetForecastAsync(double latitude, double longitude, int forecastDays, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<WeatherForecast>.Success(Forecast));
}

/// <summary>Place geocoder that resolves any name to Queensbury: no network.</summary>
internal sealed class FakePlaceGeocoder : IPlaceGeocoder
{
    public Task<Result<GeocodedPlace>> GeocodeAsync(string place, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<GeocodedPlace>.Success(new GeocodedPlace("Queensbury, New York, United States", 43.37729, -73.61317)));
}

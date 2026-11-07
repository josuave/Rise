using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Backup;
using Rise.Slices.Header;
using Rise.Slices.Layout;
using Rise.Slices.Links;
using Rise.Slices.Notes;
using Rise.Slices.OmarchyThemes;
using Rise.Slices.Search;
using Rise.Slices.Settings;
using Rise.Slices.Weather;
using Rise.Slices.Settings.Abstractions;
using Xunit;

namespace Rise.Slices.Tests;

public class SettingsGearTests : BunitContext
{
    private static readonly string[] WeatherWidgets = { "Weather" };

    private static readonly string[] CalmSettingsHeadings = { "Appearance", "Content", "System" };

    private static readonly string[] CalmSettingsCardTitles =
    {
        "Theme", "Home screen icon", "Background", "Links", "Search", "Clock", "Greeting",
        "Group", "Notes", "2048", "Weather", "Keyboard", "Backup", "Modules", "Preview features",
    };

    private readonly FakeThemeStore _theme = new();
    private readonly FakeLinkStore _store = new();
    private readonly FakeCustomThemeStore _customThemes = new();
    private readonly FakePreviewFeatureStore _previewFeatures = new();
    private readonly PreviewFeatureService _previewService;

    public SettingsGearTests()
    {
        _previewService = new PreviewFeatureService(_previewFeatures);
        Services.AddScoped<IThemeStore>(_ => _theme);
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<ICustomThemeStore>(_ => _customThemes);
        Services.AddScoped<IPreviewFeatureStore>(_ => _previewFeatures);
        Services.AddScoped<PreviewFeatureService>(_ => _previewService);
        Services.AddScoped<CustomThemeService>();
        Services.AddScoped<StartPageService>();
        Services.AddScoped<ISettingsSection, LinksSettingsSection>();
        Services.AddScoped<IModuleStore>(_ => new FakeModuleStore());
        Services.AddScoped<ModuleService>();
        Services.AddSingleton<IModuleCatalog>(new FakeModuleCatalog());
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider());
        Services.AddScoped<IOmarchyThemeCache>(_ => new FakeOmarchyThemeCache());
    }

    [Fact]
    public void GearButton_OpensDialog_WithThemeAndLinksSections()
    {
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();

        cut.Find(".calm-settings-gear").Click();

        Assert.NotNull(cut.Find(".calm-dialog"));
        Assert.Contains("Theme", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Tiles shown on the home page.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Google", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GearButton_OpensDialog_WithAllWidgetSections()
    {
        // Notes is a lazy module: its section renders through the lazy
        // section container in the browser, not from DI, so it is not
        // registered here. The browser harness covers the lazy path.
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore());
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<ClockConfig>();
        Services.AddScoped<GreetingConfig>();
        Services.AddScoped<GroupConfig>();
        Services.AddScoped<ISettingsSection, ClockSettingsSection>();
        Services.AddScoped<ISettingsSection, GreetingSettingsSection>();
        Services.AddScoped<ISettingsSection, GroupSettingsSection>();
        Services.AddScoped<IWeatherClient>(_ => new FakeWeatherClient());
        Services.AddScoped<IPlaceGeocoder>(_ => new FakePlaceGeocoder());
        Services.AddScoped<WeatherService>();
        Services.AddScoped<WeatherConfig>();
        Services.AddScoped<ISettingsSection, WeatherSettingsSection>();

        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();

        cut.Find(".calm-settings-gear").Click();

        Assert.Contains("Clock", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Greeting", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Group", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Weather", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ThemeOption_Dark_PersistsAndAppliesDark()
    {
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        IElement dark = cut.FindAll(".calm-theme-option")
            .Single(b => b.TextContent.Trim() == "Dark");
        dark.Click();

        Assert.Equal("dark", _theme.Stored);
        Assert.Equal("dark", _theme.Applied);
    }

    [Fact]
    public void ThemeOption_System_PersistsAndAppliesSystem()
    {
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        IElement system = cut.FindAll(".calm-theme-option")
            .Single(b => b.TextContent.Trim() == "System");
        system.Click();

        Assert.Equal("system", _theme.Stored);
        Assert.Equal("system", _theme.Applied);
    }

    [Fact]
    public void CustomTheme_NewTheme_CreatesAndOpensEditor()
    {
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "New theme").Click();

        Assert.NotNull(cut.Find(".calm-theme-editor"));
        Assert.Single(_customThemes.StoredThemes!);
        Assert.Equal("New theme", _customThemes.StoredThemes.Single().Name);
    }

    [Fact]
    public void CustomTheme_Apply_PersistsChoiceAndAppliesLightHalf()
    {
        _customThemes.StoredThemes = new[]
        {
            new CustomTheme("t1", "Work", ThemePalette.DefaultLight, ThemePalette.DefaultDark),
        };
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        cut.FindAll(".calm-theme-option").Single(b => b.TextContent.Trim() == "Work").Click();

        Assert.Equal("custom:t1", _theme.Stored);
        Assert.Equal(ThemePalette.DefaultLight, _theme.AppliedPalette);
    }

    [Fact]
    public void CustomTheme_DeleteActive_ResetsToSystem()
    {
        _customThemes.StoredThemes = new[]
        {
            new CustomTheme("t1", "Work", ThemePalette.DefaultLight, ThemePalette.DefaultDark),
        };
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();
        cut.FindAll(".calm-theme-option").Single(b => b.TextContent.Trim() == "Work").Click();
        Assert.Equal("custom:t1", _theme.Stored);

        cut.Find("[aria-label=\"Delete Work\"]").Click();

        Assert.Empty(_customThemes.StoredThemes!);
        Assert.Equal("system", _theme.Stored);
        Assert.Equal("system", _theme.Applied);
    }

    [Fact]
    public void DisabledModule_HidesItsSection()
    {
        var catalog = new FakeModuleCatalog
        {
            Modules = new[]
            {
                new ModuleInfo("Weather", "Weather", WeatherWidgets, "Rise.Slices.Weather", IsLazy: false, CanDisable: true),
            },
        };
        var moduleStore = new FakeModuleStore();
        moduleStore.Stored["Weather"] = false;
        Services.AddSingleton<IModuleCatalog>(catalog);
        Services.AddScoped<IModuleStore>(_ => moduleStore);
        Services.AddScoped<ModuleService>();
        Services.AddScoped<ISettingsSection, WeatherSettingsSection>();

        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        // The section is filtered before activation, so its service
        // dependencies are deliberately not registered here.
        cut.WaitForAssertion(() => Assert.DoesNotContain("Weather", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void CustomTheme_OrphanedChoice_RepairsToSystemOnOpen()
    {
        var orphanTheme = new FakeThemeStore("custom:ghost");
        Services.AddScoped<IThemeStore>(_ => orphanTheme);
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();

        cut.WaitForAssertion(() => Assert.Equal("system", orphanTheme.Stored));
        Assert.Equal("system", orphanTheme.Applied);
    }

    [Fact]
    public void PackTheme_Apply_PersistsChoiceAndAppliesPalette()
    {
        OmarchyTheme tokyo = OmarchyThemePack.All.Single(t => t.Id == "tokyo-night");
        OmarchyTheme latte = OmarchyThemePack.All.Single(t => t.Id == "catppuccin-latte");
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider
        {
            Themes = new[] { tokyo, latte },
        });
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        cut.Find("[aria-label=\"Use Tokyo Night theme\"]").Click();

        Assert.Equal("pack:omarchy:tokyo-night", _theme.Stored);
        Assert.Equal(tokyo.Palette, _theme.AppliedPalette);
    }

    [Fact]
    public void PackTheme_HiddenWhenModuleDisabled()
    {
        var catalog = new FakeModuleCatalog
        {
            Modules = new[]
            {
                new ModuleInfo("OmarchyThemes", "Omarchy themes", Array.Empty<string>(), "Rise.Slices.OmarchyThemes", IsLazy: false, CanDisable: true),
            },
        };
        var moduleStore = new FakeModuleStore();
        moduleStore.Stored["OmarchyThemes"] = false;
        Services.AddSingleton<IModuleCatalog>(catalog);
        Services.AddScoped<IModuleStore>(_ => moduleStore);
        Services.AddScoped<ModuleService>();
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider
        {
            Themes = OmarchyThemePack.All,
        });

        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Omarchy", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void PackTheme_OrphanedChoice_RepairsToSystemOnOpen()
    {
        var orphanTheme = new FakeThemeStore("pack:omarchy:ghost");
        Services.AddScoped<IThemeStore>(_ => orphanTheme);
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();

        cut.WaitForAssertion(() => Assert.Equal("system", orphanTheme.Stored));
        Assert.Equal("system", orphanTheme.Applied);
    }

    [Fact]
    public void PackTheme_CacheWrittenOnOpen()
    {
        var cache = new FakeOmarchyThemeCache();
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider
        {
            Themes = OmarchyThemePack.All,
        });
        Services.AddScoped<IOmarchyThemeCache>(_ => cache);

        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();

        cut.WaitForAssertion(() => Assert.Equal(23, cache.Written!.Count));
    }

    [Fact]
    public void FlagOff_DialogRendersFlatList()
    {
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".calm-dialog")));
        Assert.Empty(cut.FindAll(".calm-dialog-board"));
        Assert.Empty(cut.FindAll(".calm-dialog-board-card"));
        Assert.Empty(cut.FindAll(".calm-dialog-board-group"));
    }

    [Fact]
    public void FlagOn_DialogRendersBoardWithGroupedCards()
    {
        _previewFeatures.Stored[PreviewFeatures.CalmSettings] = true;
        RegisterAllSections();

        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".calm-dialog-board")));
        string[] headings = cut.FindAll(".calm-dialog-board-group")
            .Select(h => h.TextContent.Trim())
            .ToArray();
        Assert.Equal(CalmSettingsHeadings, headings);
        Assert.Equal(15, cut.FindAll(".calm-dialog-board-card").Count);
        // Cards keep the dialog's established Order ordering within
        // each group; the Theme card is first in Appearance.
        string[] cardTitles = cut.FindAll(".calm-dialog-board-card h3")
            .Select(h => h.TextContent.Trim())
            .ToArray();
        Assert.Equal(CalmSettingsCardTitles, cardTitles);
        Assert.Contains("Everything about your start page, in one place.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Lazy section body.", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToggleFlag_ReRendersDialogLive()
    {
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();
        await cut.Find(".calm-settings-gear").ClickAsync();
        await cut.WaitForAssertionAsync(() => Assert.NotNull(cut.Find(".calm-dialog")));
        Assert.Empty(cut.FindAll(".calm-dialog-board"));

        await _previewService.SetEnabledAsync(PreviewFeatures.CalmSettings, true);

        await cut.WaitForAssertionAsync(() => Assert.NotNull(cut.Find(".calm-dialog-board")));

        await _previewService.SetEnabledAsync(PreviewFeatures.CalmSettings, false);

        await cut.WaitForAssertionAsync(() => Assert.Empty(cut.FindAll(".calm-dialog-board")));
    }

    /// <summary>
    /// Registers every section the dialog can show, so the board test
    /// sees all fifteen cards. Slice sections are the real ones;
    /// Notes and 2048 are lazy modules, so they stand in fake lazy
    /// sections (the real container needs LazyAssemblyLoader, which
    /// bUnit has no use for); the shell-owned sections are internal
    /// to Rise.Wasm, so they stand in sections with the same title
    /// and order.
    /// </summary>
    private void RegisterAllSections()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore());
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<ClockConfig>();
        Services.AddScoped<GreetingConfig>();
        Services.AddScoped<GroupConfig>();
        Services.AddScoped<ISettingsSection, ClockSettingsSection>();
        Services.AddScoped<ISettingsSection, GreetingSettingsSection>();
        Services.AddScoped<ISettingsSection, GroupSettingsSection>();
        Services.AddScoped<IWeatherClient>(_ => new FakeWeatherClient());
        Services.AddScoped<IPlaceGeocoder>(_ => new FakePlaceGeocoder());
        Services.AddScoped<WeatherService>();
        Services.AddScoped<WeatherConfig>();
        Services.AddScoped<ISettingsSection, WeatherSettingsSection>();
        Services.AddScoped<SearchConfig>();
        Services.AddScoped<ISettingsSection, SearchSettingsSection>();
        Services.AddScoped<ILayoutCollectionStore>(_ => new FakeLayoutCollectionStore());
        Services.AddScoped<ITodoStore>(_ => new FakeTodoStore());
        Services.AddScoped<INoteStore>(_ => new FakeNoteStore());
        Services.AddScoped<IHotkeyStore>(_ => new FakeHotkeyStore());
        Services.AddScoped<IBackgroundStore>(_ => new FakeBackgroundStore());
        Services.AddScoped<LayoutService>();
        Services.AddScoped<TodoService>();
        Services.AddScoped<NotesService>();
        Services.AddScoped<BackupService>();
        Services.AddScoped<IFileDownloader>(_ => new FakeFileDownloader());
        Services.AddSingleton<IClock>(_ => new FakeClock(new DateTime(2026, 11, 2, 18, 0, 0)));
        Services.AddScoped<ISettingsSection, BackupSettingsSection>();
        Services.AddScoped<ISettingsSection>(_ => new FakeLazySection("Notes", 140, "List or post-it view.", "Rise.Slices.Notes.wasm", "Rise.Slices.Notes.NotesViewSettings"));
        Services.AddScoped<ISettingsSection>(_ => new FakeLazySection("2048", 145, "Best score, and a fresh board.", "Rise.Slices.Game2048.wasm", "Rise.Slices.Game2048.Game2048Settings"));
        Services.AddScoped<ISettingsSection>(_ => new FakeShellSection("Home screen icon", 90));
        Services.AddScoped<ISettingsSection>(_ => new FakeShellSection("Keyboard", 160));
        Services.AddScoped<ISettingsSection>(_ => new FakeShellSection("Background", 170));
        Services.AddScoped<ISettingsSection>(_ => new FakeShellSection("Modules", 210));
        Services.AddScoped<ISettingsSection>(_ => new FakeShellSection("Preview features", 215));
    }

    /// <summary>Stand-in for a shell-owned section (Keyboard, Background, Modules, Preview features): those sections are internal to Rise.Wasm, so slice tests stand in a section with the same title and order.</summary>
    private sealed class FakeShellSection : ISettingsSection
    {
        public FakeShellSection(string title, int order)
        {
            Title = title;
            Order = order;
        }

        public string Title { get; }

        public int Order { get; }

        public string Description => "Shell section stand-in.";

        public Type ComponentType => typeof(SectionStub);
    }

    /// <summary>Stand-in for a lazy section (Notes, 2048): same title, order, and description as the real one, but its component is a stub, so no assembly loads.</summary>
    private sealed class FakeLazySection : ILazySettingsSection
    {
        public FakeLazySection(string title, int order, string description, string assemblyFile, string componentTypeName)
        {
            Title = title;
            Order = order;
            Description = description;
            AssemblyFile = assemblyFile;
            ComponentTypeName = componentTypeName;
        }

        public string Title { get; }

        public int Order { get; }

        public string Description { get; }

        public Type ComponentType => typeof(SectionStub);

        public string AssemblyFile { get; }

        public string ComponentTypeName { get; }
    }

    /// <summary>Trivial section body for stand-in sections: renders its marker text, so the test can tell the lazy-parameters path rendered.</summary>
    private sealed class SectionStub : ComponentBase
    {
        [Parameter]
        public ILazySettingsSection? Section { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddContent(0, Section is null ? "Section body." : "Lazy section body.");
    }
}

using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Header;
using Rise.Slices.Layout;
using Rise.Slices.Links;
using Rise.Slices.Notes;
using Rise.Slices.Settings;
using Rise.Slices.Weather;
using Rise.Slices.Settings.Abstractions;
using Xunit;

namespace Rise.Slices.Tests;

public class SettingsGearTests : BunitContext
{
    private static readonly string[] WeatherWidgets = { "Weather" };

    private readonly FakeThemeStore _theme = new();
    private readonly FakeLinkStore _store = new();
    private readonly FakeCustomThemeStore _customThemes = new();

    public SettingsGearTests()
    {
        Services.AddScoped<IThemeStore>(_ => _theme);
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<ICustomThemeStore>(_ => _customThemes);
        Services.AddScoped<CustomThemeService>();
        Services.AddScoped<StartPageService>();
        Services.AddScoped<ISettingsSection, LinksSettingsSection>();
        Services.AddScoped<IModuleStore>(_ => new FakeModuleStore());
        Services.AddScoped<ModuleService>();
        Services.AddSingleton<IModuleCatalog>(new FakeModuleCatalog());
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
}

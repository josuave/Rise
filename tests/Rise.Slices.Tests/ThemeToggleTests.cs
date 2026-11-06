using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.OmarchyThemes;
using Rise.Slices.Settings;
using Xunit;

namespace Rise.Slices.Tests;

/// <summary>Tests for the Theme switcher widget, split out of SettingsGear in the widget phase.</summary>
public class ThemeToggleTests : BunitContext
{
    private readonly FakeThemeStore _theme = new();
    private readonly FakeCustomThemeStore _customThemes = new();

    public ThemeToggleTests()
    {
        Services.AddScoped<IThemeStore>(_ => _theme);
        Services.AddScoped<ICustomThemeStore>(_ => _customThemes);
        Services.AddScoped<CustomThemeService>();
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider());
        Services.AddScoped<IModuleStore>(_ => new FakeModuleStore());
        Services.AddScoped<ModuleService>();
        Services.AddSingleton<IModuleCatalog>(new FakeModuleCatalog());
    }

    [Fact]
    public void QuickToggle_FlipsLightToDark_AndBack()
    {
        IRenderedComponent<ThemeToggle> cut = Render<ThemeToggle>();

        // Default is System on a light device, so the toggle shows the moon (to dark).
        cut.Find(".calm-theme-toggle").Click();
        Assert.Equal("dark", _theme.Stored);

        // Now dark is showing, so the toggle shows the sun (to light).
        cut.Find(".calm-theme-toggle").Click();
        Assert.Equal("light", _theme.Stored);
    }

    [Fact]
    public void QuickToggle_FromSystemDark_ChoosesExplicitLight()
    {
        _theme.SystemPrefersDark = true;
        IRenderedComponent<ThemeToggle> cut = Render<ThemeToggle>();

        cut.Find(".calm-theme-toggle").Click();

        Assert.Equal("light", _theme.Stored);
    }

    [Fact]
    public void Toggle_WithActiveCustomTheme_AppliesDarkHalfOnDarkDevice()
    {
        var theme = new FakeThemeStore("custom:t1") { SystemPrefersDark = true };
        Services.AddScoped<IThemeStore>(_ => theme);
        _customThemes.StoredThemes = new[]
        {
            new CustomTheme("t1", "Work", ThemePalette.DefaultLight, ThemePalette.DefaultDark),
        };

        IRenderedComponent<ThemeToggle> cut = Render<ThemeToggle>();

        cut.WaitForAssertion(() => Assert.Equal(ThemePalette.DefaultDark, theme.AppliedPalette));
    }

    [Fact]
    public void Toggle_WithActivePackTheme_AppliesPaletteAndShowsSun()
    {
        OmarchyTheme tokyo = OmarchyThemePack.All.Single(t => t.Id == "tokyo-night");
        var theme = new FakeThemeStore("pack:omarchy:tokyo-night");
        Services.AddScoped<IThemeStore>(_ => theme);
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider
        {
            Themes = new[] { tokyo },
        });

        IRenderedComponent<ThemeToggle> cut = Render<ThemeToggle>();

        cut.WaitForAssertion(() => Assert.Equal(tokyo.Palette, theme.AppliedPalette));
        // A dark pack is showing, so the toggle shows the sun (to light).
        Assert.Equal("☀", cut.Find(".calm-theme-toggle").TextContent);
    }

    [Fact]
    public void Toggle_WithActiveLightPackTheme_AppliesPaletteAndShowsMoon()
    {
        OmarchyTheme latte = OmarchyThemePack.All.Single(t => t.Id == "catppuccin-latte");
        var theme = new FakeThemeStore("pack:omarchy:catppuccin-latte");
        Services.AddScoped<IThemeStore>(_ => theme);
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider
        {
            Themes = new[] { latte },
        });

        IRenderedComponent<ThemeToggle> cut = Render<ThemeToggle>();

        cut.WaitForAssertion(() => Assert.Equal(latte.Palette, theme.AppliedPalette));
        // A light pack is showing, so the toggle shows the moon (to dark).
        Assert.Equal("☾", cut.Find(".calm-theme-toggle").TextContent);
    }

    [Fact]
    public async Task Toggle_FollowsPackThemePickedElsewhere()
    {
        OmarchyTheme tokyo = OmarchyThemePack.All.Single(t => t.Id == "tokyo-night");
        Services.AddScoped<IOmarchyThemeProvider>(_ => new FakeOmarchyThemeProvider
        {
            Themes = new[] { tokyo },
        });
        IRenderedComponent<ThemeToggle> cut = Render<ThemeToggle>();

        // Light device, system theme: the toggle shows the moon.
        Assert.Equal("☾", cut.Find(".calm-theme-toggle").TextContent);

        // A pick made elsewhere (the Settings dialog) flows through the store's Changed event.
        await _theme.SetStoredThemeAsync("pack:omarchy:tokyo-night");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(tokyo.Palette, _theme.AppliedPalette);
            Assert.Equal("☀", cut.Find(".calm-theme-toggle").TextContent);
        });
    }
}

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
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
}

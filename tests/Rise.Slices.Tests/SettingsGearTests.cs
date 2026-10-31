using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Links;
using Rise.Slices.Settings;
using Rise.Slices.Settings.Abstractions;
using Xunit;

namespace Rise.Slices.Tests;

public class SettingsGearTests : BunitContext
{
    private readonly FakeThemeStore _theme = new();
    private readonly FakeLinkStore _store = new();

    public SettingsGearTests()
    {
        Services.AddScoped<IThemeStore>(_ => _theme);
        Services.AddScoped<ILinkStore>(_ => _store);
        Services.AddScoped<StartPageService>();
        Services.AddScoped<ISettingsSection, LinksSettingsSection>();
    }

    [Fact]
    public void GearButton_OpensDialog_WithThemeAndLinksSections()
    {
        var cut = Render<SettingsGear>();

        cut.Find(".calm-settings-gear").Click();

        Assert.NotNull(cut.Find(".calm-dialog"));
        Assert.Contains("Theme", cut.Markup);
        Assert.Contains("Tiles shown on the home page.", cut.Markup);
        Assert.Contains("Google", cut.Markup);
    }

    [Fact]
    public void ThemeOption_Dark_PersistsAndAppliesDark()
    {
        var cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        var dark = cut.FindAll(".calm-theme-option")
            .Single(b => b.TextContent.Trim() == "Dark");
        dark.Click();

        Assert.Equal("dark", _theme.Stored);
        Assert.Equal("dark", _theme.Applied);
    }

    [Fact]
    public void ThemeOption_System_PersistsAndAppliesSystem()
    {
        var cut = Render<SettingsGear>();
        cut.Find(".calm-settings-gear").Click();

        var system = cut.FindAll(".calm-theme-option")
            .Single(b => b.TextContent.Trim() == "System");
        system.Click();

        Assert.Equal("system", _theme.Stored);
        Assert.Equal("system", _theme.Applied);
    }

    [Fact]
    public void QuickToggle_FlipsLightToDark_AndBack()
    {
        var cut = Render<SettingsGear>();

        // Default is System on a light device, so the toggle shows the moon (to dark).
        cut.Find(".calm-theme-toggle").Click();
        Assert.Equal("dark", _theme.Stored);

        // Now dark is showing, so the toggle shows the sun (to light).
        cut.Find(".calm-theme-toggle").Click();
        Assert.Equal("light", _theme.Stored);
    }
}

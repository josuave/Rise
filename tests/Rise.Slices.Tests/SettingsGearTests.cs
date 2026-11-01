using AngleSharp.Dom;
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
        IRenderedComponent<SettingsGear> cut = Render<SettingsGear>();

        cut.Find(".calm-settings-gear").Click();

        Assert.NotNull(cut.Find(".calm-dialog"));
        Assert.Contains("Theme", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Tiles shown on the home page.", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Google", cut.Markup, StringComparison.Ordinal);
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
}

using Rise.Domain;
using Rise.Slices.OmarchyThemes;
using Xunit;

namespace Rise.Slices.Tests;

/// <summary>The pack itself: all 23 built-in Omarchy themes, complete palettes, and the choice format.</summary>
public class OmarchyThemePackTests
{
    private static readonly string[] LightThemeIds =
    {
        "catppuccin-latte", "flexoki-light", "lupine", "rose-pine", "white",
    };

    [Fact]
    public void Pack_HasAll23Themes() =>
        Assert.Equal(23, OmarchyThemePack.All.Count);

    [Fact]
    public void Pack_IdsAreUniqueSlugs()
    {
        Assert.Equal(OmarchyThemePack.All.Count, OmarchyThemePack.All.Select(t => t.Id).Distinct().Count());
        foreach (OmarchyTheme theme in OmarchyThemePack.All)
        {
            Assert.Matches("^[a-z0-9-]+$", theme.Id);
            Assert.False(string.IsNullOrWhiteSpace(theme.Title));
            Assert.False(string.IsNullOrWhiteSpace(theme.Accent));
        }
    }

    [Fact]
    public void Pack_ModesMatchOmarchy()
    {
        // Omarchy declares five light themes; the other eighteen are dark.
        string[] light = OmarchyThemePack.All.Where(t => !t.IsDark).Select(t => t.Id).OrderBy(id => id).ToArray();
        Assert.Equal(LightThemeIds, light);
    }

    [Fact]
    public void Pack_EveryPaletteIsComplete()
    {
        foreach (OmarchyTheme theme in OmarchyThemePack.All)
        {
            string[] values =
            {
                theme.Palette.Background, theme.Palette.Text, theme.Palette.Muted,
                theme.Palette.Faint, theme.Palette.Surface, theme.Palette.Border,
                theme.Palette.BorderSoft, theme.Palette.TileBackground, theme.Palette.Error,
            };
            foreach (string value in values)
            {
                Assert.Matches("^#[0-9a-f]{6}$", value);
            }
        }
    }

    [Fact]
    public void Pack_MappingSpotChecks()
    {
        OmarchyTheme tokyo = OmarchyThemePack.All.Single(t => t.Id == "tokyo-night");
        Assert.True(tokyo.IsDark);
        Assert.Equal("#1a1b26", tokyo.Palette.Background);
        Assert.Equal("#c0caf5", tokyo.Palette.Text);
        Assert.Equal("#414868", tokyo.Palette.Muted);
        Assert.Equal("#565f89", tokyo.Palette.Faint);
        Assert.Equal("#24283b", tokyo.Palette.Surface);
        Assert.Equal("#414868", tokyo.Palette.Border);
        Assert.Equal("#24283b", tokyo.Palette.BorderSoft);
        Assert.Equal("#292e42", tokyo.Palette.TileBackground);
        Assert.Equal("#f7768e", tokyo.Palette.Error);
        Assert.Equal("#7aa2f7", tokyo.Accent);

        OmarchyTheme latte = OmarchyThemePack.All.Single(t => t.Id == "catppuccin-latte");
        Assert.False(latte.IsDark);
        Assert.Equal("#eff1f5", latte.Palette.Background);
        Assert.Equal("#4c4f69", latte.Palette.Text);
    }

    [Fact]
    public void Choice_RoundTrips()
    {
        Assert.Equal("pack:omarchy:tokyo-night", OmarchyTheme.ChoiceFor("tokyo-night"));
        Assert.True(OmarchyTheme.TryParseChoice("pack:omarchy:tokyo-night", out string id));
        Assert.Equal("tokyo-night", id);
    }

    [Fact]
    public void Choice_RejectsNonPackChoices()
    {
        Assert.False(OmarchyTheme.TryParseChoice(null, out _));
        Assert.False(OmarchyTheme.TryParseChoice("light", out _));
        Assert.False(OmarchyTheme.TryParseChoice("custom:t1", out _));
        Assert.False(OmarchyTheme.TryParseChoice("pack:omarchy:", out _));
        Assert.False(OmarchyTheme.TryParseChoice("pack:other:tokyo-night", out _));
    }
}

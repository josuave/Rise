using Bunit;
using Microsoft.AspNetCore.Components;
using Rise.Domain;
using Rise.Slices.Settings;
using Xunit;

namespace Rise.Slices.Tests;

public class ThemeEditorTests : BunitContext
{
    private static CustomTheme WorkTheme() => new(
        "t1", "Work", ThemePalette.DefaultLight, ThemePalette.DefaultDark);

    [Fact]
    public void Save_ReturnsEditedTheme()
    {
        CustomTheme? saved = null;
        IRenderedComponent<ThemeEditor> cut = Render<ThemeEditor>(parameters => parameters
            .Add(p => p.Theme, WorkTheme())
            .Add(p => p.OnSave, EventCallback.Factory.Create<CustomTheme>(this, theme => saved = theme)));

        cut.Find("[aria-label=\"Theme name\"]").Change("Morning");
        cut.Find("[aria-label=\"Light Background\"]").Change("#112233");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Save").Click();

        Assert.NotNull(saved);
        Assert.Equal("Morning", saved.Name);
        Assert.Equal("#112233", saved.Light.Background);
        Assert.Equal(ThemePalette.DefaultDark, saved.Dark);
    }

    [Fact]
    public void Cancel_InvokesCancel_WithoutSaving()
    {
        bool cancelled = false;
        CustomTheme? saved = null;
        IRenderedComponent<ThemeEditor> cut = Render<ThemeEditor>(parameters => parameters
            .Add(p => p.Theme, WorkTheme())
            .Add(p => p.OnSave, EventCallback.Factory.Create<CustomTheme>(this, theme => saved = theme))
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelled = true)));

        cut.Find("[aria-label=\"Theme name\"]").Change("Morning");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        Assert.True(cancelled);
        Assert.Null(saved);
    }

    [Fact]
    public void Save_Disabled_WhenNameBlank()
    {
        IRenderedComponent<ThemeEditor> cut = Render<ThemeEditor>(parameters => parameters
            .Add(p => p.Theme, WorkTheme()));

        cut.Find("[aria-label=\"Theme name\"]").Change("   ");

        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Trim() == "Save").HasAttribute("disabled"));
    }
}

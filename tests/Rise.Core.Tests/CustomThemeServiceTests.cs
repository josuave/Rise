using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class CustomThemeServiceTests
{
    private static readonly string[] IdsAB = new[] { "a", "b" };

    private sealed class FakeCustomThemeStore : ICustomThemeStore
    {
        public IReadOnlyList<CustomTheme>? StoredThemes { get; private set; }

        public FakeCustomThemeStore(IReadOnlyList<CustomTheme>? initial = null) => StoredThemes = initial;

        public Task<IReadOnlyList<CustomTheme>?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(StoredThemes);

        public Task SaveAsync(IReadOnlyList<CustomTheme> themes, CancellationToken cancellationToken = default)
        {
            StoredThemes = themes;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeThemeStore : IThemeStore
    {
        public string? Stored { get; private set; }
        public string? Applied { get; private set; }
        public ThemePalette? AppliedPalette { get; private set; }
        public bool SystemPrefersDark { get; set; }

        public FakeThemeStore(string? stored = null) => Stored = stored;

        public Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default)
        {
            Stored = theme;
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

    private static CustomTheme Theme(string id, string name) => new(
        id, name, ThemePalette.DefaultLight, ThemePalette.DefaultDark);

    [Fact]
    public async Task Load_WithNothingStored_ReturnsEmpty()
    {
        var service = new CustomThemeService(new FakeCustomThemeStore(), new FakeThemeStore());

        Assert.Empty(await service.LoadAsync());
    }

    [Fact]
    public async Task Load_Sanitizes_BlankIdsDuplicatesAndBlankNames()
    {
        var store = new FakeCustomThemeStore(new[]
        {
            Theme("a", "First"),
            Theme("a", "Duplicate"),
            Theme("   ", "Blank id"),
            Theme("b", "   "),
        });
        var service = new CustomThemeService(store, new FakeThemeStore());

        IReadOnlyList<CustomTheme> loaded = await service.LoadAsync();

        Assert.Equal(IdsAB, loaded.Select(t => t.Id));
        Assert.Equal("First", loaded.Single(t => t.Id == "a").Name);
        Assert.Equal("Untitled", loaded.Single(t => t.Id == "b").Name);
    }

    [Fact]
    public async Task Create_CopiesBuiltInDefaults_AndUniquifiesNames_WithoutApplying()
    {
        var themeStore = new FakeThemeStore("dark");
        var service = new CustomThemeService(new FakeCustomThemeStore(), themeStore);

        CustomTheme first = await service.CreateAsync("New theme");
        CustomTheme second = await service.CreateAsync("New theme");

        Assert.Equal(ThemePalette.DefaultLight, first.Light);
        Assert.Equal(ThemePalette.DefaultDark, first.Dark);
        Assert.Equal("New theme", first.Name);
        Assert.Equal("New theme 2", second.Name);
        Assert.Equal("dark", themeStore.Stored);
        Assert.Null(themeStore.Applied);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        var store = new FakeCustomThemeStore();
        var service = new CustomThemeService(store, new FakeThemeStore());
        CustomTheme created = await service.CreateAsync("Work");

        CustomTheme updated = created with { Name = "Morning", Light = ThemePalette.DefaultLight with { Background = "#112233" } };
        IReadOnlyList<CustomTheme> themes = await service.UpdateAsync(updated);

        Assert.Equal("Morning", themes.Single(t => t.Id == created.Id).Name);
        Assert.Equal("#112233", themes.Single(t => t.Id == created.Id).Light.Background);
    }

    [Fact]
    public async Task Delete_ActiveTheme_ResetsChoiceToSystem()
    {
        var themeStore = new FakeThemeStore("custom:t1");
        var service = new CustomThemeService(new FakeCustomThemeStore(new[] { Theme("t1", "Work") }), themeStore);

        IReadOnlyList<CustomTheme> remaining = await service.DeleteAsync("t1");

        Assert.Empty(remaining);
        Assert.Equal("system", themeStore.Stored);
        Assert.Equal("system", themeStore.Applied);
    }

    [Fact]
    public async Task Delete_NonActiveTheme_LeavesChoiceAlone()
    {
        var themeStore = new FakeThemeStore("dark");
        var service = new CustomThemeService(new FakeCustomThemeStore(new[] { Theme("t1", "Work") }), themeStore);

        await service.DeleteAsync("t1");

        Assert.Equal("dark", themeStore.Stored);
        Assert.Null(themeStore.Applied);
    }

    [Fact]
    public async Task SetActive_PersistsCustomChoice()
    {
        var themeStore = new FakeThemeStore();
        var service = new CustomThemeService(new FakeCustomThemeStore(), themeStore);

        await service.SetActiveAsync("t1");

        Assert.Equal("custom:t1", themeStore.Stored);
    }

    [Fact]
    public async Task ResolveHalf_FollowsDevicePreference()
    {
        var themeStore = new FakeThemeStore();
        var service = new CustomThemeService(new FakeCustomThemeStore(), themeStore);
        CustomTheme theme = Theme("t1", "Work");

        Assert.Equal(ThemePalette.DefaultLight, await service.ResolveHalfAsync(theme));

        themeStore.SystemPrefersDark = true;
        Assert.Equal(ThemePalette.DefaultDark, await service.ResolveHalfAsync(theme));
    }

    [Fact]
    public void TryParseChoice_AcceptsOnlyWellFormedCustomChoices()
    {
        Assert.True(CustomThemeService.TryParseChoice("custom:abc", out string id));
        Assert.Equal("abc", id);
        Assert.False(CustomThemeService.TryParseChoice("dark", out _));
        Assert.False(CustomThemeService.TryParseChoice("custom:", out _));
        Assert.False(CustomThemeService.TryParseChoice(null, out _));
        Assert.Equal("custom:abc", CustomThemeService.ChoiceFor("abc"));
    }
}

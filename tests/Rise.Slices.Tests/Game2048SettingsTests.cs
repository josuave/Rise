using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Game2048;
using Xunit;

namespace Rise.Slices.Tests;

public class Game2048SettingsTests : BunitContext
{
    private readonly FakeGame2048Store _store = new(new Game2048State(new int[Game2048Board.CellCount], 30, 90));

    public Game2048SettingsTests()
    {
        Services.AddScoped<IGame2048Store>(_ => _store);
        Services.AddScoped<Game2048Service>(_ => new Game2048Service(_store, new Random(7)));
    }

    [Fact]
    public void ResetBest_FallsBackToCurrentScore_AndPersistsIt()
    {
        IRenderedComponent<Game2048Settings> cut = Render<Game2048Settings>();
        cut.WaitForAssertion(() => Assert.Contains("Best score 90", cut.Find(".calm-game2048-settings-best").TextContent, StringComparison.Ordinal));

        cut.Find(".calm-game2048-settings-reset").Click();

        cut.WaitForAssertion(() => Assert.Contains("Best score 30", cut.Find(".calm-game2048-settings-best").TextContent, StringComparison.Ordinal));
        Assert.NotNull(_store.StoredState);
        Assert.Equal(30, _store.StoredState.Best);
    }

    [Fact]
    public void NewGame_ResetsScore_AndKeepsBest()
    {
        IRenderedComponent<Game2048Settings> cut = Render<Game2048Settings>();
        cut.WaitForAssertion(() => Assert.Contains("Best score 90", cut.Find(".calm-game2048-settings-best").TextContent, StringComparison.Ordinal));

        cut.Find(".calm-game2048-settings-new").Click();

        cut.WaitForAssertion(() => Assert.Equal(0, _store.StoredState?.Score ?? -1));
        Assert.NotNull(_store.StoredState);
        Assert.Equal(90, _store.StoredState.Best);
        Assert.Equal(2, _store.StoredState.Cells.Count(value => value != 0));
    }
}

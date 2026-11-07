using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Game2048;
using Xunit;

namespace Rise.Slices.Tests;

public class Game2048WidgetTests : BunitContext
{
    private readonly FakeGame2048Store _store = new();

    public Game2048WidgetTests()
    {
        Services.AddScoped<IGame2048Store>(_ => _store);
        Services.AddScoped<Game2048Service>(_ => new Game2048Service(_store, new Random(7)));
    }

    private static readonly int[] FullBoardNoMoves =
    {
        2, 4, 2, 4,
        4, 2, 4, 2,
        2, 4, 2, 4,
        4, 2, 4, 2,
    };

    private static readonly TouchPoint[] SwipeLeftStart = { new() { ClientX = 200, ClientY = 100 } };

    private static readonly TouchPoint[] SwipeLeftEnd = { new() { ClientX = 100, ClientY = 100 } };

    private static readonly TouchPoint[] ShortSwipeStart = { new() { ClientX = 100, ClientY = 100 } };

    private static readonly TouchPoint[] ShortSwipeEnd = { new() { ClientX = 110, ClientY = 100 } };

    private static Game2048State StateWithRow(int row, int score, int best, params int[] values)
    {
        int[] cells = new int[Game2048Board.CellCount];
        for (int column = 0; column < Game2048Board.Size; column++)
        {
            cells[row * Game2048Board.Size + column] = values[column];
        }

        return new Game2048State(cells, score, best);
    }

    [Fact]
    public void FirstLoad_SeedsTwoTiles()
    {
        IRenderedComponent<Game2048Widget> cut = Render<Game2048Widget>();

        cut.WaitForAssertion(() => Assert.Contains("2048", cut.Markup, StringComparison.Ordinal));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".calm-game2048-cell:not(.calm-game2048-empty)").Count));
        Assert.NotNull(_store.StoredState);
        Assert.Equal(2, _store.StoredState.Cells.Count(value => value != 0));
    }

    [Fact]
    public void ArrowLeft_MergesTiles_UpdatesScore_AndPersists()
    {
        _store.SaveAsync(StateWithRow(0, 0, 0, 2, 2, 0, 0)).Wait();
        IRenderedComponent<Game2048Widget> cut = Render<Game2048Widget>();
        cut.WaitForAssertion(() => Assert.Contains("2048", cut.Markup, StringComparison.Ordinal));

        cut.Find(".calm-game2048-board").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        cut.WaitForAssertion(() => Assert.Contains("Score 4", cut.FindAll(".calm-game2048-score")[0].TextContent, StringComparison.Ordinal));
        Assert.NotNull(_store.StoredState);
        Assert.Equal(4, _store.StoredState.Score);
        Assert.Equal(4, _store.StoredState.Best);
        Assert.Contains(4, _store.StoredState.Cells);
    }

    [Fact]
    public void SwipeLeft_MergesTiles()
    {
        _store.SaveAsync(StateWithRow(0, 0, 0, 2, 2, 0, 0)).Wait();
        IRenderedComponent<Game2048Widget> cut = Render<Game2048Widget>();
        cut.WaitForAssertion(() => Assert.Contains("2048", cut.Markup, StringComparison.Ordinal));

        IElement board = cut.Find(".calm-game2048-board");
        board.TouchStart(new TouchEventArgs
        {
            ChangedTouches = SwipeLeftStart,
        });
        board.TouchEnd(new TouchEventArgs
        {
            ChangedTouches = SwipeLeftEnd,
        });

        cut.WaitForAssertion(() => Assert.Contains("Score 4", cut.FindAll(".calm-game2048-score")[0].TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void ShortSwipe_DoesNothing()
    {
        _store.SaveAsync(StateWithRow(0, 0, 0, 2, 2, 0, 0)).Wait();
        IRenderedComponent<Game2048Widget> cut = Render<Game2048Widget>();
        cut.WaitForAssertion(() => Assert.Contains("2048", cut.Markup, StringComparison.Ordinal));

        IElement board = cut.Find(".calm-game2048-board");
        board.TouchStart(new TouchEventArgs
        {
            ChangedTouches = ShortSwipeStart,
        });
        board.TouchEnd(new TouchEventArgs
        {
            ChangedTouches = ShortSwipeEnd,
        });

        Assert.Contains("Score 0", cut.FindAll(".calm-game2048-score")[0].TextContent, StringComparison.Ordinal);
        Assert.Equal(0, _store.StoredState?.Score ?? -1);
    }

    [Fact]
    public void GameOver_ShowsMessage_AfterBlockedMove()
    {
        _store.SaveAsync(new Game2048State(FullBoardNoMoves, 10, 10)).Wait();
        IRenderedComponent<Game2048Widget> cut = Render<Game2048Widget>();

        cut.WaitForAssertion(() => Assert.Contains("Game over", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void NewGameButton_RestartsBoard_WithZeroScore()
    {
        _store.SaveAsync(StateWithRow(0, 40, 40, 4, 8, 0, 0)).Wait();
        IRenderedComponent<Game2048Widget> cut = Render<Game2048Widget>();
        cut.WaitForAssertion(() => Assert.Contains("Score 40", cut.FindAll(".calm-game2048-score")[0].TextContent, StringComparison.Ordinal));

        cut.Find(".calm-game2048-new").Click();

        cut.WaitForAssertion(() => Assert.Contains("Score 0", cut.FindAll(".calm-game2048-score")[0].TextContent, StringComparison.Ordinal));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".calm-game2048-cell:not(.calm-game2048-empty)").Count));
        Assert.NotNull(_store.StoredState);
        Assert.Equal(40, _store.StoredState.Best);
    }
}

using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class Game2048ServiceTests
{
    private sealed class FakeGame2048Store : IGame2048Store
    {
        public Game2048State? Stored { get; private set; }

        public FakeGame2048Store(Game2048State? initial = null) => Stored = initial;

        public Task<Game2048State?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(Game2048State state, CancellationToken cancellationToken = default)
        {
            Stored = state;
            return Task.CompletedTask;
        }
    }

    private static readonly int[] FullBoardNoMoves =
    {
        2, 4, 2, 4,
        4, 2, 4, 2,
        2, 4, 2, 4,
        4, 2, 4, 2,
    };

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
    public async Task Load_ReturnsFresh_WhenStoreIsEmpty()
    {
        var service = new Game2048Service(new FakeGame2048Store(), new Random(7));

        Game2048State state = await service.LoadAsync();

        Assert.Equal(0, state.Score);
        Assert.Equal(0, state.Best);
        Assert.All(state.Cells, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task Move_MergesSpawnsAndPersists_WithScoreAndBest()
    {
        var store = new FakeGame2048Store();
        var service = new Game2048Service(store, new Random(7));
        Game2048State state = StateWithRow(0, 0, 0, 2, 2, 0, 0);

        Game2048State next = await service.MoveAsync(state, Game2048Direction.Left);

        Assert.Equal(4, next.Score);
        Assert.Equal(4, next.Best);
        Assert.Contains(4, next.Cells);
        // One merged tile plus one spawned tile.
        Assert.Equal(2, next.Cells.Count(value => value != 0));
        Assert.NotNull(store.Stored);
        Assert.Equal(next, store.Stored);

        // The persistence round-trip: loading returns the saved game.
        Game2048State reloaded = await service.LoadAsync();
        Assert.Equal(next, reloaded);
    }

    [Fact]
    public async Task Move_UnchangedBoard_DoesNotSave()
    {
        var store = new FakeGame2048Store();
        var service = new Game2048Service(store, new Random(7));
        Game2048State state = new(FullBoardNoMoves, 10, 10);

        Game2048State next = await service.MoveAsync(state, Game2048Direction.Left);

        Assert.Equal(state, next);
        Assert.Null(store.Stored);
    }

    [Fact]
    public async Task NewGame_SeedsTwoTiles_AndKeepsBest()
    {
        var store = new FakeGame2048Store(new Game2048State(new int[Game2048Board.CellCount], 100, 100));
        var service = new Game2048Service(store, new Random(7));

        Game2048State next = await service.NewGameAsync();

        Assert.Equal(0, next.Score);
        Assert.Equal(100, next.Best);
        Assert.Equal(2, next.Cells.Count(value => value != 0));
        Assert.NotNull(store.Stored);
        Assert.Equal(next, store.Stored);
    }

    [Fact]
    public async Task ResetBest_FallsBackToCurrentScore()
    {
        var store = new FakeGame2048Store(new Game2048State(new int[Game2048Board.CellCount], 30, 90));
        var service = new Game2048Service(store, new Random(7));

        Game2048State next = await service.ResetBestAsync();

        Assert.Equal(30, next.Score);
        Assert.Equal(30, next.Best);
        Assert.NotNull(store.Stored);
        Assert.Equal(30, store.Stored.Best);
    }
}

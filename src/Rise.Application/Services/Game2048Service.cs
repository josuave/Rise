using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for 2048: the only place that talks to the store.
/// It loads a game, hands it to the pure core (Game2048Board.Move and
/// friends), and persists whatever the core returns. It contains no
/// merge rules itself. The dice live here too — one <see cref="Random"/>
/// per service, seedable from tests (and <see cref="Random.Shared"/>
/// otherwise, so no insecure generator is ever constructed here) —
/// and a move that changes nothing neither spawns nor saves, so a
/// game over stays exactly as it was.
/// </summary>
public sealed class Game2048Service(IGame2048Store store, Random? random = null)
{
    /// <summary>
    /// Raised after any successful save, so the widget and its settings
    /// section can reload instead of going stale.
    /// </summary>
    public event EventHandler? Changed;

    private Random Random { get; } = random ?? Random.Shared;

    public async Task<Game2048State> LoadAsync(CancellationToken cancellationToken = default) =>
        await store.LoadAsync(cancellationToken) ?? Game2048State.Fresh;

    /// <summary>
    /// Slide in a direction: merge, spawn one tile, and persist — but
    /// only when the slide changed the board. An unchanged board
    /// returns the same state and saves nothing.
    /// </summary>
    public async Task<Game2048State> MoveAsync(Game2048State state, Game2048Direction direction, CancellationToken cancellationToken = default)
    {
        Game2048Move move = state.Board.Move(direction);
        if (!move.Moved)
        {
            return state;
        }

        Game2048Board board = move.Board.Spawn(Random);
        Game2048State next = state.With(board, state.Score + move.ScoreGained);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }

    /// <summary>A fresh board with two tiles, keeping Best.</summary>
    public async Task<Game2048State> NewGameAsync(CancellationToken cancellationToken = default)
    {
        Game2048State current = await LoadAsync(cancellationToken);
        Game2048State next = new(Game2048Board.NewGame(Random).Cells, 0, current.Best);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }

    /// <summary>Best falls back to the current score, never below it.</summary>
    public async Task<Game2048State> ResetBestAsync(CancellationToken cancellationToken = default)
    {
        Game2048State current = await LoadAsync(cancellationToken);
        Game2048State next = current.WithBest(current.Score);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

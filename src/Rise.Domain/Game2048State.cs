namespace Rise.Domain;

/// <summary>
/// A saved 2048 game: the board's cells in row-major order, the
/// current score, and the best score this browser has seen. Best is
/// the high-water mark of Score; the service, not this record, keeps
/// it there. Cells and Score are the live game; Best survives a new
/// game, and only an explicit reset lowers it.
/// </summary>
public sealed record Game2048State(IReadOnlyList<int> Cells, int Score, int Best)
{
    /// <summary>Nothing played yet: an empty board, no score, no best.</summary>
    public static Game2048State Fresh => new(new int[Game2048Board.CellCount], 0, 0);

    public Game2048Board Board => new(Cells);

    /// <summary>The state after a move: the new board, the new score, and Best raised if beaten.</summary>
    public Game2048State With(Game2048Board board, int score) => new(board.Cells, score, Math.Max(Best, score));

    /// <summary>The same game with Best set explicitly (a reset sets it to the current score, never below it).</summary>
    public Game2048State WithBest(int best) => new(Cells, Score, best);
}

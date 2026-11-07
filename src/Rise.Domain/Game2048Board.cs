using System.Diagnostics.CodeAnalysis;

namespace Rise.Domain;

/// <summary>
/// The 2048 board: pure functional core, like <see cref="NoteList"/>.
/// A board is sixteen cells in row-major order; 0 is empty, anything
/// else is a tile's value. Every operation takes a board and returns a
/// new board — nothing here touches storage, and nothing here rolls
/// dice except through the <see cref="Random"/> the caller passes in,
/// so tests can seed it. Sliding never spawns: the caller spawns after
/// a move that changed something, which keeps merge rules testable
/// without randomness.
/// </summary>
public sealed record Game2048Board(IReadOnlyList<int> Cells)
{
    public const int Size = 4;

    public const int CellCount = Size * Size;

    public static Game2048Board Empty() => new(new int[CellCount]);

    /// <summary>A fresh board: empty, with two tiles spawned.</summary>
    public static Game2048Board NewGame(Random random) => Empty().Spawn(random).Spawn(random);

    public int this[int row, int column] => Cells[row * Size + column];

    /// <summary>Indexes of the empty cells, in row-major order.</summary>
    public IReadOnlyList<int> EmptyIndexes() => Cells
        .Select((value, index) => (value, index))
        .Where(cell => cell.value == 0)
        .Select(cell => cell.index)
        .ToList();

    /// <summary>
    /// Whether any move can change the board: an empty cell to slide
    /// into, or an adjacent equal pair to merge.
    /// </summary>
    public bool CanMove
    {
        get
        {
            if (Cells.Any(value => value == 0))
            {
                return true;
            }

            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    int value = this[row, column];
                    if (column + 1 < Size && this[row, column + 1] == value
                        || row + 1 < Size && this[row + 1, column] == value)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>Slide the board in a direction, merging equal neighbours once each.</summary>
    public Game2048Move Move(Game2048Direction direction)
    {
        int[] next = Cells.ToArray();
        int gained = 0;
        bool moved = false;
        foreach (int[] line in Lines(direction))
        {
            int[] values = line.Select(index => next[index]).ToArray();
            (int[] merged, int lineGained) = MergeLine(values);
            gained += lineGained;
            for (int k = 0; k < Size; k++)
            {
                if (next[line[k]] != merged[k])
                {
                    moved = true;
                }

                next[line[k]] = merged[k];
            }
        }

        return new Game2048Move(new Game2048Board(next), gained, moved);
    }

    /// <summary>
    /// Spawn one tile (2 nine times in ten, 4 the tenth) on a random
    /// empty cell. A full board spawns nothing and returns unchanged.
    /// </summary>
    [SuppressMessage("Security", "CA5394", Justification = "Tile spawns are game randomness, not security-sensitive; the Random is caller-supplied so tests can seed it.")]
    public Game2048Board Spawn(Random random)
    {
        IReadOnlyList<int> empty = EmptyIndexes();
        if (empty.Count == 0)
        {
            return this;
        }

        int[] next = Cells.ToArray();
        next[empty[random.Next(empty.Count)]] = random.Next(10) == 0 ? 4 : 2;
        return new Game2048Board(next);
    }

    /// <summary>
    /// The four lines a slide runs along, in move order: for a left
    /// slide each row runs left to right, for a right slide right to
    /// left, and likewise down the columns for up and down.
    /// </summary>
    private static IEnumerable<int[]> Lines(Game2048Direction direction)
    {
        for (int i = 0; i < Size; i++)
        {
            yield return direction switch
            {
                Game2048Direction.Left => new[] { i * Size, i * Size + 1, i * Size + 2, i * Size + 3 },
                Game2048Direction.Right => new[] { i * Size + 3, i * Size + 2, i * Size + 1, i * Size },
                Game2048Direction.Up => new[] { i, i + Size, i + 2 * Size, i + 3 * Size },
                Game2048Direction.Down => new[] { i + 3 * Size, i + 2 * Size, i + Size, i },
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown 2048 direction."),
            };
        }
    }

    /// <summary>
    /// Slide one line (given in move order) toward its start, merging
    /// each equal adjacent pair once: [2, 2, 2, 2] becomes [4, 4, 0, 0],
    /// never [8, 0, 0, 0] — a tile born of a merge cannot merge again
    /// in the same move.
    /// </summary>
    private static (int[] Merged, int Gained) MergeLine(int[] values)
    {
        int[] compact = values.Where(value => value != 0).ToArray();
        int[] merged = new int[Size];
        int gained = 0;
        int write = 0;
        int i = 0;
        while (i < compact.Length)
        {
            if (i + 1 < compact.Length && compact[i] == compact[i + 1])
            {
                merged[write] = compact[i] * 2;
                gained += merged[write];
                i += 2;
            }
            else
            {
                merged[write] = compact[i];
                i += 1;
            }

            write++;
        }

        return (merged, gained);
    }
}

using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class Game2048BoardTests
{
    private static Game2048Board Board(params int[] cells) => new(cells);

    private static Game2048Board BoardWithRow(int row, params int[] values)
    {
        int[] cells = new int[Game2048Board.CellCount];
        for (int column = 0; column < Game2048Board.Size; column++)
        {
            cells[row * Game2048Board.Size + column] = values[column];
        }

        return new Game2048Board(cells);
    }

    [Fact]
    public void MoveLeft_MergesEqualPair_AndScoresIt()
    {
        Game2048Board board = BoardWithRow(0, 2, 2, 0, 0);

        Game2048Move move = board.Move(Game2048Direction.Left);

        Assert.True(move.Moved);
        Assert.Equal(4, move.ScoreGained);
        Assert.Equal(4, move.Board[0, 0]);
        Assert.Equal(0, move.Board[0, 1]);
    }

    [Fact]
    public void MoveLeft_DoesNotDoubleMerge()
    {
        Game2048Board board = BoardWithRow(0, 2, 2, 2, 2);

        Game2048Move move = board.Move(Game2048Direction.Left);

        Assert.Equal(8, move.ScoreGained);
        Assert.Equal(4, move.Board[0, 0]);
        Assert.Equal(4, move.Board[0, 1]);
        Assert.Equal(0, move.Board[0, 2]);
        Assert.Equal(0, move.Board[0, 3]);
    }

    [Fact]
    public void MoveLeft_SlidesWithoutMerge_AndScoresNothing()
    {
        Game2048Board board = BoardWithRow(0, 0, 2, 0, 4);

        Game2048Move move = board.Move(Game2048Direction.Left);

        Assert.True(move.Moved);
        Assert.Equal(0, move.ScoreGained);
        Assert.Equal(2, move.Board[0, 0]);
        Assert.Equal(4, move.Board[0, 1]);
    }

    [Fact]
    public void MoveLeft_UnchangedBoard_ReturnsNotMoved()
    {
        Game2048Board board = BoardWithRow(0, 2, 4, 8, 16);

        Game2048Move move = board.Move(Game2048Direction.Left);

        Assert.False(move.Moved);
        Assert.Equal(0, move.ScoreGained);
        // Records compare their cell list by reference, so compare
        // the cells themselves: an unchanged move keeps every cell.
        Assert.Equal(board.Cells, move.Board.Cells);
    }

    [Fact]
    public void MoveRight_MergesTowardRight()
    {
        Game2048Board board = BoardWithRow(0, 2, 2, 0, 0);

        Game2048Move move = board.Move(Game2048Direction.Right);

        Assert.True(move.Moved);
        Assert.Equal(4, move.ScoreGained);
        Assert.Equal(4, move.Board[0, 3]);
        Assert.Equal(0, move.Board[0, 0]);
    }

    [Fact]
    public void MoveUp_MergesColumnUpward()
    {
        Game2048Board board = Board(
            2, 0, 0, 0,
            2, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0);

        Game2048Move move = board.Move(Game2048Direction.Up);

        Assert.True(move.Moved);
        Assert.Equal(4, move.ScoreGained);
        Assert.Equal(4, move.Board[0, 0]);
        Assert.Equal(0, move.Board[1, 0]);
    }

    [Fact]
    public void MoveDown_MergesColumnDownward()
    {
        Game2048Board board = Board(
            2, 0, 0, 0,
            2, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0);

        Game2048Move move = board.Move(Game2048Direction.Down);

        Assert.True(move.Moved);
        Assert.Equal(4, move.ScoreGained);
        Assert.Equal(4, move.Board[3, 0]);
        Assert.Equal(0, move.Board[0, 0]);
    }

    [Fact]
    public void CanMove_FalseOnFullBoardWithNoMerges()
    {
        Game2048Board board = Board(
            2, 4, 2, 4,
            4, 2, 4, 2,
            2, 4, 2, 4,
            4, 2, 4, 2);

        Assert.False(board.CanMove);
    }

    [Fact]
    public void CanMove_TrueWithEmptyCell()
    {
        Game2048Board board = Board(
            2, 4, 2, 4,
            4, 2, 4, 2,
            2, 4, 2, 4,
            4, 2, 4, 0);

        Assert.True(board.CanMove);
    }

    [Fact]
    public void CanMove_TrueWithAdjacentPair()
    {
        Game2048Board board = Board(
            2, 2, 4, 8,
            4, 8, 16, 32,
            2, 4, 8, 16,
            4, 8, 16, 32);

        Assert.True(board.CanMove);
    }

    [Fact]
    public void Spawn_FillsExactlyOneEmptyCell_WithTwoOrFour()
    {
        Game2048Board board = Board(
            2, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0);

        Game2048Board spawned = board.Spawn(new Random(42));

        Assert.Equal(2, spawned.Cells.Count(value => value != 0));
        Assert.All(spawned.Cells.Where(value => value != 0), value => Assert.True(value is 2 or 4));
    }

    [Fact]
    public void Spawn_OnFullBoard_ReturnsSameBoard()
    {
        Game2048Board board = Board(
            2, 4, 2, 4,
            4, 2, 4, 2,
            2, 4, 2, 4,
            4, 2, 4, 2);

        Assert.Equal(board, board.Spawn(new Random(42)));
    }

    [Fact]
    public void NewGame_HasExactlyTwoTiles()
    {
        var board = Game2048Board.NewGame(new Random(7));

        Assert.Equal(2, board.Cells.Count(value => value != 0));
        Assert.All(board.Cells.Where(value => value != 0), value => Assert.True(value is 2 or 4));
    }
}

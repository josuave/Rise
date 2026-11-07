namespace Rise.Domain;

/// <summary>
/// The result of sliding a 2048 board: the board after the slide,
/// the points the merges scored, and whether anything moved at all.
/// A move that changes nothing scores nothing and spawns no tile.
/// </summary>
public sealed record Game2048Move(Game2048Board Board, int ScoreGained, bool Moved);

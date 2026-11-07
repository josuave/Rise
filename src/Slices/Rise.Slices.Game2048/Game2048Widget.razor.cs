using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Game2048;

/// <summary>
/// The 2048 widget: a 4x4 board played with arrow keys or swipe.
/// It reloads whenever the game service saves, so a new game started
/// from Settings shows here too. Arrow keys are free — the shell's
/// hotkey system owns E, T, and Ctrl+K, and nothing here rebinds them.
/// </summary>
public partial class Game2048Widget : IDisposable
{
    [Inject]
    private Game2048Service GameService { get; set; } = null!;

    private Game2048State State { get; set; } = Game2048State.Fresh;

    /// <summary>Whether the 2048 banner was dismissed for this session's game.</summary>
    private bool WinDismissed { get; set; }

    private bool ShowWin => !WinDismissed && State.Board.Cells.Any(value => value >= 2048);

    private bool IsGameOver => !State.Board.CanMove;

    private (double X, double Y)? TouchStartPoint { get; set; }

    private const double SwipeThreshold = 24;

    protected override async Task OnInitializedAsync()
    {
        State = await GameService.LoadAsync();
        if (State.Board.Cells.All(value => value == 0))
        {
            State = await GameService.NewGameAsync();
        }

        GameService.Changed += OnGameChanged;
    }

    private async void OnGameChanged(object? sender, EventArgs args)
    {
        State = await GameService.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        Game2048Direction? direction = args.Key switch
        {
            "ArrowLeft" => Game2048Direction.Left,
            "ArrowRight" => Game2048Direction.Right,
            "ArrowUp" => Game2048Direction.Up,
            "ArrowDown" => Game2048Direction.Down,
            _ => null,
        };
        if (direction is null)
        {
            return;
        }

        await MoveAsync(direction.Value);
    }

    private void OnTouchStart(TouchEventArgs args)
    {
        TouchPoint? point = args.ChangedTouches.FirstOrDefault();
        TouchStartPoint = point is null ? null : (point.ClientX, point.ClientY);
    }

    private async Task OnTouchEnd(TouchEventArgs args)
    {
        if (TouchStartPoint is not (double startX, double startY))
        {
            return;
        }

        TouchStartPoint = null;
        TouchPoint? point = args.ChangedTouches.FirstOrDefault();
        if (point is null)
        {
            return;
        }

        double deltaX = point.ClientX - startX;
        double deltaY = point.ClientY - startY;
        if (Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)) < SwipeThreshold)
        {
            return;
        }

        await MoveAsync(SwipeDirection(deltaX, deltaY));
    }

    /// <summary>The slide direction of a swipe: the dominant axis wins.</summary>
    private static Game2048Direction SwipeDirection(double deltaX, double deltaY)
    {
        if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            return deltaX < 0 ? Game2048Direction.Left : Game2048Direction.Right;
        }

        return deltaY < 0 ? Game2048Direction.Up : Game2048Direction.Down;
    }

    private async Task MoveAsync(Game2048Direction direction)
    {
        if (IsGameOver)
        {
            return;
        }

        State = await GameService.MoveAsync(State, direction);
    }

    private async Task NewGameAsync()
    {
        WinDismissed = false;
        State = await GameService.NewGameAsync();
    }

    private void DismissWin() => WinDismissed = true;

    /// <summary>
    /// A tile's class: the warm-paper scale by value, capped at the
    /// 2048 style — a 4096 tile reads as a 2048 tile rather than
    /// falling back to an empty cell.
    /// </summary>
    private static string TileClass(int value) => value == 0
        ? "calm-game2048-cell calm-game2048-empty"
        : $"calm-game2048-cell calm-game2048-tile-{Math.Min(value, 2048)}";

    private static string TileText(int value) => value == 0 ? string.Empty : value.ToString(CultureInfo.InvariantCulture);

    private static string TileLabel(int value) => value == 0 ? "Empty cell" : $"Tile {value}";

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            GameService.Changed -= OnGameChanged;
        }
    }
}

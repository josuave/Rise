using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Game2048;

/// <summary>
/// The 2048 widget: a 4x4 board played with arrow keys or swipe.
/// It reloads whenever the game service saves, so a new game started
/// from Settings shows here too. Arrow keys are free — the shell's
/// hotkey system owns E, T, and Ctrl+K, and nothing here rebinds them.
/// The Full screen button toggles the widget root (scores + board)
/// through the shell's window.riseFullscreen helper; the
/// fullscreenchange listener reports back so the label stays right
/// when the user exits with Escape. iOS Safari has no
/// requestFullscreen on arbitrary elements, so the button stays
/// hidden there.
/// </summary>
public partial class Game2048Widget : IDisposable
{
    [Inject]
    private Game2048Service GameService { get; set; } = null!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    /// <summary>
    /// The sync runtime, for Dispose only: Dispose is synchronous, so
    /// the async runtime can't be awaited there. Same shape as the
    /// shell's Index page unregisters its hotkey listener.
    /// </summary>
    [Inject]
    private IJSInProcessRuntime JSInProcess { get; set; } = null!;

    private Game2048State State { get; set; } = Game2048State.Fresh;

    /// <summary>Whether the 2048 banner was dismissed for this session's game.</summary>
    private bool WinDismissed { get; set; }

    private bool ShowWin => !WinDismissed && State.Board.Cells.Any(value => value >= 2048);

    private bool IsGameOver => !State.Board.CanMove;

    /// <summary>Whether the Fullscreen API exists: false on iOS Safari, where the button stays hidden.</summary>
    private bool FullscreenSupported { get; set; }

    /// <summary>Whether the widget root is fullscreen right now.</summary>
    private bool IsFullscreen { get; set; }

    private string FullscreenLabel => IsFullscreen ? "Exit full screen" : "Full screen";

    /// <summary>The widget root: the fullscreen target, so scores + board go fullscreen together.</summary>
    private ElementReference RootElement { get; set; }

    private DotNetObjectReference<Game2048Widget>? FullscreenRef { get; set; }

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

        try
        {
            FullscreenSupported = await JSRuntime.InvokeAsync<bool>("riseFullscreen.supported", CancellationToken.None);
            if (FullscreenSupported)
            {
                FullscreenRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("riseFullscreen.register", CancellationToken.None, FullscreenRef);
            }
        }
        catch (JSException)
        {
            // No JS (or no Fullscreen API): the button stays hidden, the game plays as a card.
            FullscreenSupported = false;
        }
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

    private async Task ToggleFullscreenAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("riseFullscreen.toggle", CancellationToken.None, RootElement);
        }
        catch (JSException)
        {
            // Leaving fullscreen to the browser: the game keeps working as a card.
        }
    }

    /// <summary>
    /// Called by the fullscreenchange listener so the button label
    /// stays right when the user exits with Escape or the browser
    /// chrome instead of the button.
    /// </summary>
    [JSInvokable]
    public Task OnFullscreenChanged(bool isFullscreen)
    {
        IsFullscreen = isFullscreen;
        return InvokeAsync(StateHasChanged);
    }

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
            if (FullscreenRef is not null)
            {
                try
                {
                    JSInProcess.InvokeVoid("riseFullscreen.unregister");
                }
                catch (JSException)
                {
                    // The page is going away; a stale listener is harmless.
                }

                FullscreenRef.Dispose();
            }
        }
    }
}

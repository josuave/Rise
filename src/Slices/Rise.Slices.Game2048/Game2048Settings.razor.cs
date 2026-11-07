using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Game2048;

/// <summary>
/// The 2048 slice's Settings section content: the best score, a new
/// game, and a best-score reset, all saved immediately. 2048 is a lazy
/// module, so this reads through the game service rather than a typed
/// config class, which cannot be registered at startup; the widget
/// reloads on the same Changed event, so a new game from here shows
/// on the page too.
/// </summary>
public partial class Game2048Settings : IDisposable
{
    [Inject]
    private Game2048Service GameService { get; set; } = null!;

    private Game2048State State { get; set; } = Game2048State.Fresh;

    protected override async Task OnInitializedAsync()
    {
        State = await GameService.LoadAsync();
        GameService.Changed += OnGameChanged;
    }

    private async void OnGameChanged(object? sender, EventArgs args)
    {
        State = await GameService.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task NewGameAsync() => State = await GameService.NewGameAsync();

    private async Task ResetBestAsync() => State = await GameService.ResetBestAsync();

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

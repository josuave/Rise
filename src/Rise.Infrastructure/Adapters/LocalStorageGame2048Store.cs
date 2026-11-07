using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>Outbound adapter: persists a 2048 game in the browser's localStorage.</summary>
public sealed class LocalStorageGame2048Store(IJSRuntime js) : IGame2048Store
{
    private const string StorageKey = "rise.game2048.v1";

    // Stored shape: flat cells plus scores, so the domain types never leak into the JSON.
    private sealed record StoredGame(int[] Cells, int Score, int Best);

    public async Task<Game2048State?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            StoredGame? parsed = JsonSerializer.Deserialize<StoredGame>(stored);
            if (parsed is null || parsed.Cells.Length != Game2048Board.CellCount
                || parsed.Cells.Any(value => value < 0)
                || parsed.Score < 0 || parsed.Best < 0)
            {
                return null;
            }

            return new Game2048State(parsed.Cells, parsed.Score, parsed.Best);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Corrupt or unavailable storage: the caller falls back to a fresh game rather than breaking the page.
            return null;
        }
    }

    public async Task SaveAsync(Game2048State state, CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = new StoredGame(state.Cells.ToArray(), state.Score, state.Best);
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>Outbound adapter: persists a to-do list in the browser's localStorage.</summary>
public sealed class LocalStorageTodoStore(IJSRuntime js) : ITodoStore
{
    private const string StorageKey = "rise.todos.v1";

    // Stored shape: flat records, so the domain types never leak into the JSON.
    private sealed record StoredTodo(Guid Id, string Text, bool Done);

    public async Task<TodoList?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            List<StoredTodo>? parsed = JsonSerializer.Deserialize<List<StoredTodo>>(stored);
            if (parsed is null)
            {
                return null;
            }

            var items = new List<TodoItem>();
            foreach (StoredTodo item in parsed)
            {
                if (string.IsNullOrWhiteSpace(item.Text))
                {
                    continue;
                }

                items.Add(new TodoItem(item.Id == Guid.Empty ? Guid.NewGuid() : item.Id, item.Text.Trim(), item.Done));
            }

            return new TodoList(items);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Corrupt or unavailable storage: the caller falls back to an empty list rather than breaking the page.
            return null;
        }
    }

    public async Task SaveAsync(TodoList list, CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = list.Items.Select(item => new StoredTodo(item.Id, item.Text, item.Done)).ToList();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}

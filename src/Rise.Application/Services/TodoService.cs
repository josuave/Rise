using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for todos: the only place that talks to the store.
/// It loads a list, hands it to the pure core (TodoList.AddTodo and
/// friends), and persists whatever the core returns. It contains no
/// to-do rules itself.
/// </summary>
public sealed class TodoService(ITodoStore store)
{
    /// <summary>
    /// Raised after any successful save, so widgets that render the
    /// same list can reload instead of going stale.
    /// </summary>
    public event EventHandler? Changed;

    public async Task<TodoList> LoadAsync(CancellationToken cancellationToken = default) =>
        await store.LoadAsync(cancellationToken) ?? TodoList.Empty();

    public async Task<Result<TodoList>> AddTodoAsync(TodoList list, string text, CancellationToken cancellationToken = default)
    {
        Result<TodoList> result = list.AddTodo(text);
        switch (result)
        {
            case Ok<TodoList> ok:
                await store.SaveAsync(ok.Value, cancellationToken);
                Changed?.Invoke(this, EventArgs.Empty);
                break;
            case Error:
            case null:
                break;
        }

        return result;
    }

    public async Task<TodoList> ToggleTodoAsync(TodoList list, Guid id, CancellationToken cancellationToken = default)
    {
        TodoList next = list.ToggleTodo(id);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }

    public async Task<TodoList> DeleteTodoAsync(TodoList list, Guid id, CancellationToken cancellationToken = default)
    {
        TodoList next = list.DeleteTodo(id);
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }

    public async Task<TodoList> ClearCompletedAsync(TodoList list, CancellationToken cancellationToken = default)
    {
        TodoList next = list.ClearCompleted();
        await store.SaveAsync(next, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return next;
    }
}

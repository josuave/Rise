namespace Rise.Domain;

/// <summary>
/// The to-do list: pure functional core. Every operation takes a list
/// and returns a new list (or a failure) — it never touches storage.
/// The imperative shell (TodoService) is what persists the returned list.
/// </summary>
public sealed record TodoList(IReadOnlyList<TodoItem> Items)
{
    public static TodoList Empty() => new([]);

    public Result<TodoList> AddTodo(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<TodoList>.Failure("Write something to do first.");
        }

        return Result<TodoList>.Success(new TodoList([..Items, TodoItem.Create(text)]));
    }

    public TodoList ToggleTodo(Guid id) =>
        new(Items.Select(item => item.Id == id ? item.Toggle() : item).ToList());

    public TodoList DeleteTodo(Guid id) =>
        new(Items.Where(item => item.Id != id).ToList());

    public TodoList ClearCompleted() =>
        new(Items.Where(item => !item.Done).ToList());
}

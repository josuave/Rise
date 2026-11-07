using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class TodoServiceTests
{
    private sealed class FakeTodoStore : ITodoStore
    {
        public TodoList? Stored { get; private set; }

        public FakeTodoStore(TodoList? initial = null) => Stored = initial;

        public Task<TodoList?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(TodoList list, CancellationToken cancellationToken = default)
        {
            Stored = list;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Load_ReturnsEmpty_WhenStoreIsEmpty()
    {
        var service = new TodoService(new FakeTodoStore());

        TodoList list = await service.LoadAsync();

        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task AddTodo_SavesAndReturnsItem()
    {
        var store = new FakeTodoStore();
        var service = new TodoService(store);

        Result<TodoList> result = await service.AddTodoAsync(TodoList.Empty(), "Buy milk");

        TodoList added = ResultAssertions.RequireOk(result);
        Assert.Single(added.Items);
        Assert.Equal("Buy milk", added.Items[0].Text);
        Assert.False(added.Items[0].Done);
        Assert.NotNull(store.Stored);
        Assert.Single(store.Stored.Items);
    }

    [Fact]
    public async Task AddTodo_BlankText_FailsWithoutSaving()
    {
        var store = new FakeTodoStore();
        var service = new TodoService(store);

        Result<TodoList> result = await service.AddTodoAsync(TodoList.Empty(), "   ");

        Assert.NotEmpty(ResultAssertions.RequireError(result));
        Assert.Null(store.Stored);
    }

    [Fact]
    public async Task ToggleTodo_FlipsDone_AndPersists()
    {
        var store = new FakeTodoStore();
        var service = new TodoService(store);
        Result<TodoList> added = await service.AddTodoAsync(TodoList.Empty(), "Buy milk");
        TodoList addedList = ResultAssertions.RequireOk(added);

        TodoList toggled = await service.ToggleTodoAsync(addedList, addedList.Items[0].Id);

        Assert.True(toggled.Items[0].Done);
        Assert.NotNull(store.Stored);
        Assert.True(store.Stored.Items[0].Done);
    }

    [Fact]
    public async Task DeleteTodo_RemovesOnlyThatItem()
    {
        var store = new FakeTodoStore();
        var service = new TodoService(store);
        Result<TodoList> first = await service.AddTodoAsync(TodoList.Empty(), "Buy milk");
        TodoList firstList = ResultAssertions.RequireOk(first);
        Result<TodoList> second = await service.AddTodoAsync(firstList, "Walk dog");
        TodoList secondList = ResultAssertions.RequireOk(second);

        TodoList deleted = await service.DeleteTodoAsync(secondList, secondList.Items[0].Id);

        Assert.Single(deleted.Items);
        Assert.Equal("Walk dog", deleted.Items[0].Text);
        Assert.NotNull(store.Stored);
        Assert.Single(store.Stored.Items);
    }

    [Fact]
    public async Task ClearCompleted_KeepsOpenItems()
    {
        var store = new FakeTodoStore();
        var service = new TodoService(store);
        Result<TodoList> first = await service.AddTodoAsync(TodoList.Empty(), "Buy milk");
        TodoList firstList = ResultAssertions.RequireOk(first);
        Result<TodoList> second = await service.AddTodoAsync(firstList, "Walk dog");
        TodoList secondList = ResultAssertions.RequireOk(second);
        TodoList toggled = await service.ToggleTodoAsync(secondList, secondList.Items[0].Id);

        TodoList cleared = await service.ClearCompletedAsync(toggled);

        Assert.Single(cleared.Items);
        Assert.Equal("Walk dog", cleared.Items[0].Text);
        Assert.NotNull(store.Stored);
        Assert.Single(store.Stored.Items);
    }
}

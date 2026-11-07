using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Todo;
using Xunit;

namespace Rise.Slices.Tests;

public class TodoWidgetTests : BunitContext
{
    private readonly FakeTodoStore _store = new();

    private readonly FakePreviewFeatureStore _previewStore = new();

    public TodoWidgetTests()
    {
        Services.AddScoped<ITodoStore>(_ => _store);
        Services.AddScoped<TodoService>();
        // The widget injects the preview service (nullable): a
        // nullable [Inject] still throws at activation when
        // unregistered, so every test registers it.
        Services.AddScoped<IPreviewFeatureStore>(_ => _previewStore);
        Services.AddScoped<PreviewFeatureService>();
    }

    [Fact]
    public void FullBoardFlag_On_ShowsTodayProgressLine()
    {
        var seeded = new FakeTodoStore(new TodoList(new[]
        {
            new TodoItem(Guid.NewGuid(), "Buy milk", true),
            new TodoItem(Guid.NewGuid(), "Walk dog", false),
        }));
        Services.AddScoped<ITodoStore>(_ => seeded);
        _previewStore.Stored[PreviewFeatures.CalmBoardFull] = true;

        IRenderedComponent<TodoWidget> cut = Render<TodoWidget>();

        cut.WaitForAssertion(() => Assert.Contains("Today · 1 of 2 done", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void FullBoardFlag_Off_HidesTodayProgressLine()
    {
        var seeded = new FakeTodoStore(new TodoList(new[]
        {
            new TodoItem(Guid.NewGuid(), "Buy milk", true),
            new TodoItem(Guid.NewGuid(), "Walk dog", false),
        }));
        Services.AddScoped<ITodoStore>(_ => seeded);

        IRenderedComponent<TodoWidget> cut = Render<TodoWidget>();

        // The items prove init ran; the line stays off without the flag.
        cut.WaitForAssertion(() => Assert.Contains("Walk dog", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("Today ·", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AddTodo_ShowsInList_AndPersistsIt()
    {
        IRenderedComponent<TodoWidget> cut = Render<TodoWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Todo", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"New todo\"]").Change("Buy milk");
        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Buy milk", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(_store.StoredList);
        Assert.Single(_store.StoredList.Items);
        Assert.Equal("Buy milk", _store.StoredList.Items[0].Text);
    }

    [Fact]
    public void AddBlank_ShowsError_AndDoesNotPersist()
    {
        IRenderedComponent<TodoWidget> cut = Render<TodoWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Todo", cut.Markup, StringComparison.Ordinal));

        cut.Find(".calm-add-form button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Write something to do first.", cut.Markup, StringComparison.Ordinal));
        Assert.Null(_store.StoredList);
    }

    [Fact]
    public void ToggleTodo_MarksDone_AndPersists()
    {
        var seeded = new FakeTodoStore(new TodoList(new[]
        {
            new TodoItem(Guid.NewGuid(), "Buy milk", false),
        }));
        Services.AddScoped<ITodoStore>(_ => seeded);

        IRenderedComponent<TodoWidget> cut = Render<TodoWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Buy milk", cut.Markup, StringComparison.Ordinal));

        cut.Find("input[type=\"checkbox\"]").Change(true);

        cut.WaitForAssertion(() => Assert.Contains("Clear completed", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(seeded.StoredList);
        Assert.True(seeded.StoredList.Items[0].Done);
    }

    [Fact]
    public void DeleteTodo_RemovesRow_AndPersists()
    {
        var seeded = new FakeTodoStore(new TodoList(new[]
        {
            new TodoItem(Guid.NewGuid(), "Buy milk", false),
            new TodoItem(Guid.NewGuid(), "Walk dog", false),
        }));
        Services.AddScoped<ITodoStore>(_ => seeded);

        IRenderedComponent<TodoWidget> cut = Render<TodoWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Walk dog", cut.Markup, StringComparison.Ordinal));

        cut.Find("[aria-label=\"Delete Buy milk\"]").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Buy milk", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Walk dog", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(seeded.StoredList);
        Assert.Single(seeded.StoredList.Items);
        Assert.Equal("Walk dog", seeded.StoredList.Items[0].Text);
    }

    [Fact]
    public void ClearCompleted_RemovesDoneItems_Only()
    {
        var seeded = new FakeTodoStore(new TodoList(new[]
        {
            new TodoItem(Guid.NewGuid(), "Buy milk", true),
            new TodoItem(Guid.NewGuid(), "Walk dog", false),
        }));
        Services.AddScoped<ITodoStore>(_ => seeded);

        IRenderedComponent<TodoWidget> cut = Render<TodoWidget>();
        cut.WaitForAssertion(() => Assert.Contains("Clear completed", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clear completed").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Buy milk", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("Walk dog", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(seeded.StoredList);
        Assert.Single(seeded.StoredList.Items);
        Assert.Equal("Walk dog", seeded.StoredList.Items[0].Text);
    }
}

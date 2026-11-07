using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Todo;

/// <summary>
/// The to-do widget: a minimal list (add, toggle, delete, clear
/// completed). It shares its list with no other widget, but reloads
/// whenever the to-do service saves, so it never goes stale.
/// </summary>
public partial class TodoWidget : IDisposable
{
    [Inject]
    private TodoService TodoService { get; set; } = null!;

    /// <summary>
    /// Null when the preview service isn't registered: the Today line
    /// stays off then. A nullable [Inject] still throws at activation
    /// when unregistered, so tests must register the service.
    /// </summary>
    [Inject]
    private PreviewFeatureService? PreviewFeatureService { get; set; }

    private TodoList List { get; set; } = TodoList.Empty();

    /// <summary>Whether the full-board flag is on; the Today line shows only then.</summary>
    private bool CalmBoardFull { get; set; }

    private int DoneCount => List.Items.Count(item => item.Done);

    private int TotalCount => List.Items.Count;

    private string NewText { get; set; } = string.Empty;

    private string? FormError { get; set; }

    protected override async Task OnInitializedAsync()
    {
        List = await TodoService.LoadAsync();
        await RefreshBoardFlagAsync();
        PreviewFeatureService?.Changed += OnPreviewFeaturesChanged;
        TodoService.Changed += OnListChanged;
    }

    private async void OnPreviewFeaturesChanged(object? sender, EventArgs args)
    {
        await RefreshBoardFlagAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshBoardFlagAsync() =>
        CalmBoardFull = PreviewFeatureService is not null
            && await PreviewFeatureService.IsEnabledAsync(PreviewFeatures.CalmBoardFull);

    private async void OnListChanged(object? sender, EventArgs args)
    {
        List = await TodoService.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddAsync()
    {
        Result<TodoList> result = await TodoService.AddTodoAsync(List, NewText);
        switch (result)
        {
            case Ok<TodoList> ok:
                List = ok.Value;
                NewText = string.Empty;
                FormError = null;
                break;
            case Error error:
                FormError = error.Message;
                break;
            case null:
                FormError = null;
                break;
        }
    }

    private async Task ToggleAsync(TodoItem item, ChangeEventArgs args)
    {
        if (args.Value is not bool done || done == item.Done)
        {
            return;
        }

        List = await TodoService.ToggleTodoAsync(List, item.Id);
    }

    private async Task DeleteAsync(TodoItem item) =>
        List = await TodoService.DeleteTodoAsync(List, item.Id);

    private async Task ClearCompletedAsync() =>
        List = await TodoService.ClearCompletedAsync(List);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            TodoService.Changed -= OnListChanged;
            PreviewFeatureService?.Changed -= OnPreviewFeaturesChanged;
        }
    }
}

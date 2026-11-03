using Microsoft.AspNetCore.Components;
using Rise.Application.Services;

namespace Rise.Slices.Layout;

/// <summary>
/// The group widget: a titled container rendering whatever the shell
/// places inside it (the layout's grouped widgets). The title comes
/// from the group's own config, defaulting to "Group", and refreshes
/// when the layout editor saves a new name.
/// </summary>
public partial class GroupWidget : IDisposable
{
    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    [Inject]
    private GroupConfig Config { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string DisplayName => string.IsNullOrWhiteSpace(Config.Name) ? "Group" : Config.Name;

    protected override async Task OnInitializedAsync()
    {
        await Config.LoadAsync();
        ConfigService.ConfigChanged += OnConfigChanged;
    }

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != GroupWidgetDefinition.WidgetId)
        {
            return;
        }

        await Config.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ConfigService.ConfigChanged -= OnConfigChanged;
        }
    }
}

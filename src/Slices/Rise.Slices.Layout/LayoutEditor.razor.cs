using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Layout;

/// <summary>
/// The layout editor widget: reorder, hide/show, and reset the page
/// widgets. Titles come from the registered definitions, so the editor
/// never names a concrete widget. Every change goes through the layout
/// service, which persists it and notifies the shell to re-render.
/// </summary>
public partial class LayoutEditor : IDisposable
{
    [Inject]
    private LayoutService? LayoutService { get; set; }

    [Inject]
    private IEnumerable<IWidgetDefinition> Definitions { get; set; } = Array.Empty<IWidgetDefinition>();

    private PageLayout Layout { get; set; } = new PageLayout(Array.Empty<WidgetPlacement>());

    protected override async Task OnInitializedAsync()
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.LoadAsync();
            LayoutService.Changed += OnLayoutChanged;
        }
    }

    private async void OnLayoutChanged(object? sender, EventArgs args)
    {
        if (LayoutService is null)
        {
            return;
        }

        Layout = await LayoutService.LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private string TitleOf(WidgetPlacement placement) =>
        Definitions.FirstOrDefault(d => d.Id == placement.Id)?.Title ?? placement.Id;

    private bool IsFirstInZone(WidgetPlacement placement)
    {
        var zone = Layout.Widgets
            .Where(w => w.Zone == placement.Zone)
            .ToList();
        return zone.Count == 0 || zone[0].Id == placement.Id;
    }

    private bool IsLastInZone(WidgetPlacement placement)
    {
        var zone = Layout.Widgets
            .Where(w => w.Zone == placement.Zone)
            .ToList();
        return zone.Count == 0 || zone[^1].Id == placement.Id;
    }

    private async Task MoveUpAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.MoveUpAsync(Layout, placement.Id);
        }
    }

    private async Task MoveDownAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.MoveDownAsync(Layout, placement.Id);
        }
    }

    private async Task ToggleVisibleAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.SetVisibleAsync(Layout, placement.Id, !placement.Visible);
        }
    }

    private async Task ResetAsync()
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.ResetAsync();
        }
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
            LayoutService?.Changed -= OnLayoutChanged;
        }
    }
}

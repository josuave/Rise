using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Layout;

/// <summary>
/// The layout editor widget: reorder, hide/show, and reset the active
/// layout's widgets, plus manage the saved layouts themselves (switch,
/// save the current arrangement as new, rename, delete). Titles come
/// from the registered definitions, so the editor never names a
/// concrete widget. Every change goes through the layout service,
/// which persists it and notifies the shell to re-render.
/// </summary>
public partial class LayoutEditor : IDisposable
{
    [Inject]
    private LayoutService? LayoutService { get; set; }

    [Inject]
    private IEnumerable<IWidgetDefinition> Definitions { get; set; } = Array.Empty<IWidgetDefinition>();

    private LayoutCollection Collection { get; set; } = new LayoutCollection(
        LayoutCollection.DefaultId,
        Array.Empty<NamedLayout>());

    private PageLayout Layout => Collection.Active?.Layout ?? new PageLayout(Array.Empty<WidgetPlacement>());

    private string NewLayoutName { get; set; } = string.Empty;

    private string RenameName { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        if (LayoutService is not null)
        {
            await RefreshAsync();
            LayoutService.Changed += OnLayoutChanged;
        }
    }

    private async void OnLayoutChanged(object? sender, EventArgs args)
    {
        if (LayoutService is null)
        {
            return;
        }

        await RefreshAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshAsync()
    {
        if (LayoutService is null)
        {
            return;
        }

        Collection = await LayoutService.LoadCollectionAsync();
        RenameName = Collection.Active?.Name ?? string.Empty;
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
            await LayoutService.MoveUpAsync(Layout, placement.Id);
        }
    }

    private async Task MoveDownAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            await LayoutService.MoveDownAsync(Layout, placement.Id);
        }
    }

    private async Task ToggleVisibleAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            await LayoutService.SetVisibleAsync(Layout, placement.Id, !placement.Visible);
        }
    }

    private async Task ResetAsync()
    {
        if (LayoutService is not null)
        {
            await LayoutService.ResetAsync();
        }
    }

    private async Task SwitchAsync(string id)
    {
        if (LayoutService is not null)
        {
            await LayoutService.SetActiveLayoutAsync(id);
        }
    }

    private async Task SaveAsAsync()
    {
        if (LayoutService is not null)
        {
            await LayoutService.SaveAsAsync(NewLayoutName);
            NewLayoutName = string.Empty;
        }
    }

    private async Task RenameAsync()
    {
        if (LayoutService is not null && Collection.Active is not null)
        {
            await LayoutService.RenameLayoutAsync(Collection.Active.Id, RenameName);
        }
    }

    private async Task DeleteAsync(string id)
    {
        if (LayoutService is not null)
        {
            await LayoutService.DeleteLayoutAsync(id);
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

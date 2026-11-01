using Microsoft.AspNetCore.Components;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Layout;

/// <summary>
/// The layout editor widget: reorder, hide/show, and reset the page
/// widgets. Every change goes through the layout service, which
/// persists it and notifies the shell to re-render.
/// </summary>
public partial class LayoutEditor : IDisposable
{
    [Inject]
    private LayoutService? LayoutService { get; set; }

    private PageLayout Layout { get; set; } = PageLayout.Default();

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

    private static string DisplayName(WidgetKind kind) =>
        kind switch
        {
            WidgetKind.Search => "Search",
            WidgetKind.Links => "Links",
            WidgetKind.LinkEditor => "Link editor",
            WidgetKind.Theme => "Theme switcher",
            WidgetKind.Settings => "Settings",
            _ => kind.ToString(),
        };

    private bool IsFirstInZone(WidgetKind kind)
    {
        var zone = Layout.Widgets
            .Where(w => PageLayout.ZoneOf(w.Kind) == PageLayout.ZoneOf(kind))
            .ToList();
        return zone.Count == 0 || zone[0].Kind == kind;
    }

    private bool IsLastInZone(WidgetKind kind)
    {
        var zone = Layout.Widgets
            .Where(w => PageLayout.ZoneOf(w.Kind) == PageLayout.ZoneOf(kind))
            .ToList();
        return zone.Count == 0 || zone[^1].Kind == kind;
    }

    private async Task MoveUpAsync(WidgetKind kind)
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.MoveUpAsync(Layout, kind);
        }
    }

    private async Task MoveDownAsync(WidgetKind kind)
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.MoveDownAsync(Layout, kind);
        }
    }

    private async Task ToggleVisibleAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            Layout = await LayoutService.SetVisibleAsync(Layout, placement.Kind, !placement.Visible);
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

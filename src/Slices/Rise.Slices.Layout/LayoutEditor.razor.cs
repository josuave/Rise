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
/// concrete widget. Widget settings live in the Settings dialog, not
/// here: this editor only arranges. A group row expands to its member
/// widgets, indented, with moves inside the group and a way back out;
/// deleting a group asks whether its member widgets go with it. Every
/// change goes through the layout service, which persists it and
/// notifies the shell to re-render.
/// </summary>
public partial class LayoutEditor : IDisposable
{
    [Inject]
    private LayoutService? LayoutService { get; set; }

    [Inject]
    private IEnumerable<IWidgetDefinition> Definitions { get; set; } = Array.Empty<IWidgetDefinition>();

    /// <summary>Null in older tests: no filtering then, every widget shows.</summary>
    [Inject]
    private ModuleService? ModuleService { get; set; }

    /// <summary>Null in older tests: no filtering then, every widget shows.</summary>
    [Inject]
    private IModuleCatalog? ModuleCatalog { get; set; }

    private LayoutCollection Collection { get; set; } = new LayoutCollection(
        LayoutCollection.DefaultId,
        Array.Empty<NamedLayout>());

    private PageLayout Layout => Collection.Active?.Layout ?? new PageLayout(Array.Empty<WidgetPlacement>());

    private string NewLayoutName { get; set; } = string.Empty;

    private string RenameName { get; set; } = string.Empty;

    private bool ConfirmingGroupDelete { get; set; }

    /// <summary>Ids of modules currently switched off. Their placements stay in the layout, but their rows move to the disabled list.</summary>
    private HashSet<string> DisabledModules { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        if (LayoutService is not null)
        {
            await RefreshAsync();
            LayoutService.Changed += OnLayoutChanged;
        }

        await RefreshDisabledAsync();
        ModuleService?.Changed += OnModulesChanged;
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

    private async Task RefreshDisabledAsync() =>
        DisabledModules = ModuleCatalog is null || ModuleService is null
            ? new HashSet<string>()
            : await ModuleService.DisabledIdsAsync(ModuleCatalog.Modules);

    private async void OnModulesChanged(object? sender, EventArgs args)
    {
        await RefreshAsync();
        await RefreshDisabledAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Whether a widget's module is switched off. Unclaimed widgets are never filtered.</summary>
    private bool IsWidgetDisabled(string widgetId) =>
        ModuleCatalog?.ForWidget(widgetId) is ModuleInfo module && DisabledModules.Contains(module.Id);

    private IEnumerable<WidgetPlacement> EnabledTopLevelWidgets() =>
        Layout.TopLevelWidgets().Where(w => !IsWidgetDisabled(w.Id));

    private IEnumerable<WidgetPlacement> EnabledGroupedWidgets() =>
        Layout.GroupedWidgets().Where(w => !IsWidgetDisabled(w.Id));

    private IReadOnlyList<ModuleInfo> DisabledModuleList() =>
        ModuleCatalog is null
            ? Array.Empty<ModuleInfo>()
            : ModuleCatalog.Modules.Where(m => DisabledModules.Contains(m.Id)).ToList();

    private async Task EnableModuleAsync(ModuleInfo module)
    {
        if (ModuleService is not null)
        {
            await ModuleService.SetEnabledAsync(module.Id, true);
        }
    }

    private string TitleOf(WidgetPlacement placement) =>
        Definitions.FirstOrDefault(d => d.Id == placement.Id)?.Title ?? placement.Id;

    private bool IsGroup(WidgetPlacement placement) =>
        Definitions.FirstOrDefault(d => d.Id == placement.Id)?.IsGroup ?? false;

    private bool IsFirstInZone(WidgetPlacement placement)
    {
        var zone = EnabledTopLevelWidgets()
            .Where(w => w.Zone == placement.Zone)
            .ToList();
        return zone.Count == 0 || zone[0].Id == placement.Id;
    }

    private bool IsLastInZone(WidgetPlacement placement)
    {
        var zone = EnabledTopLevelWidgets()
            .Where(w => w.Zone == placement.Zone)
            .ToList();
        return zone.Count == 0 || zone[^1].Id == placement.Id;
    }

    private bool IsFirstInGroup(WidgetPlacement child)
    {
        var children = Layout.GroupChildren.ToList();
        return children.Count == 0 || children[0] == child.Id;
    }

    private bool IsLastInGroup(WidgetPlacement child)
    {
        var children = Layout.GroupChildren.ToList();
        return children.Count == 0 || children[^1] == child.Id;
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

    private async Task AddToGroupAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            await LayoutService.AddToGroupAsync(Layout, placement.Id);
        }
    }

    private async Task RemoveFromGroupAsync(WidgetPlacement placement)
    {
        if (LayoutService is not null)
        {
            await LayoutService.RemoveFromGroupAsync(Layout, placement.Id);
        }
    }

    private async Task DeleteGroupOnlyAsync()
    {
        if (LayoutService is not null)
        {
            await LayoutService.DeleteGroupAsync(deleteWidgets: false);
            ConfirmingGroupDelete = false;
        }
    }

    private async Task DeleteGroupAndWidgetsAsync()
    {
        if (LayoutService is not null)
        {
            await LayoutService.DeleteGroupAsync(deleteWidgets: true);
            ConfirmingGroupDelete = false;
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
            ModuleService?.Changed -= OnModulesChanged;
        }
    }
}

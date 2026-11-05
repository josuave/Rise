using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Wasm.Pages;

/// <summary>
/// Shell-rendered command palette (not a widget): one filterable list
/// over links, widget visibility, layouts, built-in themes, and the
/// edit-layout toggle, plus a web-search fallback for the typed query.
/// Commands run through the same services the widgets use, so the page
/// re-renders through the usual Changed events. The panel owns its
/// keyboard handling; the global hotkey listener ignores keystrokes
/// typed into the input, so typing here never fires a global hotkey.
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class CommandPalette
{
    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnToggleEditLayout { get; set; }

    [Inject]
    private LayoutService LayoutService { get; set; } = null!;

    [Inject]
    private StartPageService StartPageService { get; set; } = null!;

    [Inject]
    private SearchService SearchService { get; set; } = null!;

    [Inject]
    private IThemeStore ThemeStore { get; set; } = null!;

    [Inject]
    private ModuleService ModuleService { get; set; } = null!;

    [Inject]
    private IModuleCatalog ModuleCatalog { get; set; } = null!;

    [Inject]
    private IEnumerable<IWidgetDefinition> WidgetDefinitions { get; set; } = Array.Empty<IWidgetDefinition>();

    [Inject]
    private Microsoft.JSInterop.IJSInProcessRuntime JSInProcess { get; set; } = null!;

    private string _query = string.Empty;

    /// <summary>Typing restarts selection at the top: the filtered list may be shorter than the old selection.</summary>
    private string Query
    {
        get => _query;
        set
        {
            _query = value;
            SelectedIndex = 0;
        }
    }

    private int SelectedIndex { get; set; }

    private List<PaletteCommand> Commands { get; set; } = new();

    /// <summary>Commands matching every word of the query, in group order, with the web-search fallback last.</summary>
    private List<PaletteCommand> Filtered
    {
        get
        {
            string[] words = Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matches = Commands
                .Where(command => words.All(word =>
                    command.Title.Contains(word, StringComparison.OrdinalIgnoreCase)
                    || command.Detail is not null && command.Detail.Contains(word, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (!string.IsNullOrWhiteSpace(Query))
            {
                string query = Query.Trim();
                matches.Add(new PaletteCommand(
                    $"Search the web for '{query}'",
                    "Runs the page's search",
                    async () => await SearchService.LaunchSearchAsync(query)));
            }

            return matches;
        }
    }

    private sealed record PaletteCommand(string Title, string? Detail, Func<Task> Run);

    protected override async Task OnInitializedAsync()
    {
        PageLayout layout = await LayoutService.LoadAsync();
        StartPage page = await StartPageService.LoadAsync();
        HashSet<string> disabled = await ModuleService.DisabledIdsAsync(ModuleCatalog.Modules);

        foreach (Link link in page.Links)
        {
            string url = link.Url.Value;
            Commands.Add(new PaletteCommand(
                $"Open {link.Name}",
                url,
                () => OpenTabAsync(url)));
        }

        foreach (WidgetPlacement placement in layout.Widgets)
        {
            IWidgetDefinition? definition = WidgetDefinitions.FirstOrDefault(d => d.Id == placement.Id);
            if (definition is null)
            {
                continue;
            }

            if (ModuleCatalog.ForWidget(placement.Id) is ModuleInfo module && disabled.Contains(module.Id))
            {
                continue;
            }

            bool visible = placement.Visible;
            string id = placement.Id;
            Commands.Add(new PaletteCommand(
                visible ? $"Hide {definition.Title}" : $"Show {definition.Title}",
                "Widget",
                () => LayoutService.SetVisibleAsync(layout, id, !visible)));
        }

        LayoutCollection collection = await LayoutService.LoadCollectionAsync();
        foreach (NamedLayout named in collection.Layouts)
        {
            string id = named.Id;
            Commands.Add(new PaletteCommand(
                $"Switch to {named.Name}",
                named.Id == collection.ActiveId ? "Current layout" : "Layout",
                () => LayoutService.SetActiveLayoutAsync(id)));
        }

        Commands.Add(new PaletteCommand("Theme: Light", "Built-in theme", () => SetThemeAsync("light")));
        Commands.Add(new PaletteCommand("Theme: Dark", "Built-in theme", () => SetThemeAsync("dark")));
        Commands.Add(new PaletteCommand("Theme: System", "Built-in theme", () => SetThemeAsync("system")));
        Commands.Add(new PaletteCommand("Toggle edit layout", "Arrange widgets", () => OnToggleEditLayout.InvokeAsync()));
    }

    private Task OpenTabAsync(string url)
    {
        try
        {
            JSInProcess.InvokeVoid("open", url, "_blank");
        }
        catch (Microsoft.JSInterop.JSException)
        {
            // Opening the tab failed (e.g. popup blocked); the palette still closes.
        }

        return Task.CompletedTask;
    }

    private async Task SetThemeAsync(string theme)
    {
        await ThemeStore.SetStoredThemeAsync(theme);
        await ThemeStore.ApplyThemeAsync(theme);
    }

    private async Task RunAsync(PaletteCommand command)
    {
        await command.Run();
        await Close();
    }

    private async Task Close() => await OnClose.InvokeAsync();

    private async Task OnPanelKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "ArrowDown")
        {
            SelectedIndex = Filtered.Count == 0 ? 0 : (SelectedIndex + 1) % Filtered.Count;
        }
        else if (args.Key == "ArrowUp")
        {
            SelectedIndex = Filtered.Count == 0 ? 0 : (SelectedIndex + Filtered.Count - 1) % Filtered.Count;
        }
        else if (args.Key == "Enter" && Filtered.Count > 0)
        {
            await RunAsync(Filtered[SelectedIndex]);
        }
        else if (args.Key == "Escape")
        {
            await Close();
        }
    }
}

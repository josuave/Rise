using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Infrastructure;
using Rise.Slices.Backup;
using Rise.Slices.Header;
using Rise.Slices.Layout;
using Rise.Slices.Links;
using Rise.Slices.OmarchyThemes;
using Rise.Slices.Search;
using Rise.Slices.Weather;
using Rise.Slices.Settings;
using Rise.Slices.Settings.Abstractions;
using Rise.Slices.Todo;
using Rise.Wasm;
using Rise.Wasm.Pages;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Composition root: the shell is the only place that wires core services to their adapters.
builder.Services.AddRiseCore();

// Rise is WebAssembly-only, so the JS runtime is always the in-process
// one. Register it directly so components inject IJSInProcessRuntime
// instead of casting IJSRuntime themselves.
builder.Services.AddScoped<IJSInProcessRuntime>(sp =>
    (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>());

// Slice contributions to Settings, wired here so slices never depend on each other.
builder.Services.AddLinksSettings();
builder.Services.AddBackupSettings();
builder.Services.AddHeaderSettings();
builder.Services.AddLayoutSettings();
builder.Services.AddWeatherSettings();
builder.Services.AddSearchSettings();

// Notes is a lazy module: its section is metadata here, not the
// slice's registration, so the slice assembly stays unloaded until
// the section itself renders.
builder.Services.AddScoped<ISettingsSection, LazyNotesSettingsSection>();

// 2048 is a lazy module too: same metadata treatment as Notes.
builder.Services.AddScoped<ISettingsSection, LazyGame2048SettingsSection>();

// Shell-owned section: the module switchboard. It lives here, not in
// a slice, because it lists every module including Settings itself.
builder.Services.AddScoped<ISettingsSection, ModuleSettingsSection>();

// Shell-owned section: the hotkey manager. The actions are app-wide,
// so the manager lives here, not in a slice.
builder.Services.AddScoped<ISettingsSection, KeyboardSettingsSection>();

// Shell-owned section: the preview-feature manager. The flags are
// app-wide, so the manager lives here, not in a slice.
builder.Services.AddScoped<ISettingsSection, PreviewSettingsSection>();

// Shell-owned section: the background picker. The background is
// page-wide, so the picker lives here, not in a slice.
builder.Services.AddScoped<ISettingsSection, BackgroundSettingsSection>();

// Shell-owned section: the home screen icon. The manifest is
// shell-owned, so the toggle lives here, not in a slice.
builder.Services.AddScoped<ISettingsSection, HomeScreenIconSettingsSection>();

// Widget definitions, wired here: adding a widget is a new definition
// plus one line here, never a change to the layout model or the shell.
builder.Services.AddHeaderWidgets();
builder.Services.AddBackupWidgets();
builder.Services.AddSearchWidget();
builder.Services.AddLinksWidgets();
builder.Services.AddLayoutWidgets();
builder.Services.AddTodoWidgets();
builder.Services.AddWeatherWidgets();

// Theme pack: no widgets — the slice only supplies the Omarchy
// themes, listed by the theme picker instead of the layout editor.
builder.Services.AddOmarchyThemes();

// Notes is a lazy module: its definition is metadata here, not the
// slice's registration, so the slice assembly stays unloaded until
// the widget itself shows. 2048 is lazy the same way.
builder.Services.AddSingleton<IWidgetDefinition>(LazyWidgetDefinition.Notes);
builder.Services.AddSingleton<IWidgetDefinition>(LazyWidgetDefinition.Game2048);
builder.Services.AddSettingsWidgets();

// The module catalog, owned by the composition root: the shell,
// the layout editor, and the settings dialog filter by it.
builder.Services.AddSingleton<IModuleCatalog, StaticModuleCatalog>();

await builder.Build().RunAsync();

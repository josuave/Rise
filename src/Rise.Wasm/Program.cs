using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Rise.Infrastructure;
using Rise.Slices.Backup;
using Rise.Slices.Header;
using Rise.Slices.Layout;
using Rise.Slices.Links;
using Rise.Slices.Notes;
using Rise.Slices.Search;
using Rise.Slices.Settings;
using Rise.Slices.Todo;
using Rise.Wasm;

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
builder.Services.AddNotesSettings();

// Widget definitions, wired here: adding a widget is a new definition
// plus one line here, never a change to the layout model or the shell.
builder.Services.AddHeaderWidgets();
builder.Services.AddBackupWidgets();
builder.Services.AddSearchWidget();
builder.Services.AddLinksWidgets();
builder.Services.AddLayoutWidgets();
builder.Services.AddTodoWidgets();
builder.Services.AddNotesWidgets();
builder.Services.AddSettingsWidgets();

await builder.Build().RunAsync();

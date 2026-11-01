using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rise.Infrastructure;
using Rise.Slices.Header;
using Rise.Slices.Links;
using Rise.Slices.Search;
using Rise.Slices.Settings;
using Rise.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Composition root: the shell is the only place that wires core services to their adapters.
builder.Services.AddRiseCore();

// Slice contributions to Settings, wired here so slices never depend on each other.
builder.Services.AddLinksSettings();

// Widget definitions, wired here: adding a widget is a new definition
// plus one line here, never a change to the layout model or the shell.
builder.Services.AddHeaderWidget();
builder.Services.AddSearchWidget();
builder.Services.AddLinksWidgets();
builder.Services.AddSettingsWidgets();

await builder.Build().RunAsync();

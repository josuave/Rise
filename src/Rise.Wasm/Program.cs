using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rise.Infrastructure;
using Rise.Slices.Links;
using Rise.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Composition root: the shell is the only place that wires core services to their adapters.
builder.Services.AddRiseCore();

// Slice contributions to Settings, wired here so slices never depend on each other.
builder.Services.AddLinksSettings();

await builder.Build().RunAsync();

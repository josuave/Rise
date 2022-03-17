using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rise.Blazor.App;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var appSelector = "#app";
builder.RootComponents
    .Add<App>(appSelector);

var headSelector = "head::after";
builder.RootComponents
    .Add<HeadOutlet>(headSelector);

builder.Services
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host =  builder.Build();

await host.RunAsync();
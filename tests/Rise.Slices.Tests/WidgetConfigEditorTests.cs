using System.Text.Json.Nodes;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Header;
using Xunit;
using ClockEditor = Rise.Slices.Header.ClockConfigEditor;
using GreetingEditor = Rise.Slices.Header.GreetingConfigEditor;

namespace Rise.Slices.Tests;

public class WidgetConfigEditorTests : BunitContext
{
    [Fact]
    public void ClockEditor_RendersUncheckedBoxes_ByDefault()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore());
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<ClockConfig>();

        IRenderedComponent<ClockEditor> cut = Render<ClockEditor>();

        cut.WaitForAssertion(() => Assert.Contains("24-hour clock", cut.Markup, StringComparison.Ordinal));
        Assert.False(cut.Find("[aria-label=\"24-hour clock\"]").HasAttribute("checked"));
        Assert.False(cut.Find("[aria-label=\"Show seconds\"]").HasAttribute("checked"));
    }

    [Fact]
    public void ClockEditor_TogglingBoxes_SavesConfig()
    {
        var store = new FakeWidgetConfigStore();
        Services.AddScoped<IWidgetConfigStore>(_ => store);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<ClockConfig>();

        IRenderedComponent<ClockEditor> cut = Render<ClockEditor>();

        cut.Find("[aria-label=\"24-hour clock\"]").Change(true);
        JsonObject saved = JsonNode.Parse(store.Stored["Clock"])!.AsObject();
        Assert.True(saved["Use24Hour"]?.GetValue<bool>() ?? false);
        Assert.False(saved["ShowSeconds"]?.GetValue<bool>() ?? false);

        cut.Find("[aria-label=\"Show seconds\"]").Change(true);
        saved = JsonNode.Parse(store.Stored["Clock"])!.AsObject();
        Assert.True(saved["Use24Hour"]?.GetValue<bool>() ?? false);
        Assert.True(saved["ShowSeconds"]?.GetValue<bool>() ?? false);
    }

    [Fact]
    public void GreetingEditor_ChangingText_SavesCustomText()
    {
        var store = new FakeWidgetConfigStore();
        Services.AddScoped<IWidgetConfigStore>(_ => store);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<GreetingConfig>();

        IRenderedComponent<GreetingEditor> cut = Render<GreetingEditor>();

        cut.Find("[aria-label=\"Custom greeting\"]").Change("Hey there.");
        Assert.Equal("{\"CustomText\":\"Hey there.\"}", store.Stored["Greeting"]);
    }

    [Fact]
    public void GreetingEditor_ClearingText_RemovesCustomText()
    {
        var store = new FakeWidgetConfigStore(new Dictionary<string, string>
        {
            ["Greeting"] = "{\"CustomText\":\"Hey there.\"}",
        });
        Services.AddScoped<IWidgetConfigStore>(_ => store);
        Services.AddScoped<WidgetConfigService>();
        Services.AddScoped<GreetingConfig>();

        IRenderedComponent<GreetingEditor> cut = Render<GreetingEditor>();

        cut.Find("[aria-label=\"Custom greeting\"]").Change("   ");
        JsonObject saved = JsonNode.Parse(store.Stored["Greeting"])!.AsObject();
        Assert.Null(saved["CustomText"]);
    }
}

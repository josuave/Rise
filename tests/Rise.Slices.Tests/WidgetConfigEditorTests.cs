using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
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

        IRenderedComponent<ClockEditor> cut = Render<ClockEditor>();

        cut.Find("[aria-label=\"24-hour clock\"]").Change(true);
        Assert.Equal("{\"Use24Hour\":true,\"ShowSeconds\":false}", store.Stored["Clock"]);

        cut.Find("[aria-label=\"Show seconds\"]").Change(true);
        Assert.Equal("{\"Use24Hour\":true,\"ShowSeconds\":true}", store.Stored["Clock"]);
    }

    [Fact]
    public void GreetingEditor_ChangingText_SavesCustomText()
    {
        var store = new FakeWidgetConfigStore();
        Services.AddScoped<IWidgetConfigStore>(_ => store);
        Services.AddScoped<WidgetConfigService>();

        IRenderedComponent<GreetingEditor> cut = Render<GreetingEditor>();

        cut.Find("[aria-label=\"Custom greeting\"]").Change("Hey there.");
        Assert.Equal("{\"CustomText\":\"Hey there.\"}", store.Stored["Greeting"]);
    }

    [Fact]
    public void GreetingEditor_ClearingText_SavesNull()
    {
        var store = new FakeWidgetConfigStore(new Dictionary<string, string>
        {
            ["Greeting"] = "{\"CustomText\":\"Hey there.\"}",
        });
        Services.AddScoped<IWidgetConfigStore>(_ => store);
        Services.AddScoped<WidgetConfigService>();

        IRenderedComponent<GreetingEditor> cut = Render<GreetingEditor>();

        cut.Find("[aria-label=\"Custom greeting\"]").Change("   ");
        Assert.Equal("{\"CustomText\":null}", store.Stored["Greeting"]);
    }
}

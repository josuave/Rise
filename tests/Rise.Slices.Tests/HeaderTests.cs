using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;
using DateComponent = Rise.Slices.Header.HeaderDate;
using ClockComponent = Rise.Slices.Header.HeaderClock;
using GreetingComponent = Rise.Slices.Header.HeaderGreeting;

namespace Rise.Slices.Tests;

public class HeaderTests : BunitContext
{
    [Fact]
    public void Date_RendersFormattedDate_FromTheClock()
    {
        var moment = new DateTime(2026, 11, 1, 9, 30, 0);
        Services.AddSingleton<IClock>(_ => new FakeClock(moment));

        IRenderedComponent<DateComponent> cut = Render<DateComponent>();

        Assert.Contains(Greeting.FormatDate(moment), cut.Markup, StringComparison.Ordinal);
        Assert.Contains("calm-date", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Clock_RendersTwelveHourWithoutSeconds_ByDefault()
    {
        var moment = new DateTime(2026, 11, 1, 9, 30, 7);
        Services.AddSingleton<IClock>(_ => new FakeClock(moment));
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore());
        Services.AddScoped<WidgetConfigService>();

        IRenderedComponent<ClockComponent> cut = Render<ClockComponent>();

        cut.WaitForAssertion(() => Assert.Contains("9:30 AM", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("calm-clock", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Clock_RendersConfiguredFormat_FromStoredConfig()
    {
        var moment = new DateTime(2026, 11, 1, 9, 30, 7);
        Services.AddSingleton<IClock>(_ => new FakeClock(moment));
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore(new Dictionary<string, string>
        {
            ["Clock"] = "{\"Use24Hour\":true,\"ShowSeconds\":true}",
        }));
        Services.AddScoped<WidgetConfigService>();

        IRenderedComponent<ClockComponent> cut = Render<ClockComponent>();

        cut.WaitForAssertion(() => Assert.Contains("09:30:07", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Greeting_RendersTimeBasedGreeting_ByDefault()
    {
        var moment = new DateTime(2026, 11, 1, 9, 30, 0);
        Services.AddSingleton<IClock>(_ => new FakeClock(moment));
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore());
        Services.AddScoped<WidgetConfigService>();

        IRenderedComponent<GreetingComponent> cut = Render<GreetingComponent>();

        cut.WaitForAssertion(() => Assert.Contains(Greeting.For(moment), cut.Markup, StringComparison.Ordinal));
        Assert.Contains("calm-greeting", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Greeting_RendersCustomText_WhenConfigured()
    {
        var moment = new DateTime(2026, 11, 1, 9, 30, 0);
        Services.AddSingleton<IClock>(_ => new FakeClock(moment));
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore(new Dictionary<string, string>
        {
            ["Greeting"] = "{\"CustomText\":\"Hey there.\"}",
        }));
        Services.AddScoped<WidgetConfigService>();

        IRenderedComponent<GreetingComponent> cut = Render<GreetingComponent>();

        cut.WaitForAssertion(() => Assert.Contains("Hey there.", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain(Greeting.For(moment), cut.Markup, StringComparison.Ordinal);
    }
}

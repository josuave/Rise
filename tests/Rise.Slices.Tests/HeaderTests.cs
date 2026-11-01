using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;
using Xunit;
using HeaderComponent = Rise.Slices.Header.Header;

namespace Rise.Slices.Tests;

public class HeaderTests : BunitContext
{
    [Fact]
    public void RendersDateClockAndGreeting_FromTheClock()
    {
        var moment = new DateTime(2026, 11, 1, 9, 30, 0);
        Services.AddSingleton<IClock>(_ => new FakeClock(moment));

        IRenderedComponent<HeaderComponent> cut = Render<HeaderComponent>();

        Assert.Contains(Greeting.FormatDate(moment), cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Greeting.FormatTime(moment), cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Greeting.For(moment), cut.Markup, StringComparison.Ordinal);
        Assert.Contains("calm-date", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("calm-clock", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("calm-greeting", cut.Markup, StringComparison.Ordinal);
    }
}

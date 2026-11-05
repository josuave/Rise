using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Slices.Notes;
using Xunit;

namespace Rise.Slices.Tests;

public class NotesViewSettingsTests : BunitContext
{
    private readonly FakeWidgetConfigStore _configStore = new();

    public NotesViewSettingsTests()
    {
        Services.AddScoped<IWidgetConfigStore>(_ => _configStore);
        Services.AddScoped<WidgetConfigService>();
    }

    [Fact]
    public void PostItsButton_SwitchesView_AndPersistsIt()
    {
        IRenderedComponent<NotesViewSettings> cut = Render<NotesViewSettings>();

        cut.Find("[aria-label=\"Show notes as post-its\"]").Click();

        cut.WaitForAssertion(() => Assert.True(
            _configStore.Stored.TryGetValue("Notes", out string? json)
                && json.Contains("PostItView\":true", StringComparison.Ordinal)));
        Assert.Contains(
            "selected",
            cut.Find("[aria-label=\"Show notes as post-its\"]").ClassName,
            StringComparison.Ordinal);
    }
}

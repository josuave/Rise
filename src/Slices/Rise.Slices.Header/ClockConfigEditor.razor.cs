using Microsoft.AspNetCore.Components;
using Rise.Application.Services;

namespace Rise.Slices.Header;

/// <summary>
/// The clock widget's settings, rendered by the layout editor:
/// 24-hour format and seconds, saved immediately on change.
/// </summary>
public partial class ClockConfigEditor
{
    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private WidgetConfig.ClockConfig Config { get; set; } = WidgetConfig.ClockConfig.Default;

    protected override async Task OnInitializedAsync() =>
        Config = await WidgetConfig.LoadClockAsync(ConfigService);

    private async Task Set24HourAsync(ChangeEventArgs args)
    {
        if (args.Value is not bool use24Hour)
        {
            return;
        }

        Config = Config with { Use24Hour = use24Hour };
        await WidgetConfig.SaveClockAsync(ConfigService, Config);
    }

    private async Task SetSecondsAsync(ChangeEventArgs args)
    {
        if (args.Value is not bool showSeconds)
        {
            return;
        }

        Config = Config with { ShowSeconds = showSeconds };
        await WidgetConfig.SaveClockAsync(ConfigService, Config);
    }
}

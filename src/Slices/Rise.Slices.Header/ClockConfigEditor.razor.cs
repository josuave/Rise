using Microsoft.AspNetCore.Components;

namespace Rise.Slices.Header;

/// <summary>
/// The clock widget's settings, rendered in the Settings dialog:
/// 24-hour format and seconds, saved immediately on change.
/// </summary>
public partial class ClockConfigEditor
{
    [Inject]
    private ClockConfig Config { get; set; } = null!;

    protected override async Task OnInitializedAsync() =>
        await Config.LoadAsync();

    private async Task Set24HourAsync(ChangeEventArgs args)
    {
        if (args.Value is not bool use24Hour)
        {
            return;
        }

        Config.Use24Hour = use24Hour;
        await Config.SaveAsync();
    }

    private async Task SetSecondsAsync(ChangeEventArgs args)
    {
        if (args.Value is not bool showSeconds)
        {
            return;
        }

        Config.ShowSeconds = showSeconds;
        await Config.SaveAsync();
    }
}

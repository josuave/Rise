using Microsoft.AspNetCore.Components;
using Rise.Application.Services;

namespace Rise.Slices.Header;

/// <summary>
/// The greeting widget's settings, rendered by the layout editor:
/// a custom greeting text, saved on change. Blank restores the
/// automatic time-based greeting.
/// </summary>
public partial class GreetingConfigEditor
{
    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private WidgetConfig.GreetingConfig Config { get; set; } = WidgetConfig.GreetingConfig.Default;

    protected override async Task OnInitializedAsync() =>
        Config = await WidgetConfig.LoadGreetingAsync(ConfigService);

    private async Task SetCustomTextAsync(ChangeEventArgs args)
    {
        string? text = args.Value as string;
        Config = Config with { CustomText = string.IsNullOrWhiteSpace(text) ? null : text };
        await WidgetConfig.SaveGreetingAsync(ConfigService, Config);
    }
}

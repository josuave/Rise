using Microsoft.AspNetCore.Components;

namespace Rise.Slices.Header;

/// <summary>
/// The greeting widget's settings, rendered in the Settings dialog:
/// a custom greeting text, saved on change. Blank restores the
/// automatic time-based greeting.
/// </summary>
public partial class GreetingConfigEditor
{
    [Inject]
    private GreetingConfig Config { get; set; } = null!;

    protected override async Task OnInitializedAsync() =>
        await Config.LoadAsync();

    private async Task SetCustomTextAsync(ChangeEventArgs args)
    {
        string? text = args.Value as string;
        Config.CustomText = string.IsNullOrWhiteSpace(text) ? null : text;
        await Config.SaveAsync();
    }
}

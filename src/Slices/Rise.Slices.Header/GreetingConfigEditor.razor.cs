using Microsoft.AspNetCore.Components;

namespace Rise.Slices.Header;

/// <summary>
/// The greeting widget's settings, rendered in the Settings dialog:
/// a name used in the automatic greeting, and a custom greeting
/// text, both saved on change. A blank name is not used; blank
/// custom text restores the automatic time-based greeting.
/// </summary>
public partial class GreetingConfigEditor
{
    [Inject]
    private GreetingConfig Config { get; set; } = null!;

    protected override async Task OnInitializedAsync() =>
        await Config.LoadAsync();

    private async Task SetNameAsync(ChangeEventArgs args)
    {
        string? name = args.Value as string;
        Config.Name = string.IsNullOrWhiteSpace(name) ? null : name;
        await Config.SaveAsync();
    }

    private async Task SetCustomTextAsync(ChangeEventArgs args)
    {
        string? text = args.Value as string;
        Config.CustomText = string.IsNullOrWhiteSpace(text) ? null : text;
        await Config.SaveAsync();
    }
}

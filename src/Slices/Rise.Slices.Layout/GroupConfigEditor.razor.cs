using Microsoft.AspNetCore.Components;

namespace Rise.Slices.Layout;

/// <summary>
/// The group widget's settings, rendered in the Settings dialog: a
/// display name, saved on change. Blank restores the default.
/// </summary>
public partial class GroupConfigEditor
{
    [Inject]
    private GroupConfig Config { get; set; } = null!;

    protected override async Task OnInitializedAsync() =>
        await Config.LoadAsync();

    private async Task SetNameAsync(ChangeEventArgs args)
    {
        string? text = args.Value as string;
        Config.Name = string.IsNullOrWhiteSpace(text) ? null : text;
        await Config.SaveAsync();
    }
}

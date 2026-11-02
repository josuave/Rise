using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;

namespace Rise.Slices.Header;

/// <summary>
/// The clock widget: the current time in the configured format,
/// refreshed every second while seconds are shown, every 30 seconds
/// otherwise. Reloads its config when the layout editor saves one.
/// </summary>
public partial class HeaderClock : IDisposable
{
    [Inject]
    private IClock Clock { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private WidgetConfig.ClockConfig Config { get; set; } = WidgetConfig.ClockConfig.Default;

    private System.Threading.Timer? _timer;

    protected override async Task OnInitializedAsync()
    {
        Config = await WidgetConfig.LoadClockAsync(ConfigService);
        ConfigService.ConfigChanged += OnConfigChanged;

        StartTimer();
    }

    private void StartTimer()
    {
        _timer?.Dispose();
        _timer = new System.Threading.Timer(
            _ => InvokeAsync(StateHasChanged),
            null,
            TimeSpan.FromSeconds(1),
            Config.ShowSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(30));
    }

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != ClockWidgetDefinition.WidgetId)
        {
            return;
        }

        Config = await WidgetConfig.LoadClockAsync(ConfigService);
        StartTimer();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
            ConfigService.ConfigChanged -= OnConfigChanged;
        }
    }
}

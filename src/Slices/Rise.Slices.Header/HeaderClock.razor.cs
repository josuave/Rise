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

    [Inject]
    private ClockConfig Config { get; set; } = null!;

    private System.Threading.Timer? _timer;

    /// <summary>The clock's parts for the split rendering: digits, seconds, meridiem — concatenated they equal the formatted time.</summary>
    private Rise.Domain.TimeParts Parts => Rise.Domain.Greeting.FormatTimeParts(Clock.Now, Config.Use24Hour, Config.ShowSeconds);

    protected override async Task OnInitializedAsync()
    {
        await Config.LoadAsync();
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

        await Config.LoadAsync();
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

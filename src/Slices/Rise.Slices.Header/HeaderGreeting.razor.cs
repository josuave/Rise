using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Header;

/// <summary>
/// The greeting widget: the time-based greeting, or the user's
/// custom text when one is saved. Refreshed once a minute, which
/// covers the morning/afternoon/evening boundaries, and reloaded
/// when the layout editor saves a new custom text.
/// </summary>
public partial class HeaderGreeting : IDisposable
{
    [Inject]
    private IClock Clock { get; set; } = null!;

    [Inject]
    private WidgetConfigService ConfigService { get; set; } = null!;

    private WidgetConfig.GreetingConfig Config { get; set; } = WidgetConfig.GreetingConfig.Default;

    private System.Threading.Timer? _timer;

    private string Text
    {
        get
        {
            string? custom = Config.CustomText?.Trim();
            return string.IsNullOrEmpty(custom) ? Greeting.For(Clock.Now) : custom;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Config = await WidgetConfig.LoadGreetingAsync(ConfigService);
        ConfigService.ConfigChanged += OnConfigChanged;

        _timer = new System.Threading.Timer(
            _ => InvokeAsync(StateHasChanged),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMinutes(1));
    }

    private async void OnConfigChanged(object? sender, WidgetConfigChangedEventArgs args)
    {
        if (args.WidgetId != GreetingWidgetDefinition.WidgetId)
        {
            return;
        }

        Config = await WidgetConfig.LoadGreetingAsync(ConfigService);
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

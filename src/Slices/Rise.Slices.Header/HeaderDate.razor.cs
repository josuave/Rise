using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;

namespace Rise.Slices.Header;

/// <summary>
/// The date widget: the current date, refreshed once a minute.
/// Split from the old combined header so it moves and hides on
/// its own.
/// </summary>
public partial class HeaderDate : IDisposable
{
    [Inject]
    private IClock Clock { get; set; } = null!;

    private System.Threading.Timer? _timer;

    protected override void OnInitialized() => _timer = new System.Threading.Timer(
        _ => InvokeAsync(StateHasChanged),
        null,
        TimeSpan.FromSeconds(1),
        TimeSpan.FromMinutes(1));

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
        }
    }
}

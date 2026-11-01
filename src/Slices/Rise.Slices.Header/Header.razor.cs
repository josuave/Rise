using Microsoft.AspNetCore.Components;
using Rise.Application.Ports;

namespace Rise.Slices.Header;

/// <summary>
/// The header widget: date, clock, and greeting, previously rendered
/// directly by the shell. It owns the refresh timer now, so the shell
/// no longer re-renders on a clock tick — it re-renders only when the
/// layout changes. The clock text has minute precision, so a 30-second
/// tick is plenty; the first tick fires after one second.
/// </summary>
public partial class Header : IDisposable
{
    [Inject]
    private IClock Clock { get; set; } = null!;

    private System.Threading.Timer? _timer;

    protected override void OnInitialized() => _timer = new System.Threading.Timer(
        _ => InvokeAsync(StateHasChanged),
        null,
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(30));

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

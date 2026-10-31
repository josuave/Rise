using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>Outbound adapter: the real clock. The only place DateTime.Now is read.</summary>
public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

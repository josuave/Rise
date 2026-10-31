namespace Rise.Application.Ports;

/// <summary>Outbound port: where "now" comes from. Pure code takes a DateTime; only the shell reads the real clock.</summary>
public interface IClock
{
    DateTime Now { get; }
}

namespace Rise.Application.Ports;

/// <summary>Outbound port: how a search URL is opened. The core knows nothing about browser tabs.</summary>
public interface ISearchLauncher
{
    Task LaunchAsync(Uri url, CancellationToken cancellationToken = default);
}

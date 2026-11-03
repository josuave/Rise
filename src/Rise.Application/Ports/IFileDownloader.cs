namespace Rise.Application.Ports;

/// <summary>Outbound port: save a file to the user's machine (a download). The core never touches JS itself.</summary>
public interface IFileDownloader
{
    Task DownloadAsync(string filename, string content, CancellationToken cancellationToken = default);
}

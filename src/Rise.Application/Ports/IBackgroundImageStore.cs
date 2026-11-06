namespace Rise.Application.Ports;

/// <summary>
/// Port for uploaded background image bytes: IndexedDB, keyed by the
/// SHA-256 hash of the file. The choice and the backup carry only the
/// descriptor; this port is the bytes. Save reports whether the bytes
/// actually landed, so the service can tell the user when the browser
/// would not store them (e.g. private mode) instead of claiming the
/// image applied. GetUrl turns stored bytes back into a blob URL the
/// page references like any image, or null when the bytes are gone.
/// </summary>
public interface IBackgroundImageStore
{
    Task<bool> SaveAsync(string hash, byte[] bytes, string mimeType, CancellationToken cancellationToken = default);

    Task<string?> GetUrlAsync(string hash, CancellationToken cancellationToken = default);

    Task DeleteAsync(string hash, CancellationToken cancellationToken = default);

    Task ClearUrlAsync(CancellationToken cancellationToken = default);
}

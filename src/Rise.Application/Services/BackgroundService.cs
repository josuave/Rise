using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// The background switchboard: the one chosen background. Absent
/// means none. The choice is cached after the first load — this
/// service is the app's only writer — and every change raises
/// Changed so the shell can re-render the page layer live.
/// An uploaded image's bytes live in the image store keyed by
/// hash; the choice carries only the descriptor. The first
/// GetCssAsync for an upload resolves the bytes to a blob URL and
/// remembers whether they were there — a missing upload sets
/// UploadMissing, which the Settings section reads to prompt for
/// a re-upload naming that file.
/// </summary>
public sealed class BackgroundService(IBackgroundStore store, IBackgroundImageStore imageStore)
{
    /// <summary>Raised after any successful change, so the page layer can re-render.</summary>
    public event EventHandler? Changed;

    private BackgroundChoice? _cached;

    private string? _resolvedUrl;

    private bool _resolutionAttempted;

    private bool _uploadMissing;

    /// <summary>True when the current choice is an upload whose bytes could not be found.</summary>
    public bool UploadMissing => _uploadMissing;

    /// <summary>The missing file's name, when UploadMissing is true.</summary>
    public string? MissingFileName => !_uploadMissing
        ? null
        : _cached switch
        {
            UploadBackground upload => upload.Image.FileName,
            PresetBackground => null,
            CustomBackground => null,
            null => null,
        };

    /// <summary>Warms the cache; call once at startup so later reads never touch storage.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        _ = await GetChoiceAsync(cancellationToken);

    public async Task<BackgroundChoice> GetChoiceAsync(CancellationToken cancellationToken = default)
    {
        _cached ??= await store.GetAsync(cancellationToken) ?? BackgroundChoice.None;
        return _cached.Value;
    }

    /// <summary>
    /// The CSS background-image value for the current choice: a
    /// preset gradient, a custom image URL, an uploaded image's blob
    /// URL, or empty for none. Resolves an upload's bytes on the
    /// first call and caches the result; a missing upload resolves
    /// to empty and sets UploadMissing.
    /// </summary>
    public async Task<string> GetCssAsync(CancellationToken cancellationToken = default)
    {
        BackgroundChoice choice = await GetChoiceAsync(cancellationToken);
        if (choice is not UploadBackground upload)
        {
            return choice.Css;
        }

        if (!_resolutionAttempted)
        {
            _resolutionAttempted = true;
            _resolvedUrl = await imageStore.GetUrlAsync(upload.Image.Hash, cancellationToken);
            _uploadMissing = _resolvedUrl is null;
        }

        return _resolvedUrl is null ? string.Empty : $"url('{_resolvedUrl}')";
    }

    public async Task<Result<BackgroundChoice>> SetPresetAsync(string presetId, CancellationToken cancellationToken = default)
    {
        if (!BackgroundPresets.All.Any(p => p.Id == presetId))
        {
            return Result<BackgroundChoice>.Failure("Unknown background.");
        }

        return await SaveAsync(new PresetBackground(presetId), cancellationToken);
    }

    public async Task<Result<BackgroundChoice>> SetCustomUrlAsync(string? address, CancellationToken cancellationToken = default)
    {
        string? text = address?.Trim();
        if (string.IsNullOrWhiteSpace(text)
            || text.Contains('"', StringComparison.Ordinal)
            || text.Contains('\'', StringComparison.Ordinal)
            || !text.Contains("://", StringComparison.Ordinal)
            || !Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result<BackgroundChoice>.Failure("Enter an image address starting with http:// or https://.");
        }

        return await SaveAsync(new CustomBackground(text), cancellationToken);
    }

    /// <summary>
    /// Applies an uploaded image: stores the bytes under the hash,
    /// then saves the descriptor as the choice. Replacing a
    /// different upload deletes the old bytes. A hash that is not a
    /// 64-character uppercase hex string, a blank file name, a
    /// non-positive size, or a non-image type is refused. When the
    /// browser would not store the bytes, the choice is left
    /// untouched and the failure is reported.
    /// </summary>
    public async Task<Result<BackgroundChoice>> SetUploadAsync(UploadedImage image, byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (image.Hash.Length != 64
            || !image.Hash.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F')
            || string.IsNullOrWhiteSpace(image.FileName)
            || image.Size <= 0
            || !image.MimeType.StartsWith("image/", StringComparison.Ordinal))
        {
            return Result<BackgroundChoice>.Failure("That file isn't a usable image.");
        }

        BackgroundChoice current = await GetChoiceAsync(cancellationToken);
        UploadedImage? currentImage = current.UploadImage;
        if (currentImage is not null && currentImage.Hash != image.Hash)
        {
            await imageStore.DeleteAsync(currentImage.Hash, cancellationToken);
        }

        if (!await imageStore.SaveAsync(image.Hash, bytes, image.MimeType, cancellationToken))
        {
            return Result<BackgroundChoice>.Failure("Couldn't store that image in this browser.");
        }

        return await SaveAsync(new UploadBackground(image), cancellationToken);
    }

    public async Task<Result<BackgroundChoice>> ClearAsync(CancellationToken cancellationToken = default) =>
        await SaveAsync(BackgroundChoice.None, cancellationToken);

    private async Task<Result<BackgroundChoice>> SaveAsync(BackgroundChoice choice, CancellationToken cancellationToken)
    {
        _cached = choice;
        _resolvedUrl = null;
        _resolutionAttempted = false;
        _uploadMissing = false;
        if (choice is not UploadBackground)
        {
            await imageStore.ClearUrlAsync(cancellationToken);
        }

        await store.SaveAsync(choice, cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return Result<BackgroundChoice>.Success(choice);
    }
}

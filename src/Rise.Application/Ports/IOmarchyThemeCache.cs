using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>
/// Writes the pack palettes to the browser cache the shell's
/// before-paint script reads. The script runs before Blazor starts,
/// so it cannot ask .NET for the palettes; the app writes them here
/// on boot instead, and later loads apply a pack choice before
/// first paint. A failed write only costs a first-paint flash.
/// </summary>
public interface IOmarchyThemeCache
{
    Task WriteCacheAsync(IReadOnlyList<OmarchyTheme> themes, CancellationToken cancellationToken = default);
}

using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>
/// Supplies the Omarchy theme pack: every theme the module ships.
/// The data is static and lives in the OmarchyThemes slice; the
/// shell reads it through this port so no slice references another.
/// Whether the pack shows is a module question, answered by the
/// module service — not by this provider.
/// </summary>
public interface IOmarchyThemeProvider
{
    IReadOnlyList<OmarchyTheme> Themes { get; }
}

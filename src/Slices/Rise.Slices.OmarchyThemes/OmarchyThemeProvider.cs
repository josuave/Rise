using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.OmarchyThemes;

/// <summary>Exposes the pack registry through the provider port. The data is static, so this is a thin wrapper.</summary>
public sealed class OmarchyThemeProvider : IOmarchyThemeProvider
{
    public IReadOnlyList<OmarchyTheme> Themes => OmarchyThemePack.All;
}

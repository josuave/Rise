using Microsoft.Extensions.DependencyInjection;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Links;

/// <summary>
/// The Links slice's fulfillment of the Settings contract: its section
/// descriptor, plus the DI registration the shell calls. The Settings slice
/// never references this type — it only sees <see cref="ISettingsSection"/>
/// instances the shell registered.
/// </summary>
public sealed class LinksSettingsSection : ISettingsSection
{
    public string Title => "Links";
    public int Order => 100;
    public string Description => "Tiles shown on the home page.";
    public Type ComponentType => typeof(LinksSettings);
}

/// <summary>Shell wiring for the Links slice's Settings contribution.</summary>
public static class LinksSettingsExtensions
{
    public static IServiceCollection AddLinksSettings(this IServiceCollection services)
    {
        services.AddScoped<ISettingsSection, LinksSettingsSection>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Layout;

/// <summary>
/// The layout slice's fulfillment of the Settings contract: the group
/// name section, plus the DI registration the shell calls.
/// </summary>
public sealed class GroupSettingsSection : ISettingsSection
{
    public string Title => "Group";
    public int Order => 130;
    public string Description => "Name the group on the page.";
    public Type ComponentType => typeof(GroupConfigEditor);
}

/// <summary>Shell wiring for the layout slice's Settings contribution.</summary>
public static class LayoutSettingsExtensions
{
    public static IServiceCollection AddLayoutSettings(this IServiceCollection services)
    {
        services.AddScoped<ISettingsSection, GroupSettingsSection>();
        return services;
    }
}

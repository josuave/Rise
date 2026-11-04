using Microsoft.Extensions.DependencyInjection;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Header;

/// <summary>
/// The header slice's fulfillment of the Settings contract: clock and
/// greeting sections, plus the DI registration the shell calls. The
/// dialog renders the slices' own editor components, so the settings
/// live in one place instead of behind Configure buttons in the
/// layout editor.
/// </summary>
public sealed class ClockSettingsSection : ISettingsSection
{
    public string Title => "Clock";
    public int Order => 110;
    public string Description => "12-hour or 24-hour, with or without seconds.";
    public Type ComponentType => typeof(ClockConfigEditor);
}

public sealed class GreetingSettingsSection : ISettingsSection
{
    public string Title => "Greeting";
    public int Order => 120;
    public string Description => "Custom greeting text; blank restores the automatic greeting.";
    public Type ComponentType => typeof(GreetingConfigEditor);
}

/// <summary>Shell wiring for the header slice's Settings contributions.</summary>
public static class HeaderSettingsExtensions
{
    public static IServiceCollection AddHeaderSettings(this IServiceCollection services)
    {
        services.AddScoped<ISettingsSection, ClockSettingsSection>();
        services.AddScoped<ISettingsSection, GreetingSettingsSection>();
        return services;
    }
}

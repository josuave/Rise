using Microsoft.Extensions.DependencyInjection;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Notes;

/// <summary>
/// The Notes slice's fulfillment of the Settings contract: its view
/// section, plus the DI registration the shell calls.
/// </summary>
public sealed class NotesSettingsSection : ISettingsSection
{
    public string Title => "Notes";
    public int Order => 140;
    public string Description => "List or post-it view.";
    public Type ComponentType => typeof(NotesViewSettings);
}

/// <summary>Shell wiring for the Notes slice's Settings contribution.</summary>
public static class NotesSettingsExtensions
{
    public static IServiceCollection AddNotesSettings(this IServiceCollection services)
    {
        services.AddScoped<ISettingsSection, NotesSettingsSection>();
        return services;
    }
}

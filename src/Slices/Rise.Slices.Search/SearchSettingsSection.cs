using Microsoft.Extensions.DependencyInjection;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Search;

/// <summary>
/// The Search slice's fulfillment of the Settings contract: the
/// search-engine picker, plus the DI registration the shell calls.
/// </summary>
public sealed class SearchSettingsSection : ISettingsSection
{
    public string Title => "Search";
    public int Order => 105;
    public string Description => "Which search engine the search box and the command palette use.";
    public Type ComponentType => typeof(SearchSettings);
}

/// <summary>Shell wiring for the Search slice's Settings contribution.</summary>
public static class SearchSettingsExtensions
{
    public static IServiceCollection AddSearchSettings(this IServiceCollection services)
    {
        services.AddScoped<ISettingsSection, SearchSettingsSection>();
        return services;
    }
}

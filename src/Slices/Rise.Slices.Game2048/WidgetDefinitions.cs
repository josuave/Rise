using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Game2048;

/// <summary>
/// The 2048 slice's widget definition: the game as a page widget,
/// hidden by default and shown from the layout editor.
/// </summary>
public sealed class Game2048WidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Game2048";

    public string Id => WidgetId;

    public string Title => "2048";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order => 38;

    public Type ComponentType => typeof(Game2048Widget);
}

/// <summary>
/// Shell wiring for the 2048 slice's widget, kept for tests: in the
/// app itself 2048 is a lazy module, so the shell registers its lazy
/// definition instead and this extension is not called.
/// </summary>
public static class Game2048WidgetExtensions
{
    public static IServiceCollection AddGame2048Widgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, Game2048WidgetDefinition>();
        return services;
    }
}

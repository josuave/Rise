using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Todo;

/// <summary>
/// The to-do slice's widget definition: a minimal to-do list (add,
/// toggle, delete, clear completed) as a page widget, hidden by
/// default and shown from the layout editor.
/// </summary>
public sealed class TodoWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Todo";

    public string Id => WidgetId;

    public string Title => "Todo";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order => 36;

    public Type ComponentType => typeof(TodoWidget);
}

/// <summary>Shell wiring for the to-do slice's widget.</summary>
public static class TodoWidgetExtensions
{
    public static IServiceCollection AddTodoWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, TodoWidgetDefinition>();
        return services;
    }
}

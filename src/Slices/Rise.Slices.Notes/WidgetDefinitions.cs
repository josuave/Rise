using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Slices.Notes;

/// <summary>
/// The Notes slice's widget definition: a list of plain-text notes
/// (add, edit, delete) as a page widget, hidden by default and shown
/// from the layout editor. Deliberately no markdown editor.
/// </summary>
public sealed class NotesWidgetDefinition : IWidgetDefinition
{
    public const string WidgetId = "Notes";

    public string Id => WidgetId;

    public string Title => "Notes";

    public WidgetZone Zone => WidgetZone.Main;

    public bool DefaultVisible => false;

    public int Order => 37;

    public Type ComponentType => typeof(NotesWidget);
}

/// <summary>
/// Shell wiring for the Notes slice's widget, kept for tests: in the
/// app itself Notes is a lazy module, so the shell registers its lazy
/// definition instead and this extension is not called.
/// </summary>
public static class NotesWidgetExtensions
{
    public static IServiceCollection AddNotesWidgets(this IServiceCollection services)
    {
        services.AddSingleton<IWidgetDefinition, NotesWidgetDefinition>();
        return services;
    }
}

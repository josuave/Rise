namespace Rise.Application.Ports;

/// <summary>
/// Opt-in extension of a widget definition: the widget has settings
/// the layout editor can show. The config component is slice-owned
/// (like the widget itself) and loads and saves its widget's config
/// through the widget-config service; the layout editor only renders
/// it, never knows what it edits. Slices opt in by implementing this
/// instead of <see cref="IWidgetDefinition"/>, so the base contract
/// never changes for a new configurable widget.
/// </summary>
public interface IConfigurableWidgetDefinition : IWidgetDefinition
{
    /// <summary>The component the layout editor renders for this widget's settings.</summary>
    Type ConfigComponentType { get; }
}

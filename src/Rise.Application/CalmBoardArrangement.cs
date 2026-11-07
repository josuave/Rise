namespace Rise.Application;

/// <summary>
/// The full Calm Board (mock-up D) arrangement: which widgets the
/// board shows, which it hides, and which sit in the calm top above
/// the board. The ids are opaque strings — Application never
/// references slice definition types — and the order a widget takes
/// is its definition Order at apply time (see
/// <see cref="Services.LayoutService.ApplyCalmBoardArrangementAsync"/>),
/// so a slice that renumbers its Order moves the widget here too.
/// The flag itself never rewrites a stored layout: this arrangement
/// applies only when the user asks for it from the Edit board bar.
/// </summary>
public static class CalmBoardArrangement
{
    /// <summary>The D top: Date, Clock, Greeting, Search — full-width unboxed rows above the board, in layout order.</summary>
    public static IReadOnlyList<string> TopIds { get; } = new[]
    {
        "Date",
        "Clock",
        "Greeting",
        "Search",
    };

    /// <summary>Every widget the D board shows, top and board alike.</summary>
    public static IReadOnlyList<string> VisibleIds { get; } = new[]
    {
        "Date",
        "Clock",
        "Greeting",
        "Weather",
        "Search",
        "Links",
        "Todo",
        "Notes",
    };

    /// <summary>Widgets the D board hides: Backup and the group.</summary>
    public static IReadOnlyList<string> HiddenIds { get; } = new[]
    {
        "Backup",
        "Group",
    };

    /// <summary>All ids the arrangement places, visible and hidden.</summary>
    public static IReadOnlyList<string> AllIds { get; } = VisibleIds.Concat(HiddenIds).ToList();
}

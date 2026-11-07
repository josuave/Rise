namespace Rise.Domain;

/// <summary>One preview feature: an upcoming piece of Rise, off by default, that the user opts into from Settings.</summary>
public sealed record PreviewFeature(string Id, string Title, string Description);

/// <summary>
/// The preview-feature registry: every feature the manager knows.
/// A feature joins by adding one entry here; the service, the
/// Settings section, and backup all read this list, so nothing else
/// needs to know the flags exist.
/// </summary>
public static class PreviewFeatures
{
    /// <summary>The Calm Board start-page layout (mock-up D).</summary>
    public const string CalmBoard = "calm-board";

    /// <summary>The Calm Board layout, only when the viewport is landscape.</summary>
    public const string CalmBoardLandscape = "calm-board-landscape";

    /// <summary>The full Calm Board layout (mock-up D): the D arrangement, Edit board bar, and add-widget slot.</summary>
    public const string CalmBoardFull = "calm-board-full";

    /// <summary>Settings redrawn in the Calm Board style: sections as warm-paper cards in a grid.</summary>
    public const string CalmSettings = "calm-settings";

    public static IReadOnlyList<PreviewFeature> All { get; } = new[]
    {
        new PreviewFeature(
            CalmBoard,
            "Calm Board layout",
            "The Calm Board start-page layout: the board treatment with a hero clock. Preview it before it becomes the default."),
        new PreviewFeature(
            CalmBoardLandscape,
            "Calm Board layout (landscape only)",
            "The board treatment, only when the screen is landscape — rotate a phone to see it. The stack stays in portrait."),
        new PreviewFeature(
            CalmBoardFull,
            "Full Calm Board layout",
            "The full Calm Board: the D arrangement with the Edit board bar and an add-widget slot. The board treatment on its own stays under 'Calm Board layout'."),
        new PreviewFeature(
            CalmSettings,
            "Calm Board settings",
            "Settings redrawn in the Calm Board style: sections as warm-paper cards in a grid, grouped by area."),
    };
}
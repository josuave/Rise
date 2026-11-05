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
    };
}

namespace Rise.Domain;

/// <summary>How the links on a start page are ordered for display. Stored order is never rewritten by sorting.</summary>
public enum LinkSort
{
    /// <summary>Stored order, as the user arranged it.</summary>
    Custom,

    /// <summary>Most-clicked first.</summary>
    Frequency,

    /// <summary>Name order, ordinal ignoring case.</summary>
    Alphabetical,

    /// <summary>Most recently added first.</summary>
    DateAdded,
}

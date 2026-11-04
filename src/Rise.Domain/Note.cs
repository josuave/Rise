namespace Rise.Domain;

/// <summary>A plain-text note. An entity: it has an identity (Id) and can be edited or removed.</summary>
public sealed record Note(Guid Id, string Text)
{
    public static Note Create(string text) => new(Guid.NewGuid(), text.Trim());
}

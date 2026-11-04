namespace Rise.Domain;

/// <summary>A to-do item: text and a done flag. An entity: it has an identity (Id) and can be toggled or removed.</summary>
public sealed record TodoItem(Guid Id, string Text, bool Done)
{
    public static TodoItem Create(string text) => new(Guid.NewGuid(), text.Trim(), false);

    public TodoItem Toggle() => this with { Done = !Done };
}

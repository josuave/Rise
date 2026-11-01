namespace Rise.Domain;

/// <summary>One saved arrangement: a name over a widget layout.</summary>
public sealed record NamedLayout(string Id, string Name, PageLayout Layout);

/// <summary>
/// A set of saved layouts with one active. The default layout always
/// exists and can never be deleted; deleting the active layout falls
/// back to it. Names are unique (case-insensitively) — saving or
/// renaming onto a taken name appends " 2", " 3", and so on. Blank
/// names are ignored. Layouts themselves are pure placement lists, so
/// none of this knows anything about concrete widgets.
/// </summary>
public sealed record LayoutCollection(string ActiveId, IReadOnlyList<NamedLayout> Layouts)
{
    public const string DefaultId = "default";

    public const string DefaultName = "Default";

    public NamedLayout? Active => Layouts.FirstOrDefault(l => l.Id == ActiveId);

    public LayoutCollection SetActive(string id) =>
        Layouts.Any(l => l.Id == id) ? new LayoutCollection(id, Layouts) : this;

    public LayoutCollection SaveAs(string name, PageLayout layout)
    {
        string trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return this;
        }

        string unique = UniqueName(trimmed, Layouts.Select(l => l.Name));
        string id = Guid.NewGuid().ToString();
        return new LayoutCollection(id, Layouts.Append(new NamedLayout(id, unique, layout)).ToList());
    }

    public LayoutCollection Rename(string id, string name)
    {
        string trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return this;
        }

        string unique = UniqueName(trimmed, Layouts.Where(l => l.Id != id).Select(l => l.Name));
        var next = Layouts
            .Select(l => l.Id == id ? new NamedLayout(l.Id, unique, l.Layout) : l)
            .ToList();
        return new LayoutCollection(ActiveId, next);
    }

    public LayoutCollection Delete(string id)
    {
        if (id == DefaultId || !Layouts.Any(l => l.Id == id))
        {
            return this;
        }

        var next = Layouts.Where(l => l.Id != id).ToList();
        string active = ActiveId == id ? DefaultId : ActiveId;
        return new LayoutCollection(active, next);
    }

    public LayoutCollection UpdateActive(PageLayout layout)
    {
        var next = Layouts
            .Select(l => l.Id == ActiveId ? new NamedLayout(l.Id, l.Name, layout) : l)
            .ToList();
        return new LayoutCollection(ActiveId, next);
    }

    private static string UniqueName(string name, IEnumerable<string> existing)
    {
        var taken = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(name))
        {
            return name;
        }

        int suffix = 2;
        while (taken.Contains($"{name} {suffix}"))
        {
            suffix++;
        }

        return $"{name} {suffix}";
    }
}

using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: how a note list is persisted. The core knows nothing about localStorage.</summary>
public interface INoteStore
{
    Task<NoteList?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(NoteList list, CancellationToken cancellationToken = default);
}

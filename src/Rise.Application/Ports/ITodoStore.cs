using Rise.Domain;

namespace Rise.Application.Ports;

/// <summary>Outbound port: how a to-do list is persisted. The core knows nothing about localStorage.</summary>
public interface ITodoStore
{
    Task<TodoList?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TodoList list, CancellationToken cancellationToken = default);
}

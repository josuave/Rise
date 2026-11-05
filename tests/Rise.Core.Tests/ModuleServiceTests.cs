using Rise.Application.Ports;
using Rise.Application.Services;
using Xunit;

namespace Rise.Core.Tests;

public class ModuleServiceTests
{
    private sealed class FakeModuleStore : IModuleStore
    {
        public Dictionary<string, bool> Stored { get; } = new();

        public Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, bool>(Stored));

        public Task SetAsync(string moduleId, bool enabled, CancellationToken cancellationToken = default)
        {
            Stored[moduleId] = enabled;
            return Task.CompletedTask;
        }

        public Task SaveAllAsync(Dictionary<string, bool> choices, CancellationToken cancellationToken = default)
        {
            Stored.Clear();
            foreach (KeyValuePair<string, bool> pair in choices)
            {
                Stored[pair.Key] = pair.Value;
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task IsEnabledAsync_NoStoredChoice_ReturnsTrue()
    {
        var service = new ModuleService(new FakeModuleStore());

        Assert.True(await service.IsEnabledAsync("Notes"));
    }

    [Fact]
    public async Task SetEnabledAsync_False_DisablesAndPersists()
    {
        var store = new FakeModuleStore();
        var service = new ModuleService(store);

        await service.SetEnabledAsync("Notes", false);

        Assert.False(await service.IsEnabledAsync("Notes"));
        Assert.True(store.Stored.TryGetValue("Notes", out bool enabled) && !enabled);
    }

    [Fact]
    public async Task SetEnabledAsync_RaisesChanged()
    {
        var service = new ModuleService(new FakeModuleStore());
        bool raised = false;
        service.Changed += (_, _) => raised = true;

        await service.SetEnabledAsync("Notes", false);

        Assert.True(raised);
    }
}

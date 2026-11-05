using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class PreviewFeatureServiceTests
{
    private sealed class FakePreviewFeatureStore : IPreviewFeatureStore
    {
        public Dictionary<string, bool> Stored { get; } = new();

        public Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, bool>(Stored));

        public Task SetAsync(string featureId, bool enabled, CancellationToken cancellationToken = default)
        {
            Stored[featureId] = enabled;
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
    public async Task IsEnabled_DefaultsToFalse()
    {
        var service = new PreviewFeatureService(new FakePreviewFeatureStore());

        Assert.False(await service.IsEnabledAsync(PreviewFeatures.CalmBoard));
    }

    [Fact]
    public async Task SetEnabled_PersistsAndMatches()
    {
        var store = new FakePreviewFeatureStore();
        var service = new PreviewFeatureService(store);

        await service.SetEnabledAsync(PreviewFeatures.CalmBoard, true);

        Assert.True(await service.IsEnabledAsync(PreviewFeatures.CalmBoard));
        Assert.True(store.Stored[PreviewFeatures.CalmBoard]);

        await service.SetEnabledAsync(PreviewFeatures.CalmBoard, false);

        Assert.False(await service.IsEnabledAsync(PreviewFeatures.CalmBoard));
    }

    [Fact]
    public async Task GetAll_ListsRegistryFeaturesWithStates()
    {
        var store = new FakePreviewFeatureStore();
        var service = new PreviewFeatureService(store);

        IReadOnlyList<(PreviewFeature Feature, bool Enabled)> before = await service.GetAllAsync();

        Assert.Contains(before, pair => pair.Feature.Id == PreviewFeatures.CalmBoard && !pair.Enabled);

        await service.SetEnabledAsync(PreviewFeatures.CalmBoard, true);

        IReadOnlyList<(PreviewFeature Feature, bool Enabled)> after = await service.GetAllAsync();

        Assert.Contains(after, pair => pair.Feature.Id == PreviewFeatures.CalmBoard && pair.Enabled);
    }

    [Fact]
    public async Task GetAll_IgnoresUnknownStoredIds()
    {
        var store = new FakePreviewFeatureStore();
        store.Stored["removed-flag"] = true;
        var service = new PreviewFeatureService(store);

        IReadOnlyList<(PreviewFeature Feature, bool Enabled)> all = await service.GetAllAsync();

        Assert.DoesNotContain(all, pair => pair.Feature.Id == "removed-flag");
    }

    [Fact]
    public async Task SetEnabled_RaisesChanged()
    {
        var service = new PreviewFeatureService(new FakePreviewFeatureStore());
        int raised = 0;
        service.Changed += (_, _) => raised++;

        await service.SetEnabledAsync(PreviewFeatures.CalmBoard, true);

        Assert.Equal(1, raised);
    }
}

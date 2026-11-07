using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class HotkeyServiceTests
{
    private sealed class FakeHotkeyStore : IHotkeyStore
    {
        public Dictionary<string, HotkeyBinding> Stored { get; } = new();

        public Task<Dictionary<string, HotkeyBinding>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<string, HotkeyBinding>(Stored));

        public Task SetAsync(string actionId, HotkeyBinding binding, CancellationToken cancellationToken = default)
        {
            Stored[actionId] = binding;
            return Task.CompletedTask;
        }

        public Task SaveAllAsync(Dictionary<string, HotkeyBinding> bindings, CancellationToken cancellationToken = default)
        {
            Stored.Clear();
            foreach (KeyValuePair<string, HotkeyBinding> pair in bindings)
            {
                Stored[pair.Key] = pair.Value;
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Load_Defaults_WhenNothingStored()
    {
        var service = new HotkeyService(new FakeHotkeyStore());

        Dictionary<string, HotkeyBinding> bindings = await service.LoadAsync();

        Assert.Equal(new HotkeyBinding("k", Mod: true, Alt: false, Shift: false), bindings["open-palette"]);
        Assert.Equal(new HotkeyBinding("e", Mod: false, Alt: false, Shift: false), bindings["toggle-edit-layout"]);
        Assert.Equal(new HotkeyBinding("t", Mod: false, Alt: false, Shift: false), bindings["toggle-theme"]);
    }

    [Fact]
    public async Task Load_StoredOverride_Wins_AndUnknownIdsIgnored()
    {
        var store = new FakeHotkeyStore();
        store.Stored["toggle-theme"] = new HotkeyBinding("y", Mod: false, Alt: false, Shift: false);
        store.Stored["not-an-action"] = new HotkeyBinding("z", Mod: false, Alt: false, Shift: false);
        var service = new HotkeyService(store);

        Dictionary<string, HotkeyBinding> bindings = await service.LoadAsync();

        Assert.Equal("y", bindings["toggle-theme"].Key);
        Assert.DoesNotContain("not-an-action", bindings.Keys);
    }

    [Fact]
    public async Task Set_ConflictingBinding_Fails_WithOwnerTitle()
    {
        var service = new HotkeyService(new FakeHotkeyStore());

        Result<HotkeyBinding> result = await service.SetAsync(
            "toggle-theme", new HotkeyBinding("e", Mod: false, Alt: false, Shift: false));

        Assert.Contains("Toggle edit layout", ResultAssertions.RequireError(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Set_NewBinding_Persists_AndMatches()
    {
        var store = new FakeHotkeyStore();
        var service = new HotkeyService(store);

        Result<HotkeyBinding> result = await service.SetAsync(
            "toggle-theme", new HotkeyBinding("y", Mod: false, Alt: false, Shift: false));

        _ = ResultAssertions.RequireOk(result);
        Assert.Equal("y", store.Stored["toggle-theme"].Key);
        Assert.Equal("toggle-theme", service.TryMatch("y", mod: false, alt: false, shift: false));
    }

    [Fact]
    public async Task TryMatch_IsCaseInsensitive_OnKey_ButExact_OnModifiers()
    {
        var service = new HotkeyService(new FakeHotkeyStore());
        await service.LoadAsync();

        Assert.Equal("toggle-edit-layout", service.TryMatch("E", mod: false, alt: false, shift: false));
        Assert.Null(service.TryMatch("e", mod: false, alt: false, shift: true));
        Assert.Null(service.TryMatch("e", mod: true, alt: false, shift: false));
        Assert.Equal("open-palette", service.TryMatch("K", mod: true, alt: false, shift: false));
    }

    [Fact]
    public async Task Reset_RestoresDefaults_AndClearsStored()
    {
        var store = new FakeHotkeyStore();
        var service = new HotkeyService(store);
        await service.SetAsync("toggle-theme", new HotkeyBinding("y", Mod: false, Alt: false, Shift: false));

        await service.ResetAsync();

        Assert.Empty(store.Stored);
        Assert.Equal("t", (await service.LoadAsync())["toggle-theme"].Key);
    }
}

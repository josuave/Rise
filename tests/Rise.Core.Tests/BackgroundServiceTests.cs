using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class BackgroundServiceTests
{
    private sealed class FakeBackgroundStore : IBackgroundStore
    {
        public BackgroundChoice? Stored { get; private set; }

        public Task<BackgroundChoice?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task SaveAsync(BackgroundChoice choice, CancellationToken cancellationToken = default)
        {
            Stored = choice;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBackgroundImageStore : IBackgroundImageStore
    {
        public Dictionary<string, byte[]> Blobs { get; } = new();

        public bool FailSave { get; set; }

        public string? UrlFor { get; set; } = "blob:test";

        public Task<bool> SaveAsync(string hash, byte[] bytes, string mimeType, CancellationToken cancellationToken = default)
        {
            if (FailSave)
            {
                return Task.FromResult(false);
            }

            Blobs[hash] = bytes;
            return Task.FromResult(true);
        }

        public Task<string?> GetUrlAsync(string hash, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(Blobs.ContainsKey(hash) ? UrlFor : null);

        public Task DeleteAsync(string hash, CancellationToken cancellationToken = default)
        {
            Blobs.Remove(hash);
            return Task.CompletedTask;
        }

        public Task ClearUrlAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static BackgroundService ServiceWith(FakeBackgroundStore store, FakeBackgroundImageStore images) =>
        new(store, images);

    [Fact]
    public async Task GetChoice_DefaultsToNone()
    {
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), new FakeBackgroundImageStore());

        BackgroundChoice choice = await service.GetChoiceAsync();

        Assert.Equal(BackgroundPresets.None, choice.PresetId);
        Assert.Equal(string.Empty, choice.Css);
    }

    [Fact]
    public async Task SetPreset_AppliesKnownPreset()
    {
        var store = new FakeBackgroundStore();
        BackgroundService service = ServiceWith(store, new FakeBackgroundImageStore());

        Result<BackgroundChoice> result = await service.SetPresetAsync("dawn");

        Assert.True(result.IsSuccess);
        Assert.Equal("dawn", store.Stored?.PresetId);
        Assert.Contains("linear-gradient", result.Value?.Css ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetPreset_RejectsUnknownPreset()
    {
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), new FakeBackgroundImageStore());

        Result<BackgroundChoice> result = await service.SetPresetAsync("nope");

        Assert.False(result.IsSuccess);
        Assert.Equal("Unknown background.", result.Error);
    }

    [Fact]
    public async Task SetCustomUrl_AcceptsHttpsAddress()
    {
        var store = new FakeBackgroundStore();
        BackgroundService service = ServiceWith(store, new FakeBackgroundImageStore());

        Result<BackgroundChoice> result = await service.SetCustomUrlAsync("https://example.com/photo.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundPresets.Custom, store.Stored?.PresetId);
        Assert.Equal("url('https://example.com/photo.jpg')", result.Value?.Css);
    }

    [Fact]
    public async Task SetCustomUrl_RejectsInvalidAddresses()
    {
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), new FakeBackgroundImageStore());

        Assert.False((await service.SetCustomUrlAsync("not a url")).IsSuccess);
        Assert.False((await service.SetCustomUrlAsync("example.com/photo.jpg")).IsSuccess);
        Assert.False((await service.SetCustomUrlAsync("ftp://example.com/photo.jpg")).IsSuccess);
        Assert.False((await service.SetCustomUrlAsync("https://example.com/ph\"oto.jpg")).IsSuccess);
        Assert.False((await service.SetCustomUrlAsync(null)).IsSuccess);
    }

    [Fact]
    public async Task Clear_ResetsToNone()
    {
        var store = new FakeBackgroundStore();
        BackgroundService service = ServiceWith(store, new FakeBackgroundImageStore());
        await service.SetPresetAsync("dusk");

        await service.ClearAsync();

        Assert.Equal(BackgroundPresets.None, store.Stored?.PresetId);
        Assert.Equal(string.Empty, (await service.GetChoiceAsync()).Css);
    }

    [Fact]
    public async Task SetPreset_RaisesChanged()
    {
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), new FakeBackgroundImageStore());
        int raised = 0;
        service.Changed += (_, _) => raised++;

        await service.SetPresetAsync("mist");

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetUpload_StoresBytesAndDescriptor()
    {
        var store = new FakeBackgroundStore();
        var images = new FakeBackgroundImageStore();
        BackgroundService service = ServiceWith(store, images);
        var image = new UploadedImage(new string('A', 64), "sunset.jpg", 12, "image/jpeg");

        Result<BackgroundChoice> result = await service.SetUploadAsync(image, new byte[] { 1, 2, 3 });

        Assert.True(result.IsSuccess);
        Assert.Equal(BackgroundPresets.Upload, store.Stored?.PresetId);
        Assert.Equal("sunset.jpg", store.Stored?.Upload?.FileName);
        Assert.True(images.Blobs.ContainsKey(new string('A', 64)));
    }

    [Fact]
    public async Task SetUpload_RejectsBadDescriptor()
    {
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), new FakeBackgroundImageStore());
        byte[] bytes = new byte[] { 1 };

        Assert.False((await service.SetUploadAsync(new UploadedImage("short", "a.jpg", 1, "image/jpeg"), bytes)).IsSuccess);
        Assert.False((await service.SetUploadAsync(new UploadedImage(new string('A', 64), "", 1, "image/jpeg"), bytes)).IsSuccess);
        Assert.False((await service.SetUploadAsync(new UploadedImage(new string('A', 64), "a.jpg", 0, "image/jpeg"), bytes)).IsSuccess);
        Assert.False((await service.SetUploadAsync(new UploadedImage(new string('A', 64), "a.jpg", 1, "text/plain"), bytes)).IsSuccess);
    }

    [Fact]
    public async Task SetUpload_WhenStoreRefuses_LeavesChoiceUntouched()
    {
        var store = new FakeBackgroundStore();
        var images = new FakeBackgroundImageStore { FailSave = true };
        BackgroundService service = ServiceWith(store, images);

        Result<BackgroundChoice> result = await service.SetUploadAsync(new UploadedImage(new string('A', 64), "a.jpg", 1, "image/jpeg"), new byte[] { 1 });

        Assert.False(result.IsSuccess);
        Assert.Null(store.Stored);
    }

    [Fact]
    public async Task SetUpload_ReplacingDeletesOldBytes()
    {
        var images = new FakeBackgroundImageStore();
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), images);
        await service.SetUploadAsync(new UploadedImage(new string('A', 64), "a.jpg", 1, "image/jpeg"), new byte[] { 1 });

        await service.SetUploadAsync(new UploadedImage(new string('B', 64), "b.jpg", 1, "image/jpeg"), new byte[] { 2 });

        Assert.False(images.Blobs.ContainsKey(new string('A', 64)));
        Assert.True(images.Blobs.ContainsKey(new string('B', 64)));
    }

    [Fact]
    public async Task GetCss_Upload_ReturnsBlobUrl()
    {
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), new FakeBackgroundImageStore());
        await service.SetUploadAsync(new UploadedImage(new string('A', 64), "a.jpg", 1, "image/jpeg"), new byte[] { 1 });

        string css = await service.GetCssAsync();

        Assert.Equal("url('blob:test')", css);
        Assert.False(service.UploadMissing);
    }

    [Fact]
    public async Task GetCss_UploadMissing_ReturnsEmptyAndFlagsMissing()
    {
        var images = new FakeBackgroundImageStore();
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), images);
        await service.SetUploadAsync(new UploadedImage(new string('A', 64), "sunset.jpg", 1, "image/jpeg"), new byte[] { 1 });
        images.Blobs.Clear();

        string css = await service.GetCssAsync();

        Assert.Equal(string.Empty, css);
        Assert.True(service.UploadMissing);
        Assert.Equal("sunset.jpg", service.MissingFileName);
    }

    [Fact]
    public async Task SetPreset_AfterMissing_ClearsMissingFlag()
    {
        var images = new FakeBackgroundImageStore();
        BackgroundService service = ServiceWith(new FakeBackgroundStore(), images);
        await service.SetUploadAsync(new UploadedImage(new string('A', 64), "a.jpg", 1, "image/jpeg"), new byte[] { 1 });
        images.Blobs.Clear();
        _ = await service.GetCssAsync();
        Assert.True(service.UploadMissing);

        await service.SetPresetAsync("mist");

        Assert.False(service.UploadMissing);
        Assert.Null(service.MissingFileName);
    }
}

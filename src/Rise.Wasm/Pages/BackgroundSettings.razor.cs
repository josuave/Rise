using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Wasm.Pages;

/// <summary>
/// The Background settings section: one swatch per built-in preset,
/// a custom image address field, and an image upload. Choosing is
/// live — the page layer re-renders through the service's Changed
/// event, no restart. An upload is hashed and stored by that hash;
/// re-uploading the same file restores a missing image, while a
/// different file is flagged and only replaces the background on
/// an explicit "use this image instead".
/// </summary>
[SuppressMessage("Design", "CA1515", Justification = "Razor generates the component's other partial as public; the class cannot be internal.")]
public partial class BackgroundSettings : IDisposable
{
    private const long MaxImageBytes = 10 * 1024 * 1024;

    [Inject]
    private BackgroundService BackgroundService { get; set; } = null!;

    private BackgroundChoice Choice { get; set; } = BackgroundChoice.None;

    private bool Loaded { get; set; }

    private string? CustomUrl { get; set; }

    private string? Status { get; set; }

    private (UploadedImage Image, byte[] Bytes)? PendingUpload { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
        Loaded = true;
        BackgroundService.Changed += OnBackgroundChanged;
    }

    private async void OnBackgroundChanged(object? sender, EventArgs args)
    {
        await RefreshAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshAsync()
    {
        _ = await BackgroundService.GetCssAsync();
        Choice = await BackgroundService.GetChoiceAsync();
        if (Choice is CustomBackground { Url: var url })
        {
            CustomUrl = url;
        }
    }

    private bool IsSelected(BackgroundPreset preset) => Choice is PresetBackground { Id: var id } && id == preset.Id;

    private async Task SelectPresetAsync(BackgroundPreset preset)
    {
        PendingUpload = null;
        Result<BackgroundChoice> result = await BackgroundService.SetPresetAsync(preset.Id);
        switch (result)
        {
            case Ok<BackgroundChoice>:
                Status = preset.Id == BackgroundPresets.None ? "Background off." : $"{preset.Title} background on.";
                break;
            case Error error:
                Status = error.Message;
                break;
            case null:
                Status = null;
                break;
        }
    }

    private async Task ApplyUrlAsync()
    {
        PendingUpload = null;
        Result<BackgroundChoice> result = await BackgroundService.SetCustomUrlAsync(CustomUrl);
        switch (result)
        {
            case Ok<BackgroundChoice>:
                Status = "Image applied.";
                break;
            case Error error:
                Status = error.Message;
                break;
            case null:
                Status = null;
                break;
        }
    }

    private async Task UploadAsync(InputFileChangeEventArgs args)
    {
        IBrowserFile file = args.File;
        if (!file.ContentType.StartsWith("image/", StringComparison.Ordinal))
        {
            Status = "That file isn't an image.";
            return;
        }

        if (file.Size > MaxImageBytes)
        {
            Status = "That image is over 10 MB.";
            return;
        }

        byte[] bytes;
        using (MemoryStream stream = new())
        {
            await file.OpenReadStream(MaxImageBytes).CopyToAsync(stream);
            bytes = stream.ToArray();
        }

        string hash = Convert.ToHexString(SHA256.HashData(bytes));
        var image = new UploadedImage(hash, file.Name, file.Size, file.ContentType);
        UploadedImage? currentImage = Choice.UploadImage;
        if (currentImage is not null && currentImage.Hash != hash)
        {
            PendingUpload = (image, bytes);
            Status = $"That's a different image than {currentImage.FileName}.";
            return;
        }

        PendingUpload = null;
        Result<BackgroundChoice> result = await BackgroundService.SetUploadAsync(image, bytes);
        switch (result)
        {
            case Ok<BackgroundChoice>:
                Status = "Image applied.";
                break;
            case Error error:
                Status = error.Message;
                break;
            case null:
                Status = null;
                break;
        }
    }

    private async Task UsePendingUploadAsync()
    {
        if (PendingUpload is null)
        {
            return;
        }

        (UploadedImage image, byte[] bytes) = PendingUpload.Value;
        PendingUpload = null;
        Result<BackgroundChoice> result = await BackgroundService.SetUploadAsync(image, bytes);
        switch (result)
        {
            case Ok<BackgroundChoice>:
                Status = "Image applied.";
                break;
            case Error error:
                Status = error.Message;
                break;
            case null:
                Status = null;
                break;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            BackgroundService.Changed -= OnBackgroundChanged;
        }
    }
}

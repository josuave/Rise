using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;

namespace Rise.Slices.Backup;

/// <summary>
/// Backup controls: export the current state, import a backup file
/// (replacing current state, then reloading the page), or migrate
/// an older backup to a chosen version and download the result
/// without touching this page. Rendered both as a Settings section
/// and as a page widget; the two hosts share this component.
/// </summary>
public partial class BackupControls
{
    private const long MaxFileSize = 10_000_000;

    [Inject]
    private BackupService BackupService { get; set; } = null!;

    [Inject]
    private IFileDownloader FileDownloader { get; set; } = null!;

    [Inject]
    private IClock Clock { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private string Status { get; set; } = string.Empty;

    private string? ImportJson { get; set; }

    private string? MigrateJson { get; set; }

    private BackupEnvelope? MigrateEnvelope { get; set; }

    private int MigrateTargetVersion { get; set; } = BackupFormat.CurrentVersion;

    private async Task ExportAsync()
    {
        try
        {
            string json = await BackupService.ExportAsync();
            string filename = $"rise-backup-{Clock.Now:yyyy-MM-dd}.json";
            await FileDownloader.DownloadAsync(filename, json);
            Status = "Exported.";
        }
        catch (InvalidOperationException)
        {
            Status = "The download couldn't be started.";
        }
    }

    private async Task ImportFileSelected(InputFileChangeEventArgs args)
    {
        ImportJson = await ReadFileAsync(args.File);
        Status = ImportJson is null ? "That file couldn't be read." : $"Selected {args.File.Name} for import.";
    }

    private async Task ImportAsync()
    {
        if (ImportJson is null)
        {
            return;
        }

        Result<BackupEnvelope> result = await BackupService.ImportAsync(ImportJson);
        switch (result)
        {
            case Ok<BackupEnvelope> ok:
                Status = $"Imported a v{ok.Value.FormatVersion} backup. Reloading…";
                Navigation.NavigateTo("/", forceLoad: true);
                break;
            case Error error:
                Status = error.Message;
                break;
            case null:
                Status = "That backup couldn't be imported.";
                break;
        }
    }

    private async Task MigrateFileSelected(InputFileChangeEventArgs args)
    {
        MigrateJson = await ReadFileAsync(args.File);
        if (MigrateJson is null)
        {
            MigrateEnvelope = null;
            Status = "That file couldn't be read.";
            return;
        }

        Result<BackupEnvelope> parsed = BackupFormat.Parse(MigrateJson);
        switch (parsed)
        {
            case Ok<BackupEnvelope> ok:
                MigrateEnvelope = ok.Value;
                MigrateTargetVersion = BackupFormat.CurrentVersion;
                Status = $"Selected {args.File.Name} (v{MigrateEnvelope.FormatVersion}).";
                break;
            case Error error:
                MigrateEnvelope = null;
                Status = error.Message;
                break;
            case null:
                MigrateEnvelope = null;
                Status = "That file couldn't be read.";
                break;
        }
    }

    private async Task MigrateAsync()
    {
        if (MigrateEnvelope is null)
        {
            return;
        }

        Result<BackupEnvelope> migrated = BackupFormat.Migrate(MigrateEnvelope, MigrateTargetVersion);
        BackupEnvelope envelope;
        switch (migrated)
        {
            case Ok<BackupEnvelope> ok:
                envelope = ok.Value;
                break;
            case Error error:
                Status = error.Message;
                return;
            case null:
                Status = "That backup couldn't be migrated.";
                return;
        }

        try
        {
            string filename = $"rise-backup-v{MigrateTargetVersion}-{Clock.Now:yyyy-MM-dd}.json";
            await FileDownloader.DownloadAsync(filename, BackupFormat.Serialize(envelope));
            Status = $"Migrated v{MigrateEnvelope.FormatVersion} to v{MigrateTargetVersion} and downloaded it. This page is unchanged.";
        }
        catch (InvalidOperationException)
        {
            Status = "The download couldn't be started.";
        }
    }

    private static async Task<string?> ReadFileAsync(IBrowserFile file)
    {
        if (file.Size > MaxFileSize)
        {
            return null;
        }

        try
        {
            await using Stream stream = file.OpenReadStream(MaxFileSize);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return null;
        }
    }
}

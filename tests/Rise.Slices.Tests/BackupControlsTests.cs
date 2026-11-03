using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rise.Application.Ports;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Backup;
using Xunit;

namespace Rise.Slices.Tests;

public class BackupControlsTests : BunitContext
{
    private readonly FakeFileDownloader _downloader = new();

    public BackupControlsTests()
    {
        Services.AddScoped<ILinkStore>(_ => new FakeLinkStore());
        Services.AddScoped<IThemeStore>(_ => new FakeThemeStore());
        Services.AddScoped<ICustomThemeStore>(_ => new FakeCustomThemeStore());
        Services.AddScoped<ILayoutCollectionStore>(_ => new FakeLayoutCollectionStore());
        Services.AddScoped<IWidgetConfigStore>(_ => new FakeWidgetConfigStore());
        Services.AddScoped<StartPageService>();
        Services.AddScoped<CustomThemeService>();
        Services.AddScoped<LayoutService>();
        Services.AddScoped<BackupService>();
        Services.AddScoped<IFileDownloader>(_ => _downloader);
        Services.AddSingleton<IClock>(_ => new FakeClock(new DateTime(2026, 11, 2, 18, 0, 0)));
    }

    [Fact]
    public void RendersExportImportAndMigrateSections()
    {
        IRenderedComponent<BackupControls> cut = Render<BackupControls>();

        Assert.Contains("Export state", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Import", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Migrate", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_DownloadsCurrentVersionEnvelope()
    {
        IRenderedComponent<BackupControls> cut = Render<BackupControls>();

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Export state").Click();

        cut.WaitForAssertion(() => Assert.Single(_downloader.Downloads));
        Assert.StartsWith("rise-backup-2026-11-02", _downloader.Downloads[0].Filename, StringComparison.Ordinal);
        Assert.Contains("\"FormatVersion\": 1", _downloader.Downloads[0].Content, StringComparison.Ordinal);
    }
}

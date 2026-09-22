using Emignatik.NxFileViewer.FileLoading;
using Emignatik.NxFileViewer.Services.Integrity;
using Emignatik.NxFileViewer.Services.KeysManagement;
using Emignatik.NxFileViewer.Settings;
using Microsoft.Extensions.Logging;

namespace NxFileViewer.Ava.Services;

public sealed class AppServices
{
    public static AppServices Current { get; } = new();

    private ILoggerFactory LoggerFactory { get; }

    public IFileLoadingSettings FileLoadingSettings { get; }

    public IKeySetProviderService KeySetProvider { get; }

    private IFileLoader FileLoader { get; }

    public FileOpeningService FileOpening { get; }

    public INcasIntegrityVerifier IntegrityVerifier { get; }

    public TreeItemExportService TreeItemExport { get; }

    private AppServices()
    {
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddDebug();
        });

        FileLoadingSettings = new ConfigFileLoadingSettings();
        KeySetProvider = new KeySetProviderService(FileLoadingSettings, LoggerFactory);

        var packageTypeAnalyzer = new PackageTypeAnalyzer(LoggerFactory);
        var fileItemLoader = new FileItemLoader(KeySetProvider, LoggerFactory, FileLoadingSettings);
        var fileOverviewLoader = new FileOverviewLoader(LoggerFactory);
        FileLoader = new FileLoader(LoggerFactory, packageTypeAnalyzer, fileItemLoader, fileOverviewLoader);
        FileOpening = new FileOpeningService(FileLoader, LoggerFactory);

        var ncaHashService = new NcaHashService();
        var ncaItemIntegrityService = new NcaItemIntegrityService(ncaHashService, LoggerFactory.CreateLogger<NcaItemIntegrityService>());
        IntegrityVerifier = new NcasIntegrityVerifier(FileLoadingSettings, ncaItemIntegrityService, LoggerFactory.CreateLogger<NcasIntegrityVerifier>());
        TreeItemExport = new TreeItemExportService(FileLoadingSettings);
    }
}

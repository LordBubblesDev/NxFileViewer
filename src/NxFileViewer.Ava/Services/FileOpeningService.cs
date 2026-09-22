using Emignatik.NxFileViewer.FileLoading;
using Emignatik.NxFileViewer.Models;
using Microsoft.Extensions.Logging;
using NxFileViewer.Ava.Localization;

namespace NxFileViewer.Ava.Services;

public sealed class FileOpeningService(IFileLoader fileLoader, ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<FileOpeningService>();

    public async Task<NxFile?> SafeOpenFile(string filePath)
    {
        try {
            Config.Shared.LastOpenedFile = filePath;
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) {
                Config.Shared.LastUsedDir = directory;
            }

            AppStatus.Set(Locale[FvLocale.Status_LoadingFile], "fa-regular fa-hourglass", StatusType.Working);

            var nxFile = await Task.Run(() => fileLoader.Load(filePath)).ConfigureAwait(true);

            AppStatus.SetTemporaryShort(Locale[FvLocale.Status_FileOpened]);
            return nxFile;
        }
        catch (NotSupportedException) {
            var message = Locale[FvLocale.FileNotSupported_Log, filePath];
            _logger.LogError(message);
            AppStatus.SetTemporaryLong(message, "fa-regular fa-circle-xmark");
            return null;
        }
        catch (Exception ex) {
            var message = Locale[FvLocale.LoadingError_Failed, filePath, ex.Message];
            _logger.LogError(ex, message);
            AppStatus.SetTemporaryLong(message, "fa-regular fa-circle-xmark");
            throw;
        }
    }
}

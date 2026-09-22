using System;
using System.ComponentModel;
using System.IO;
using Emignatik.NxFileViewer.Localization;
using Emignatik.NxFileViewer.Services.BackgroundTask;
using Emignatik.NxFileViewer.Settings;
using Emignatik.NxFileViewer.Utils;
using Emignatik.NxFileViewer.Utils.MVVM;
using LibHac.Common.Keys;
using Microsoft.Extensions.Logging;

namespace Emignatik.NxFileViewer.Services.KeysManagement;

public class KeySetProviderService : NotifyPropertyChangedBase, IKeySetProviderService
{


    private readonly object _lock = new();
    private readonly IFileLoadingSettings _appSettings;
    private KeySet? _keySet;

    private readonly ILogger _logger;
    private string? _actualProdKeysFilePath;
    private string? _actualTitleKeysFilePath;

    public KeySetProviderService(IFileLoadingSettings appSettings, ILoggerFactory loggerFactory)
    {
        _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger(this.GetType());
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        AppDirProdKeysFilePath = Path.Combine(PathHelper.CurrentAppDir, IKeySetProviderService.DefaultProdKeysFileName);
        AppDirTitleKeysFilePath = Path.Combine(PathHelper.CurrentAppDir, IKeySetProviderService.DefaultTitleKeysFileName);

        Reset();

        appSettings.PropertyChanged += OnSettingChanged;
    }

    public string AppDirProdKeysFilePath { get; }

    public string AppDirTitleKeysFilePath { get; }

    public string? ActualProdKeysFilePath
    {
        get => _actualProdKeysFilePath;
        private set
        {
            _actualProdKeysFilePath = value;
            NotifyPropertyChanged();
        }
    }

    public string? ActualTitleKeysFilePath
    {
        get => _actualTitleKeysFilePath;
        private set
        {
            _actualTitleKeysFilePath = value;
            NotifyPropertyChanged();
        }
    }

    public KeySet GetKeySet(bool forceReload = false)
    {
        lock (_lock) {
            if (forceReload) {
                UnloadCurrentKeySet();
            }
            else if (_keySet != null) {
                return _keySet;
            }

            _keySet = LoadKeySet();
            return _keySet;
        }
    }

    private void OnSettingChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(IFileLoadingSettings.ProdKeysFilePath) ||
            args.PropertyName == nameof(IFileLoadingSettings.TitleKeysFilePath)) {
            Reset();
        }

    }

    public void Reset()
    {
        UnloadCurrentKeySet();
        UpdateActualProdKeysFilePath();
        UpdateActualTitleKeysFilePath();
    }

    private void UnloadCurrentKeySet()
    {
        lock (_lock) {
            _keySet = null;
        }
    }

    private KeySet LoadKeySet()
    {
        try {
            var actualProdKeysFilePath = ActualProdKeysFilePath;
            var actualTitleKeysFilePath = ActualTitleKeysFilePath;

            _logger.LogInformation(LoadingLocalizationKeys.KeysLoadingStartingLog);

            _logger.LogInformation(LoadingLocalizationKeys.KeysFileUsed.SafeFormat(IKeySetProviderService.DefaultProdKeysFileName, actualProdKeysFilePath ?? LoadingLocalizationKeys.NoneKeysFile));
            _logger.LogInformation(LoadingLocalizationKeys.KeysFileUsed.SafeFormat(IKeySetProviderService.DefaultTitleKeysFileName, actualTitleKeysFilePath ?? LoadingLocalizationKeys.NoneKeysFile));

            var keySet = KeySet.CreateDefaultKeySet();

            ExternalKeyReader.ReadKeyFile(keySet, filename: actualProdKeysFilePath, titleKeysFilename: actualTitleKeysFilePath, consoleKeysFilename: null,
                new LibHacProgressReportRelay(
                    _ => {},
                    message => { _logger.LogWarning(message); })
            );

            _logger.LogInformation(LoadingLocalizationKeys.KeysLoadingSuccessfulLog);

            return keySet;
        }
        catch (Exception ex) {
            Reset();
            throw new Exception(LoadingLocalizationKeys.KeysLoadingError.SafeFormat(ex.Message));
        }
    }

    private void UpdateActualProdKeysFilePath()
    {
        var findProdKeysFile = FindKeysFile(_appSettings.ProdKeysFilePath, IKeySetProviderService.DefaultProdKeysFileName);

        if (findProdKeysFile == null) {
            _logger.LogWarning(LoadingLocalizationKeys.WarnNoProdKeysFileFound);
        }

        ActualProdKeysFilePath = findProdKeysFile;
    }

    private void UpdateActualTitleKeysFilePath()
    {
        ActualTitleKeysFilePath = FindKeysFile(_appSettings.TitleKeysFilePath, IKeySetProviderService.DefaultTitleKeysFileName);
    }

    private string? FindKeysFile(string? keysFilePathRawFromSettings, string keysFileName)
    {

        // 1. Check from settings (if defined)
        if (!string.IsNullOrWhiteSpace(keysFilePathRawFromSettings)) {
            var keysFilePathTemp = keysFilePathRawFromSettings.ToFullPath();
            
            if (File.Exists(keysFilePathTemp)) {
                return keysFilePathTemp;
            }
            
            _logger.LogWarning(
                LoadingLocalizationKeys.InvalidSettingKeysFileNotFound.SafeFormat(
                    keysFilePathRawFromSettings));
        }

        // 2. Try to load from the current app dir
        var appDirKeysFilePath = Path.Combine(PathHelper.CurrentAppDir, keysFileName);
        
        if (File.Exists(appDirKeysFilePath)) {
            return appDirKeysFilePath;
        }

        // 3. Check from "userHomeDir/.switch" directory
        var homeUserDir = PathHelper.HomeUserDir;
        
        if (homeUserDir == null) {
            return null;
        }
        
        var homeDirKeysFilePath = Path.Combine(homeUserDir, ".switch", keysFileName).ToFullPath();
        return File.Exists(homeDirKeysFilePath) ? homeDirKeysFilePath : null;
    }
}

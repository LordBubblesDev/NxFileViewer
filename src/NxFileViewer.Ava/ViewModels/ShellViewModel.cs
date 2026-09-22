using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emignatik.NxFileViewer.Services.KeysManagement;
using FluentAvalonia.UI.Controls;
using LibHac.Ncm;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.Models;
using NxFileViewer.Ava.Services;

namespace NxFileViewer.Ava.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    public ShellViewModel()
    {
        Documents.CollectionChanged += OnDocumentsChanged;
        AppServices.Current.KeySetProvider.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(IKeySetProviderService.ActualProdKeysFilePath)) {
                OnPropertyChanged(nameof(NoProdKeysLoaded));
            }
        };
    }

    public IStorageProvider? StorageProvider { get; set; }

    public ObservableCollection<Document> Documents { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckSignatureCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckHashCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenTitleWebPageCommand))]
    public partial Document? CurrentDocument { get; set; }

    public bool HasDocuments => Documents.Count > 0;

    public bool NoProdKeysLoaded => AppServices.Current.KeySetProvider.ActualProdKeysFilePath == null;

    [RelayCommand]
    private void AddEmptyTab()
    {
        var empty = Documents.OfType<EmptyDocument>().FirstOrDefault();
        if (empty == null) {
            empty = new EmptyDocument(OpenFileCommand);
            Documents.Add(empty);
        }

        CurrentDocument = empty;
    }

    [RelayCommand]
    public void OpenSettings()
    {
        var existing = Documents.OfType<SettingsViewModel>().FirstOrDefault();
        if (existing == null) {
            existing = new SettingsViewModel();
            Documents.Add(existing);
        }

        CurrentDocument = existing;
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        if (StorageProvider == null) {
            return;
        }

        var options = new FilePickerOpenOptions
        {
            Title = Locale[FvLocale.OpenFile_Title],
            AllowMultiple = false,
            SuggestedStartLocation = await TryGetLastUsedFolder(),
            FileTypeFilter =
            [
                new FilePickerFileType(Locale[FvLocale.OpenFile_NintendoSwitchFiles])
                {
                    Patterns = ["*.nsp", "*.nsz", "*.xci", "*.xcz"]
                },
                FilePickerFileTypes.All
            ]
        };

        var files = await StorageProvider.OpenFilePickerAsync(options);
        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (!string.IsNullOrEmpty(path)) {
            await OpenFilePath(path);
        }
    }

    [RelayCommand]
    private async Task OpenPrevious()
    {
        var lastOpened = Config.Shared.LastOpenedFile;
        if (string.IsNullOrWhiteSpace(lastOpened) || !File.Exists(lastOpened)) {
            AppStatus.SetTemporaryLong(Locale[FvLocale.Status_NoPreviouslyOpenedFile], "fa-regular fa-circle-xmark");
            return;
        }

        await OpenFilePath(lastOpened);
    }

    [RelayCommand]
    private async Task Close()
    {
        if (CurrentDocument == null) {
            return;
        }

        if (await CurrentDocument.CloseRequested()) {
            RemoveDocument(CurrentDocument);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheckIntegrity))]
    private async Task CheckSignature()
    {
        if (CurrentDocument is OpenedFileDocument document) {
            await document.ViewModel.VerifySignatureCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheckIntegrity))]
    private async Task CheckHash()
    {
        if (CurrentDocument is OpenedFileDocument document) {
            await document.ViewModel.VerifyHashCommand.ExecuteAsync(null);
        }
    }

    private bool CanCheckIntegrity() => CurrentDocument is OpenedFileDocument;

    [RelayCommand]
    private void ReloadKeys()
    {
        try {
            AppServices.Current.KeySetProvider.GetKeySet(forceReload: true);
            OnPropertyChanged(nameof(NoProdKeysLoaded));
            AppStatus.SetTemporaryShort(Locale[FvLocale.Status_KeysReloaded], "fa-regular fa-key");
        }
        catch (Exception ex) {
            AppStatus.SetTemporaryLong(ex.Message, "fa-regular fa-circle-xmark");
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenTitleWebPage))]
    private void OpenTitleWebPage()
    {
        var titleId = GetOpenedTitleId();
        if (titleId == null) {
            return;
        }

        try {
            var url = Config.Shared.TitlePageUrl.Replace("{TitleId}", Uri.EscapeDataString(titleId), StringComparison.OrdinalIgnoreCase);
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch (Exception ex) {
            AppStatus.SetTemporaryLong(Locale[FvLocale.OpenTitleWebPage_Failed, ex.Message], "fa-regular fa-circle-xmark");
        }
    }

    private bool CanOpenTitleWebPage() => GetOpenedTitleId() != null;

    private string? GetOpenedTitleId()
    {
        if (CurrentDocument is not OpenedFileDocument document) {
            return null;
        }

        var cnmtItems = document.ViewModel.NxFile.Overview.CnmtContainers
            .Select(container => container.CnmtItem)
            .ToArray();
        // Prefer the application CNMT so patches/DLC opened alone still try the base title page.
        var appCnmt = cnmtItems.FirstOrDefault(item => item.ContentType == ContentMetaType.Application);
        return (appCnmt ?? cnmtItems.FirstOrDefault())?.TitleId;
    }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.Shutdown();
        }
    }

    public async Task OpenFilePath(string filePath)
    {
        var existing = Documents.OfType<OpenedFileDocument>()
            .FirstOrDefault(d => string.Equals(d.ViewModel.NxFile.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing != null) {
            CurrentDocument = existing;
            return;
        }

        try {
            var nxFile = await AppServices.Current.FileOpening.SafeOpenFile(filePath);
            if (nxFile == null) {
                return;
            }

            var document = new OpenedFileDocument(nxFile);
            var empty = Documents.OfType<EmptyDocument>().FirstOrDefault();
            if (empty != null) {
                var index = Documents.IndexOf(empty);
                Documents.RemoveAt(index);
                Documents.Insert(index, document);
            }
            else {
                Documents.Add(document);
            }

            CurrentDocument = document;
        }
        catch (Exception ex) {
            await new FAContentDialog
            {
                Title = Locale[FvLocale.FailedToOpenFile],
                Content = ex.Message,
                PrimaryButtonText = Locale[FvLocale.Dialog_OK]
            }.ShowAsync();
        }
    }

    public async Task OnFilesDropped(IReadOnlyList<string> files)
    {
        if (files.Count == 0) {
            return;
        }

        if (files.Count > 1) {
            AppStatus.SetTemporaryLong(Locale[FvLocale.MultipleFilesDragAndDropNotSupported]);
        }

        var filePath = files[0];
        var fileName = Path.GetFileName(filePath);

        if (string.Equals(fileName, IKeySetProviderService.DefaultProdKeysFileName, StringComparison.OrdinalIgnoreCase)) {
            AppServices.Current.FileLoadingSettings.ProdKeysFilePath = filePath;
            AppStatus.SetTemporaryLong(Locale[FvLocale.Status_ProductionKeysPathUpdated], "fa-regular fa-key");
            OnPropertyChanged(nameof(NoProdKeysLoaded));
            return;
        }

        if (string.Equals(fileName, IKeySetProviderService.DefaultTitleKeysFileName, StringComparison.OrdinalIgnoreCase)) {
            AppServices.Current.FileLoadingSettings.TitleKeysFilePath = filePath;
            AppStatus.SetTemporaryLong(Locale[FvLocale.Status_TitleKeysPathUpdated], "fa-regular fa-key");
            return;
        }

        await OpenFilePath(filePath);
    }

    public void RemoveDocument(Document document)
    {
        var index = Documents.IndexOf(document);
        if (index < 0) {
            return;
        }

        Documents.RemoveAt(index);

        if (CurrentDocument == document) {
            CurrentDocument = Documents.Count == 0
                ? null
                : Documents[Math.Min(index, Documents.Count - 1)];
        }
    }

    private async Task<IStorageFolder?> TryGetLastUsedFolder()
    {
        if (StorageProvider == null) {
            return null;
        }

        var lastUsedDir = Config.Shared.LastUsedDir;
        if (string.IsNullOrWhiteSpace(lastUsedDir) || !Directory.Exists(lastUsedDir)) {
            return null;
        }

        return await StorageProvider.TryGetFolderFromPathAsync(lastUsedDir);
    }

    private void OnDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasDocuments));
    }
}

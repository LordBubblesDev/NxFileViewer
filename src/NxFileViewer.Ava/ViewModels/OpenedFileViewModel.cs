using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emignatik.NxFileViewer.Localization;
using Emignatik.NxFileViewer.Models;
using Emignatik.NxFileViewer.Models.Overview;
using Emignatik.NxFileViewer.Models.TreeItems;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using Emignatik.NxFileViewer.Services.Integrity;
using FluentAvalonia.UI.Controls;
using LibHac.Fs;
using LibHac.Ns;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.Services;

namespace NxFileViewer.Ava.ViewModels;

public partial class OpenedFileViewModel : ObservableObject, IDisposable
{
    private readonly FileOverview _overview;
    private CancellationTokenSource? _integrityCts;
    private CancellationTokenSource? _exportCts;
    private IItem? _selectedItem;

    public OpenedFileViewModel(NxFile nxFile)
    {
        NxFile = nxFile;
        _overview = nxFile.Overview;
        RootNodes = [new TreeNodeViewModel(nxFile.RootItem, this, expand: true)];
        FileType = _overview.FileType.ToString();
        CompressionType = _overview.NcaCompressionType.ToString();
        IsSuperPackage = _overview.IsSuperPackage;
        MissingKeysText = string.Join(
            Environment.NewLine,
            _overview.MissingKeys.Select(k =>
                LoadingLocalizationKeys.ToolTipKeyMissing.SafeFormat(k.KeyName, k.KeyType)));

        Packages = _overview.CnmtContainers
            .Select((container, index) => new CnmtPackageViewModel(container, index + 1, FileType, CompressionType))
            .ToArray();
        SelectedPackage = Packages.FirstOrDefault();

        _overview.PropertyChanged += OnOverviewPropertyChanged;
        UpdateIntegrity();
    }

    public NxFile NxFile { get; }

    public IReadOnlyList<TreeNodeViewModel> RootNodes { get; }

    private string FileType { get; }

    private string CompressionType { get; }

    public bool IsSuperPackage { get; }

    public IReadOnlyList<CnmtPackageViewModel> Packages { get; }

    public string MissingKeysText { get; }

    public bool HasMissingKeys => MissingKeysText.Length > 0;

    [ObservableProperty]
    public partial CnmtPackageViewModel? SelectedPackage { get; set; }

    [ObservableProperty]
    public partial string SignatureText { get; private set; } = ItemPropertyRows.ToDisplayName(NcasIntegrity.Unchecked);

    [ObservableProperty]
    public partial IBrush SignatureBrush { get; set; } = Brushes.Gray;

    [ObservableProperty]
    public partial string HashText { get; private set; } = ItemPropertyRows.ToDisplayName(NcasIntegrity.Unchecked);

    [ObservableProperty]
    public partial IBrush HashBrush { get; set; } = Brushes.Gray;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(VerifySignatureCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyHashCommand))]
    private partial bool IsCheckingSignature { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(VerifySignatureCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyHashCommand))]
    private partial bool IsCheckingHash { get; set; }

    public string SignatureButtonText => IsCheckingSignature ? Locale[FvLocale.CancelAction] : Locale[FvLocale.CheckAction];

    public string HashButtonText => IsCheckingHash ? Locale[FvLocale.CancelAction] : Locale[FvLocale.CheckAction];

    public IItem? SelectedItem
    {
        get => _selectedItem;
        set {
            if (ReferenceEquals(_selectedItem, value)) {
                return;
            }

            if (_selectedItem != null) {
                _selectedItem.PropertyChanged -= OnSelectedItemPropertyChanged;
            }

            _selectedItem = value;
            OnPropertyChanged();

            if (_selectedItem != null) {
                _selectedItem.PropertyChanged += OnSelectedItemPropertyChanged;
            }

            RefreshProperties();
        }
    }

    public ObservableCollection<ItemPropertyRow> Properties { get; } = [];

    public bool HasSelectedItem => SelectedItem != null;

    [RelayCommand(CanExecute = nameof(CanVerifySignature))]
    private async Task VerifySignature()
    {
        if (IsCheckingSignature) {
            _integrityCts?.Cancel();
            return;
        }

        await RunIntegrityAsync(IntegrityCheckKind.Signature);
    }

    private bool CanVerifySignature() => !IsCheckingHash;

    [RelayCommand(CanExecute = nameof(CanVerifyHash))]
    private async Task VerifyHash()
    {
        if (IsCheckingHash) {
            _integrityCts?.Cancel();
            return;
        }

        await RunIntegrityAsync(IntegrityCheckKind.Hash);
    }

    private bool CanVerifyHash() => !IsCheckingSignature;

    private async Task RunIntegrityAsync(IntegrityCheckKind kind)
    {
        var checkingSignature = kind.HasFlag(IntegrityCheckKind.Signature);
        var checkingHash = kind.HasFlag(IntegrityCheckKind.Hash);

        if (checkingSignature) {
            IsCheckingSignature = true;
        }
        if (checkingHash) {
            IsCheckingHash = true;
        }

        _integrityCts = new CancellationTokenSource();

        try {
            await AppServices.Current.IntegrityVerifier.VerifyAsync(
                _overview,
                new AppStatusProgressReporter(),
                _integrityCts.Token,
                kind);
        }
        finally {
            if (checkingSignature) {
                IsCheckingSignature = false;
            }
            if (checkingHash) {
                IsCheckingHash = false;
            }
            _integrityCts.Dispose();
            _integrityCts = null;
            AppStatus.Reset();
            RefreshProperties();
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowItemErrors))]
    private async Task ShowItemErrors()
    {
        if (SelectedItem == null || SelectedItem.Errors.Count == 0) {
            return;
        }

        var message = string.Join(Environment.NewLine, SelectedItem.Errors.Select(error => error.Message));
        await new ContentDialog
        {
            Title = Locale[FvLocale.ItemErrors_Title],
            Content = message,
            PrimaryButtonText = Locale[FvLocale.Dialog_OK]
        }.ShowAsync();
    }

    private bool CanShowItemErrors() => SelectedItem is { Errors.Count: > 0 };

    [RelayCommand]
    private async Task SaveNcaRaw()
    {
        if (SelectedItem is NcaItem nca) {
            await ExportFileAsync(() => nca.LoadFile(), nca.FileName);
        }
    }

    [RelayCommand]
    private async Task SaveNcaPlaintext()
    {
        if (SelectedItem is not NcaItem nca) {
            return;
        }

        var path = await AppServices.Current.TreeItemExport.PickSaveFileAsync(nca.FileName);
        if (path == null) {
            return;
        }

        await RunExportAsync(ct => {
            var storage = nca.Nca.OpenDecryptedNca();
            return AppServices.Current.TreeItemExport.SaveStorageAsync(storage, path, ct);
        });
    }

    [RelayCommand]
    private async Task SavePartitionFile()
    {
        if (SelectedItem is PartitionFileEntryItemBase partition) {
            await ExportFileAsync(() => partition.LoadFile(), partition.Name);
        }
    }

    [RelayCommand]
    private async Task SaveDirectoryEntry()
    {
        if (SelectedItem is not DirectoryEntryItem entry) {
            return;
        }

        if (entry.DirectoryEntryType == DirectoryEntryType.File) {
            await ExportFileAsync(entry.GetFile, entry.Name);
            return;
        }

        var folder = await AppServices.Current.TreeItemExport.PickFolderAsync();
        if (folder == null) {
            return;
        }

        await RunExportAsync(ct => AppServices.Current.TreeItemExport.SaveDirectoryEntriesAsync([entry], folder, ct));
    }

    [RelayCommand]
    private async Task SaveSection()
    {
        if (SelectedItem is not SectionItem section || section.ChildItems.Length == 0) {
            return;
        }

        var folder = await AppServices.Current.TreeItemExport.PickFolderAsync();
        if (folder == null) {
            return;
        }

        var target = System.IO.Path.Combine(folder, $"Section_{section.SectionIndex}");
        await RunExportAsync(ct => AppServices.Current.TreeItemExport.SaveDirectoryEntriesAsync(section.ChildItems, target, ct));
    }

    public void Dispose()
    {
        _exportCts?.Cancel();
        _exportCts?.Dispose();
        _integrityCts?.Cancel();
        _integrityCts?.Dispose();
        _overview.PropertyChanged -= OnOverviewPropertyChanged;
        if (_selectedItem != null) {
            _selectedItem.PropertyChanged -= OnSelectedItemPropertyChanged;
        }

        foreach (var node in RootNodes) {
            node.Dispose();
        }

        foreach (var package in Packages) {
            package.Dispose();
        }

        NxFile.Dispose();
    }

    private void OnOverviewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(FileOverview.SignatureIntegrity) or nameof(FileOverview.HashIntegrity) or nameof(FileOverview.NcasIntegrity))) {
            return;
        }

        Dispatcher.UIThread.Post(UpdateIntegrity);
    }

    private void OnSelectedItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NcaItem.HashValid) or nameof(NcaItem.HeaderSignatureValidity)) {
            Dispatcher.UIThread.Post(RefreshProperties);
        }
    }

    private void UpdateIntegrity()
    {
        SignatureText = ItemPropertyRows.ToDisplayName(_overview.SignatureIntegrity);
        SignatureBrush = ToIntegrityBrush(_overview.SignatureIntegrity);
        HashText = ItemPropertyRows.ToDisplayName(_overview.HashIntegrity);
        HashBrush = ToIntegrityBrush(_overview.HashIntegrity);
    }

    private static IBrush ToIntegrityBrush(NcasIntegrity integrity) => integrity switch {
        NcasIntegrity.Original => Brushes.LimeGreen,
        NcasIntegrity.Incomplete or NcasIntegrity.Modified => Brushes.Orange,
        NcasIntegrity.Corrupted or NcasIntegrity.Error => Brushes.IndianRed,
        _ => Brushes.Gray
    };

    private void RefreshProperties()
    {
        Properties.Clear();
        foreach (var row in ItemPropertyRows.Create(SelectedItem)) {
            Properties.Add(row);
        }

        OnPropertyChanged(nameof(HasSelectedItem));
        ShowItemErrorsCommand.NotifyCanExecuteChanged();
    }

    private async Task ExportFileAsync(Func<LibHac.Fs.Fsa.IFile> openFile, string fileName)
    {
        var path = await AppServices.Current.TreeItemExport.PickSaveFileAsync(fileName);
        if (path == null) {
            return;
        }

        await RunExportAsync(async ct => {
            using var file = openFile();
            await AppServices.Current.TreeItemExport.SaveLibHacFileAsync(file, path, ct);
        });
    }

    private async Task RunExportAsync(Func<CancellationToken, Task> export)
    {
        _exportCts?.Cancel();
        _exportCts?.Dispose();
        _exportCts = new CancellationTokenSource();

        try {
            await export(_exportCts.Token);
            AppStatus.SetTemporaryShort(Locale[FvLocale.Status_Saved]);
        }
        catch (OperationCanceledException) {
            AppStatus.SetTemporaryLong(Locale[FvLocale.Log_SaveFileCanceled], "fa-regular fa-circle-xmark");
        }
        catch (Exception ex) {
            AppStatus.SetTemporaryLong(Locale[FvLocale.SaveFile_Error, ex.Message], "fa-regular fa-circle-xmark");
        }
        finally {
            _exportCts.Dispose();
            _exportCts = null;
        }
    }

    partial void OnIsCheckingSignatureChanged(bool value)
    {
        OnPropertyChanged(nameof(SignatureButtonText));
    }

    partial void OnIsCheckingHashChanged(bool value)
    {
        OnPropertyChanged(nameof(HashButtonText));
    }
}

public sealed partial class CnmtPackageViewModel : ObservableObject, IDisposable
{
    public CnmtPackageViewModel(CnmtContainer container, int number, string fileType, string compressionType)
    {
        DisplayName = $"{number} {container.CnmtItem.ContentType}";
        FileType = fileType;
        CompressionType = compressionType;
        ContentType = container.CnmtItem.ContentType.ToString();
        TitleId = container.CnmtItem.TitleId;
        TitleVersion = container.CnmtItem.TitleVersion;
        PatchLevel = LoadingLocalizationKeys.ToolTipPatchNumber.SafeFormat(container.CnmtItem.PatchNumber);
        MinimumSystemVersion = container.CnmtItem.MinimumSystemVersion?.ToString();
        DisplayVersion = container.NacpContainer?.NacpItem.DisplayVersion;
        IsDemo = container.NacpContainer?.NacpItem.Attribute == ApplicationControlProperty.AttributeFlagValue.Demo;
        HasPresentation = container.NacpContainer != null;

        // Sparse NSO sections cannot be opened, so the build ID is unavailable rather than missing.
        BuildId = container.MainItem?.ModuleId
                  ?? (container.MainItemSectionIsSparse
                      ? LoadingLocalizationKeys.CnmtOverviewBuildIdNotAvailableBecauseSectionIsSparse
                      : string.Empty);

        Titles = (container.NacpContainer?.Titles ?? [])
            .Select(title => new TitleLanguageViewModel(title))
            .ToArray();
        SelectedTitle = Titles.FirstOrDefault();
    }

    public string DisplayName { get; }

    public string FileType { get; }

    public string CompressionType { get; }

    public string ContentType { get; }

    public string TitleId { get; }

    public string? TitleVersion { get; }

    public string PatchLevel { get; }

    public string? MinimumSystemVersion { get; }

    public string? DisplayVersion { get; }

    public string BuildId { get; }

    public bool IsDemo { get; }

    public bool HasPresentation { get; }

    public IReadOnlyList<TitleLanguageViewModel> Titles { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveIconCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyIconCommand))]
    public partial TitleLanguageViewModel? SelectedTitle { get; set; }

    public bool HasIcon => SelectedTitle?.IconBytes is { Length: > 0 };

    private bool CanUseIcon() => HasIcon;

    [RelayCommand(CanExecute = nameof(CanUseIcon))]
    private async Task SaveIcon()
    {
        var title = SelectedTitle;
        if (title?.IconBytes is not { Length: > 0 } bytes) {
            return;
        }

        var topLevel = GetMainWindow();
        if (topLevel == null) {
            return;
        }

        var suggestedName = SanitizeFileName($"{title.AppName}_({title.Language}).jpg");
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Locale[FvLocale.SaveIcon_Title],
            SuggestedFileName = suggestedName,
            DefaultExtension = "jpg",
            FileTypeChoices =
            [
                new FilePickerFileType(Locale[FvLocale.SaveDialog_JpegFilter]) { Patterns = ["*.jpg", "*.jpeg"] }
            ]
        });

        if (file == null) {
            return;
        }

        try {
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(bytes);
            AppStatus.SetTemporaryShort(Locale[FvLocale.Status_IconSaved]);
        }
        catch (Exception ex) {
            AppStatus.SetTemporaryLong(Locale[FvLocale.SaveTitleImageError, ex.Message], "fa-regular fa-circle-xmark");
        }
    }

    [RelayCommand(CanExecute = nameof(CanUseIcon))]
    private async Task CopyIcon()
    {
        var icon = SelectedTitle?.Icon;
        var clipboard = GetMainWindow()?.Clipboard;
        if (icon == null || clipboard == null) {
            return;
        }

        try {
            await using var stream = new MemoryStream();
            icon.Save(stream);
            var data = new DataObject();
            data.Set("PNG", stream.ToArray());
            await clipboard.SetDataObjectAsync(data);
            AppStatus.SetTemporaryShort(Locale[FvLocale.Status_IconCopied], "fa-regular fa-copy");
        }
        catch (Exception ex) {
            AppStatus.SetTemporaryLong(Locale[FvLocale.CopyTitleImageError, ex.Message], "fa-regular fa-circle-xmark");
        }
    }

    partial void OnSelectedTitleChanged(TitleLanguageViewModel? value)
    {
        OnPropertyChanged(nameof(HasIcon));
    }

    public void Dispose()
    {
        foreach (var title in Titles) {
            title.Dispose();
        }
    }

    private static TopLevel? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }
            ? window
            : null;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(c => invalidChars.Contains(c) ? '_' : c));
    }
}

public sealed class TitleLanguageViewModel : IDisposable
{
    public TitleLanguageViewModel(TitleInfo title)
    {
        Language = ItemPropertyRows.ToDisplayName(title.Language);
        AppName = title.AppName;
        Publisher = title.Publisher;
        IconBytes = title.Icon is { Length: > 0 } ? title.Icon : null;
        if (IconBytes != null) {
            using var stream = new MemoryStream(IconBytes);
            Icon = new Bitmap(stream);
        }
    }

    public string Language { get; }

    public string AppName { get; }

    public string Publisher { get; }

    public byte[]? IconBytes { get; }

    public Bitmap? Icon { get; }

    public void Dispose()
    {
        Icon?.Dispose();
    }
}

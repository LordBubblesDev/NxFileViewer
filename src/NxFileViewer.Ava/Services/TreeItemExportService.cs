using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using Emignatik.NxFileViewer.Settings;
using LibHac.Fs;
using NxFileViewer.Ava.Localization;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using Path = System.IO.Path;

namespace NxFileViewer.Ava.Services;

public sealed class TreeItemExportService(IFileLoadingSettings settings)
{
    public async Task SaveLibHacFileAsync(IFile source, string destinationPath, CancellationToken cancellationToken)
    {
        await using var stream = source.AsStream();
        await SaveStreamAsync(stream, destinationPath, cancellationToken);
    }

    public async Task SaveStorageAsync(IStorage source, string destinationPath, CancellationToken cancellationToken)
    {
        await using var stream = source.AsStream();
        await SaveStreamAsync(stream, destinationPath, cancellationToken);
    }

    public async Task SaveDirectoryEntriesAsync(IEnumerable<DirectoryEntryItem> items, string targetDirectory, CancellationToken cancellationToken)
    {
        var elements = ListElements(items);
        var reporter = new AppStatusProgressReporter();
        var index = 0;

        foreach (var (item, relativePath) in elements) {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            reporter.SetText(Locale[FvLocale.SavingDirectory_Progress, index, elements.Count]);

            var destination = Path.Combine(targetDirectory, relativePath);
            switch (item.DirectoryEntryType) {
                case DirectoryEntryType.Directory:
                    Directory.CreateDirectory(destination);
                    reporter.SetPercentage((double)index / elements.Count);
                    break;
                case DirectoryEntryType.File:
                    using (var file = item.GetFile()) {
                        await SaveLibHacFileAsync(file, destination, cancellationToken);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public async Task<string?> PickSaveFileAsync(string suggestedFileName)
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) {
            return null;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Locale[FvLocale.SaveDialog_Title],
            SuggestedFileName = SanitizeFileName(suggestedFileName),
            SuggestedStartLocation = await TryGetLastUsedFolder(topLevel.StorageProvider)
        });

        var path = file?.TryGetLocalPath();
        RememberDirectory(path);
        return path;
    }

    public async Task<string?> PickFolderAsync()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) {
            return null;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Locale[FvLocale.SaveDialog_SelectFolder],
            AllowMultiple = false,
            SuggestedStartLocation = await TryGetLastUsedFolder(topLevel.StorageProvider)
        });

        var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (!string.IsNullOrEmpty(path)) {
            Config.Shared.LastUsedDir = path;
        }

        return path;
    }

    private async Task SaveStreamAsync(Stream source, string destinationPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }

        var reporter = new AppStatusProgressReporter();
        reporter.SetText(Locale[FvLocale.SavingFile_Progress, Path.GetFileName(destinationPath)]);

        var bufferSize = Math.Max(settings.ProgressBufferSize, 64 * 1024);
        var buffer = new byte[bufferSize];
        var totalBytes = source.CanSeek ? source.Length : 0L;
        long written = 0;

        try {
            await using var destination = File.Create(destinationPath);
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await source.ReadAsync(buffer, cancellationToken);
                if (read <= 0) {
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                written += read;
                if (totalBytes > 0) {
                    reporter.SetPercentage((double)written / totalBytes);
                }
            }
        }
        catch (OperationCanceledException) {
            // Don't leave a half-written file behind if the user cancels.
            if (File.Exists(destinationPath)) {
                File.Delete(destinationPath);
            }
            throw;
        }
    }

    private static List<(DirectoryEntryItem Item, string RelativePath)> ListElements(IEnumerable<DirectoryEntryItem> roots)
    {
        var elements = new List<(DirectoryEntryItem, string)>();
        var remaining = new Queue<(DirectoryEntryItem Item, string RelativePath)>();

        foreach (var item in roots) {
            remaining.Enqueue((item, SanitizeFileName(item.Name)));
        }

        while (remaining.Count > 0) {
            var current = remaining.Dequeue();
            elements.Add(current);

            foreach (var child in current.Item.ChildItems) {
                remaining.Enqueue((child, Path.Combine(current.RelativePath, SanitizeFileName(child.Name))));
            }
        }

        return elements;
    }

    private static void RememberDirectory(string? filePath)
    {
        var directory = string.IsNullOrEmpty(filePath) ? null : Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) {
            Config.Shared.LastUsedDir = directory;
        }
    }

    private static async Task<IStorageFolder?> TryGetLastUsedFolder(IStorageProvider storageProvider)
    {
        var lastUsedDir = Config.Shared.LastUsedDir;
        if (string.IsNullOrWhiteSpace(lastUsedDir) || !Directory.Exists(lastUsedDir)) {
            return null;
        }

        return await storageProvider.TryGetFolderFromPathAsync(lastUsedDir);
    }

    private static TopLevel? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }
            ? window
            : null;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(c => invalidChars.Contains(c) ? '_' : c));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emignatik.NxFileViewer.Localization;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;

namespace Emignatik.NxFileViewer.Utils.LibHacExtensions;

/// <summary>
/// Opens a folder of split dump parts as a single concatenated <see cref="IStorage"/>
/// </summary>
public static class SplitFileStorage
{
    public static IStorage OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath)) {
            throw new ArgumentException("Folder path can't be empty.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath)) {
            throw new DirectoryNotFoundException(folderPath);
        }

        var files = Directory.EnumerateFiles(folderPath)
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        if (files.Length == 0) {
            throw new FileNotFoundException(LoadingLocalizationKeys.LoadingErrorSplitFolderEmpty.SafeFormat(folderPath), folderPath);
        }

        var parts = new List<IStorage>(files.Length);
        try {
            foreach (var file in files) {
                parts.Add(new LocalStorage(file, FileAccess.Read));
            }

            return new ConcatenationStorage(parts, leaveOpen: false);
        }
        catch {
            foreach (var part in parts) {
                part.Dispose();
            }

            throw;
        }
    }
}

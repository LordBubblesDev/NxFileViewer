using System;
using System.IO;

namespace Emignatik.NxFileViewer.Utils;

public static class PathHelper
{
    static PathHelper()
    {
        try {
            CurrentAppDir = AppDomain.CurrentDomain.BaseDirectory;
        }
        catch {
            CurrentAppDir = Directory.GetCurrentDirectory();
        }
                
        try {
            HomeUserDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        catch {
            HomeUserDir = null;
        }
    }

    public static string? HomeUserDir { get; }
    public static string CurrentAppDir { get; }

    public static string ToFullPath(this string? relOrAbsPath)
    {
        return string.IsNullOrEmpty(relOrAbsPath)
            ? CurrentAppDir
            : Path.GetFullPath(Path.IsPathRooted(relOrAbsPath) ? relOrAbsPath : Path.Combine(CurrentAppDir, relOrAbsPath));
    }

}

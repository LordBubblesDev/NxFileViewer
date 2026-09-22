using System;

namespace Emignatik.NxFileViewer.Localization;

public static class LocalizationStringExtension
{
    public static string SafeFormat(this string str, params object?[] args)
    {
        try {
            return string.Format(str, args);
        }
        catch (Exception) {
            return str;
        }
    }
}
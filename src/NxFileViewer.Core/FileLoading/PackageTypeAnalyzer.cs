using System;
using System.IO;
using System.Text;
using Emignatik.NxFileViewer.Localization;
using Emignatik.NxFileViewer.Utils.LibHacExtensions;
using LibHac.Common;
using LibHac.Fs;
using Microsoft.Extensions.Logging;

namespace Emignatik.NxFileViewer.FileLoading;

public class PackageTypeAnalyzer : IPackageTypeAnalyzer
{
    private readonly ILogger _logger;

    public PackageTypeAnalyzer(ILoggerFactory loggerFactory)
    {
        _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger(this.GetType());
    }

    public PackageType GetType(string filePath)
    {
        if (Directory.Exists(filePath)) {
            using var storage = SplitFileStorage.OpenFolder(filePath);
            return IsNsp(storage) ? PackageType.NSP : PackageType.XCI;
        }

        using var fileStream = File.OpenRead(filePath);
        var buffer = new byte[0x104];
        var read = fileStream.Read(buffer);
        
        switch (read) {
            case >= 4 when Encoding.ASCII.GetString(buffer, 0, 4) == "PFS0":
                CheckExtensionConsistency(filePath, ".nsp", ".nsz");
                return PackageType.NSP;
            case >= 0x104 when Encoding.ASCII.GetString(buffer, 0x100, 4) == "HEAD":
                CheckExtensionConsistency(filePath, ".xci", ".xcz");
                return PackageType.XCI;
            default:
                return PackageType.Unknown;
        }
    }

    private static bool IsNsp(IStorage storage)
    {
        Span<byte> buffer = stackalloc byte[4];
        storage.Read(0, buffer).ThrowIfFailure();
        return buffer.SequenceEqual("PFS0"u8);
    }

    private void CheckExtensionConsistency(string filePath, string expectedExt1, string expectedExt2)
    {
        var fileExtension = System.IO.Path.GetExtension(filePath);
        if (!string.Equals(fileExtension, expectedExt1, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fileExtension, expectedExt2, StringComparison.OrdinalIgnoreCase)) {
            _logger.LogWarning(LoadingLocalizationKeys.SuspiciousFileExtension.SafeFormat(fileExtension, expectedExt1, expectedExt2));
        }
    }
}

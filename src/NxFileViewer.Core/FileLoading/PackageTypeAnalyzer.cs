using System;
using System.IO;
using System.Text;
using Emignatik.NxFileViewer.Localization;
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

    private void CheckExtensionConsistency(string filePath, string expectedExt1, string expectedExt2)
    {
        var fileExtension = Path.GetExtension(filePath);
        if (!string.Equals(fileExtension, expectedExt1, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fileExtension, expectedExt2, StringComparison.OrdinalIgnoreCase)) {
            _logger.LogWarning(LoadingLocalizationKeys.SuspiciousFileExtension.SafeFormat(fileExtension, expectedExt1, expectedExt2));
        }
    }
}

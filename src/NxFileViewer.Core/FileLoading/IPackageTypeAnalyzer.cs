namespace Emignatik.NxFileViewer.FileLoading;

public interface IPackageTypeAnalyzer
{
    PackageType GetType(string filePath);
}


public enum PackageType
{
    Unknown,
    XCI,
    NSP
}
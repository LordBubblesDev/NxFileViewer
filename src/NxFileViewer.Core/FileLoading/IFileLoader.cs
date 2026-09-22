using Emignatik.NxFileViewer.Models;

namespace Emignatik.NxFileViewer.FileLoading;

public interface IFileLoader
{
    public NxFile Load(string filePath);
}
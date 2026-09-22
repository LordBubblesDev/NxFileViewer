
using System;
using System.IO;
using Emignatik.NxFileViewer.Models.Overview;
using Emignatik.NxFileViewer.Models.TreeItems;

namespace Emignatik.NxFileViewer.Models;

public class NxFile(string filePath, IItem rootItem, FileOverview overview) : IDisposable
{
    public string FilePath { get; } = filePath;
    public IItem RootItem { get; } = rootItem;
    public FileOverview Overview { get; } = overview;
    public string FileName { get; } = Path.GetFileName(filePath);

    public void Dispose()
    {
        RootItem.Dispose();
    }
}
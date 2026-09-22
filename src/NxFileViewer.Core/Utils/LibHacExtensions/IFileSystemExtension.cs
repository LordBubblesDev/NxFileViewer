using LibHac.Common;
using LibHac.Fs;
using LibHac.Tools.Fs;
using LibHac.Fs.Fsa;

namespace Emignatik.NxFileViewer.Utils.LibHacExtensions;

public static class IFileSystemExtension
{
    public static IFile LoadFile(this IFileSystem fileSystem, DirectoryEntryEx directoryEntryEx, OpenMode openMode = OpenMode.Read)
    {
        using var uniqueRefFile = new UniqueRef<IFile>();
        fileSystem.OpenFile(ref uniqueRefFile.Ref, directoryEntryEx.FullPath.ToU8Span(), openMode).ThrowIfFailure();
        return uniqueRefFile.Release();
    }
}

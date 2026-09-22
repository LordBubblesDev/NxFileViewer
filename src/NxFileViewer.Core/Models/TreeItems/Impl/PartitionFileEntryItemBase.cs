using System;
using Emignatik.NxFileViewer.Utils.LibHacExtensions;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public abstract class PartitionFileEntryItemBase(DirectoryEntryEx fileEntry, PartitionFileSystemItemBase parentItem)
    : ItemBase(parentItem)
{
    public new PartitionFileSystemItemBase ParentItem { get; } = parentItem ?? throw new ArgumentNullException(nameof(parentItem));

    public DirectoryEntryEx FileEntry { get; } = fileEntry ?? throw new ArgumentNullException(nameof(fileEntry));

    public sealed override string LibHacTypeName => nameof(FileEntry);

    public override string Name => FileEntry.Name;

    public override string DisplayName => Name;

    public long Size => FileEntry.Size;

    public IFile LoadFile()
    {
        return ParentItem.PartitionFileSystem.LoadFile(FileEntry);
    }
}
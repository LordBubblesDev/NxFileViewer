using LibHac.Tools.Fs;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public class PartitionFileEntryItem(DirectoryEntryEx partitionFileEntry, PartitionFileSystemItemBase parentItem)
    : PartitionFileEntryItemBase(partitionFileEntry, parentItem)
{
    public override string? Format => null;
}
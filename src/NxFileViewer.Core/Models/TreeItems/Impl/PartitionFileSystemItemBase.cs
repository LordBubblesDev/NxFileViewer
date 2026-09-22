using System;
using System.Linq;
using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public abstract class PartitionFileSystemItemBase(IFileSystem partitionFileSystem, ItemBase? parent) : ItemBase(parent)
{
    public IFileSystem PartitionFileSystem { get; } = partitionFileSystem ?? throw new ArgumentNullException(nameof(partitionFileSystem));

    public string PartitionType => PartitionFileSystem.GetType().Name;

    public int NbEntries => PartitionFileSystem.GetEntryCount(OpenDirectoryMode.All);

    public sealed override string LibHacTypeName => PartitionFileSystem.GetType().Name;

    public abstract KeySet KeySet { get; }

    public NcaItem[] NcaChildItems => [.. ChildItems.OfType<NcaItem>()];
}
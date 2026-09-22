using System;
using System.Linq;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

/// <summary>
/// Can be either a file or a directory from a <see cref="IFileSystem"/> LibHac model
/// </summary>
public class DirectoryEntryItem : ItemBase
{
    public DirectoryEntryItem(SectionItem parentItem, DirectoryEntryEx directoryEntry)
        : this(parentItem, parentItem, directoryEntry)
    {
        ParentSectionItem = parentItem ?? throw new ArgumentNullException(nameof(parentItem));
    }

    public DirectoryEntryItem(SectionItem containerSectionItem, DirectoryEntryEx directoryEntry, DirectoryEntryItem parentItem)
        : this(parentItem, containerSectionItem, directoryEntry)
    {
        ParentDirectoryEntryItem = parentItem ?? throw new ArgumentNullException(nameof(parentItem));
    }

    private DirectoryEntryItem(ItemBase parentItem, SectionItem containerSectionItem, DirectoryEntryEx directoryEntry) : base(parentItem)
    {
        DirectoryEntry = directoryEntry;
        ContainerSectionItem = containerSectionItem ?? throw new ArgumentNullException(nameof(containerSectionItem));
        Size = directoryEntry.Size;
    }

    public DirectoryEntryItem? ParentDirectoryEntryItem { get; }
    public SectionItem? ParentSectionItem { get; }
    public SectionItem ContainerSectionItem { get; }
    public new DirectoryEntryItem[] ChildItems => base.ChildItems.OfType<DirectoryEntryItem>().ToArray();
    private DirectoryEntryEx DirectoryEntry { get; }
    public sealed override string LibHacTypeName => DirectoryEntry.GetType().Name;
    public override string? Format => null;
    public override string Name => DirectoryEntry.Name;
    public long Size { get; }
    public DirectoryEntryType DirectoryEntryType => DirectoryEntry.Type;
    public string Path => DirectoryEntry.FullPath;
    public override string DisplayName => Name;

    public IFile GetFile()
    {
        using var uniqueRefFile = new UniqueRef<IFile>();
        ContainerSectionItem.FileSystem!.OpenFile(ref uniqueRefFile.Ref, Path.ToU8Span(), OpenMode.Read).ThrowIfFailure();
        return uniqueRefFile.Release();
    }

    public override string ToString()
    {
        return $"{DisplayName} ({DirectoryEntry.Type})";
    }
}
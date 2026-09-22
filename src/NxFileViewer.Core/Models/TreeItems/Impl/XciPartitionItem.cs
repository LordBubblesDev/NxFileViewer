using System;
using LibHac.Common.Keys;
using LibHac.Tools.Fs;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public class XciPartitionItem(XciPartition xciPartition, XciPartitionType xciPartitionType, XciItem parentItem)
    : PartitionFileSystemItemBase(xciPartition, parentItem)
{
    private XciItem ParentItem { get; } = parentItem ?? throw new ArgumentNullException(nameof(parentItem));

    public XciPartition XciPartition { get; } = xciPartition ?? throw new ArgumentNullException(nameof(xciPartition));

    public override string Format => nameof(XciPartition);

    public override string Name => XciPartitionType.ToString();

    public override string DisplayName => Name;

    public override KeySet KeySet => ParentItem.KeySet;

    public XciPartitionType XciPartitionType { get; } = xciPartitionType;
}
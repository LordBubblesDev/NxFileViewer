using System;
using Emignatik.NxFileViewer.Utils;
using LibHac.Ncm;
using LibHac.Tools.Ncm;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

/// <summary>
/// Contains metadata about an NCA
/// </summary>
public class CnmtContentEntryItem(CnmtContentEntry cnmtContentEntry, CnmtItem parentItem, int index) : ItemBase(parentItem) {
    public override string DisplayName => $"Entry {Index}";
    public override string Name => Index.ToString();
    private int Index { get; } = index;
    private CnmtContentEntry CnmtContentEntry { get; } = cnmtContentEntry ?? throw new ArgumentNullException(nameof(cnmtContentEntry));
    public ContentType NcaContentType => CnmtContentEntry.Type;
    public string NcaId => CnmtContentEntry.NcaId.ToStrId();
    public byte[] NcaHash => CnmtContentEntry.Hash;
    public long NcaSize => CnmtContentEntry.Size;
    public CnmtItem ParentItem { get; } = parentItem ?? throw new ArgumentNullException(nameof(parentItem));
    public override string LibHacTypeName => CnmtContentEntry.GetType().Name;
    public override string? Format => null;
}
using LibHac.NSZ;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem.NcaUtils;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public class NczItem(Ncz ncz, DirectoryEntryEx partitionFileEntry, PartitionFileSystemItemBase parentItem)
    : NcaItem(ncz, partitionFileEntry, parentItem)
{
    public Ncz Ncz { get; } = ncz;

    public override Nca GetOriginalNca()
    {
        return Ncz.SwitchReadMode(NczReadMode.Original);
    }
}
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibHac.Common;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem.NcaUtils;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public class NcaItem : PartitionFileEntryItemBase
{
    private Validity _headerSignatureValidity;
    private bool? _hashValid;

    public static int MaxSections => 4;

    public NcaItem(Nca nca, DirectoryEntryEx ncaFileEntry, PartitionFileSystemItemBase parentItem) : base(ncaFileEntry, parentItem)
    {
        Nca = nca ?? throw new ArgumentNullException(nameof(nca));
        Id = FileEntry.Name.Split('.', 2)[0];
    }

    public new SectionItem[] ChildItems => base.ChildItems.OfType<SectionItem>().ToArray();

    public Nca Nca { get; }

    public override string Format => nameof(Nca);

    /// <summary>
    /// ID of the NCA (name without extension), also corresponds to the NCA hash in hex
    /// </summary>
    public string Id { get; }

    public TitleVersion SdkVersion => Nca.Header.SdkVersion;

    public NcaContentType ContentType => Nca.Header.ContentType;

    public DistributionType DistributionType => Nca.Header.DistributionType;

    public int ContentIndex => Nca.Header.ContentIndex;

    public byte KeyGeneration => Nca.Header.KeyGeneration;

    public NcaVersion FormatVersion => Nca.Header.FormatVersion;

    public bool IsNca0 => Nca.Header.IsNca0();

    public bool IsEncrypted => Nca.Header.IsEncrypted;

    public string FileName => FileEntry.Name;

    public override string DisplayName => $"{FileName} ({Nca.Header.ContentType})";

    public Validity HeaderSignatureValidity
    {
        get => _headerSignatureValidity;
        internal set
        {
            _headerSignatureValidity = value;
            NotifyPropertyChanged();
        }
    }

    public bool? HashValid
    {
        get => _hashValid;
        set
        {
            _hashValid = value;
            NotifyPropertyChanged();
        }
    }

    public virtual Nca GetOriginalNca()
    {
        return Nca;
    }

    public bool TryGetExpectedHashFromId([NotNullWhen(true)] out byte[]? hash)
    {
        try {
            if (Id.Length % 2 != 0) {
                hash = null;
                return false;
            }

            hash = new byte[Id.Length / 2];
            var byteIndex = 0;
            for (var i = 0; i < Id.Length; i += 2) {
                var byteHex = Id.Substring(i, 2);
                hash[byteIndex++] = Convert.ToByte(byteHex, 16);
            }
        }
        catch {
            hash = null;
            return false;
        }
        return true;
    }
}

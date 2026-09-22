using CommunityToolkit.Mvvm.ComponentModel;
using Emignatik.NxFileViewer.Models.Overview;
using Emignatik.NxFileViewer.Models.TreeItems;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using Emignatik.NxFileViewer.Utils;
using NxFileViewer.Ava.Localization;

namespace NxFileViewer.Ava.ViewModels;

public sealed partial class ItemPropertyRow : ObservableObject
{
    public ItemPropertyRow(string name, object? value)
    {
        Name = name;
        Value = Format(value);
    }

    public string Name { get; }

    [ObservableProperty]
    public partial string Value { get; set; }

    private static string Format(object? value)
    {
        return value switch {
            null => string.Empty,
            bool flag => Locale[flag ? FvLocale.Boolean_True : FvLocale.Boolean_False],
            _ => value.ToString() ?? string.Empty
        };
    }
}

public static class ItemPropertyRows
{
    public static IReadOnlyList<ItemPropertyRow> Create(IItem? item)
    {
        if (item == null) {
            return [];
        }

        var rows = new List<ItemPropertyRow>
        {
            new(L(FvLocale.Prop_Name), item.Name),
            new(L(FvLocale.Prop_Type), item.LibHacTypeName),
            new(L(FvLocale.Prop_Format), item.Format)
        };

        switch (item) {
            case CnmtItem cnmt:
                rows.Add(new(L(FvLocale.Prop_ContentType), cnmt.ContentType));
                rows.Add(new(L(FvLocale.Prop_TitleId), cnmt.TitleId));
                rows.Add(new(L(FvLocale.Prop_ApplicationTitleId), cnmt.ApplicationTitleId));
                rows.Add(new(L(FvLocale.Prop_PatchTitleId), cnmt.PatchTitleId));
                rows.Add(new(L(FvLocale.Prop_TitleVersion), cnmt.TitleVersion));
                rows.Add(new(L(FvLocale.Prop_PatchLevel), cnmt.PatchNumber));
                rows.Add(new(L(FvLocale.Prop_MinimumApplicationVersion), cnmt.MinimumApplicationVersion));
                rows.Add(new(L(FvLocale.Prop_MinimumSystemVersion), cnmt.MinimumSystemVersion));
                break;

            case NacpItem nacp:
                AddDirectoryRows(rows, nacp);
                rows.Add(new(L(FvLocale.Prop_DisplayVersion), nacp.DisplayVersion));
                rows.Add(new(L(FvLocale.Prop_PresenceGroupId), nacp.PresenceGroupId));
                rows.Add(new(L(FvLocale.Prop_AddOnContentBaseId), nacp.AddOnContentBaseId));
                rows.Add(new(L(FvLocale.Prop_SaveDataOwnerId), nacp.SaveDataOwnerId));
                rows.Add(new(L(FvLocale.Prop_StartupUserAccount), nacp.StartupUserAccount));
                rows.Add(new(L(FvLocale.Prop_Screenshot), nacp.Screenshot));
                rows.Add(new(L(FvLocale.Prop_VideoCapture), nacp.VideoCapture));
                rows.Add(new(L(FvLocale.Prop_Attribute), nacp.Attribute));
                rows.Add(new(L(FvLocale.Prop_ParentalControl), nacp.ParentalControl));
                rows.Add(new(L(FvLocale.Prop_Isbn), nacp.Isbn));
                break;

            case MainItem main:
                AddDirectoryRows(rows, main);
                rows.Add(new(L(FvLocale.Prop_ModuleId), main.ModuleId));
                break;

            case DirectoryEntryItem directory:
                AddDirectoryRows(rows, directory);
                break;

            case NcaItem nca:
                rows.Add(new(L(FvLocale.Prop_Size), nca.Size.ToFileSize()));
                rows.Add(new(L(FvLocale.Prop_HeaderSignature), nca.HeaderSignatureValidity));
                rows.Add(new(L(FvLocale.Prop_HashValid), nca.HashValid));
                rows.Add(new(L(FvLocale.Prop_ContentType), nca.ContentType));
                rows.Add(new(L(FvLocale.Prop_SdkVersion), nca.SdkVersion));
                rows.Add(new(L(FvLocale.Prop_DistributionType), nca.DistributionType));
                rows.Add(new(L(FvLocale.Prop_KeyGeneration), nca.KeyGeneration));
                rows.Add(new(L(FvLocale.Prop_ContentIndex), nca.ContentIndex));
                rows.Add(new(L(FvLocale.Prop_FormatVersion), nca.FormatVersion));
                rows.Add(new(L(FvLocale.Prop_Encrypted), nca.IsEncrypted));
                rows.Add(new(L(FvLocale.Prop_IsNca0), nca.IsNca0));
                break;

            case TicketItem ticket:
                rows.Add(new(L(FvLocale.Prop_Size), ticket.Size.ToFileSize()));
                rows.Add(new(L(FvLocale.Prop_RightsId), ticket.RightsId));
                rows.Add(new(L(FvLocale.Prop_AccessKey), ticket.AccessKey));
                rows.Add(new(L(FvLocale.Prop_Issuer), ticket.Issuer));
                rows.Add(new(L(FvLocale.Prop_AccountId), ticket.AccountId.ToStrId()));
                rows.Add(new(L(FvLocale.Prop_DeviceId), ticket.DeviceId.ToStrId()));
                rows.Add(new(L(FvLocale.Prop_CryptoType), ticket.CryptoType));
                rows.Add(new(L(FvLocale.Prop_FormatVersion), ticket.FormatVersion));
                rows.Add(new(L(FvLocale.Prop_TicketId), ticket.TicketId.ToStrId()));
                rows.Add(new(L(FvLocale.Prop_TitleKeyType), ticket.TitleKeyType));
                rows.Add(new(L(FvLocale.Prop_LicenseType), ticket.LicenseType));
                rows.Add(new(L(FvLocale.Prop_TicketVersion), ticket.TicketVersion));
                rows.Add(new(L(FvLocale.Prop_PropertyMask), ticket.PropertyMask));
                break;

            case PartitionFileEntryItemBase partitionFile:
                rows.Add(new(L(FvLocale.Prop_Size), partitionFile.Size.ToFileSize()));
                break;

            case SectionItem section:
                rows.Add(new(L(FvLocale.Prop_SectionIndex), section.SectionIndex));
                rows.Add(new(L(FvLocale.Prop_NcaSectionType), section.NcaSectionType));
                rows.Add(new(L(FvLocale.Prop_EncryptionType), section.FsHeader.EncryptionType));
                rows.Add(new(L(FvLocale.Prop_HashType), section.FsHeader.HashType));
                rows.Add(new(L(FvLocale.Prop_Version), section.FsHeader.Version));
                rows.Add(new(L(FvLocale.Prop_ContentType), section.SectionType));
                rows.Add(new(L(FvLocale.Prop_Sparse), section.IsSparse));
                AddIfNotNull(rows, L(FvLocale.Prop_SparseGeneration), section.SparseInfo?.Generation);
                AddIfNotNull(rows, L(FvLocale.Prop_SparseMetaSize), section.SparseInfo?.MetaSize);
                AddIfNotNull(rows, L(FvLocale.Prop_SparseMetaOffset), section.SparseInfo?.MetaOffset);
                AddIfNotNull(rows, L(FvLocale.Prop_SparsePhysicalOffset), section.SparseInfo?.PhysicalOffset);
                AddIfNotNull(rows, L(FvLocale.Prop_PatchRelocationTreeOffset), section.PatchInfo?.RelocationTreeOffset);
                AddIfNotNull(rows, L(FvLocale.Prop_PatchRelocationTreeSize), section.PatchInfo?.RelocationTreeSize);
                AddIfNotNull(rows, L(FvLocale.Prop_PatchEncryptionTreeOffset), section.PatchInfo?.EncryptionTreeOffset);
                AddIfNotNull(rows, L(FvLocale.Prop_PatchEncryptionTreeSize), section.PatchInfo?.EncryptionTreeSize);
                break;

            case CnmtContentEntryItem entry:
                rows.Add(new(L(FvLocale.Prop_NcaId), entry.NcaId));
                rows.Add(new(L(FvLocale.Prop_NcaHash), Convert.ToHexString(entry.NcaHash)));
                rows.Add(new(L(FvLocale.Prop_NcaContentType), entry.NcaContentType));
                rows.Add(new(L(FvLocale.Prop_NcaSize), entry.NcaSize.ToFileSize()));
                break;

            case XciPartitionItem partition:
                rows.Add(new(L(FvLocale.Prop_PartitionType), partition.PartitionType));
                rows.Add(new(L(FvLocale.Prop_XciPartitionType), partition.XciPartitionType));
                rows.Add(new(L(FvLocale.Prop_Entries), partition.NbEntries));
                break;

            case PartitionFileSystemItemBase partition:
                rows.Add(new(L(FvLocale.Prop_PartitionType), partition.PartitionType));
                rows.Add(new(L(FvLocale.Prop_Entries), partition.NbEntries));
                break;
        }

        return rows;
    }

    public static string ToDisplayName(NacpLanguage language)
    {
        return language switch {
            NacpLanguage.AmericanEnglish => L(FvLocale.Lng_AmericanEnglish),
            NacpLanguage.BritishEnglish => L(FvLocale.Lng_BritishEnglish),
            NacpLanguage.CanadianFrench => L(FvLocale.Lng_CanadianFrench),
            NacpLanguage.Dutch => L(FvLocale.Lng_Dutch),
            NacpLanguage.French => L(FvLocale.Lng_French),
            NacpLanguage.German => L(FvLocale.Lng_German),
            NacpLanguage.Italian => L(FvLocale.Lng_Italian),
            NacpLanguage.Japanese => L(FvLocale.Lng_Japanese),
            NacpLanguage.Korean => L(FvLocale.Lng_Korean),
            NacpLanguage.LatinAmericanSpanish => L(FvLocale.Lng_LatinAmericanSpanish),
            NacpLanguage.Portuguese => L(FvLocale.Lng_Portuguese),
            NacpLanguage.Russian => L(FvLocale.Lng_Russian),
            NacpLanguage.SimplifiedChinese => L(FvLocale.Lng_SimplifiedChinese),
            NacpLanguage.Spanish => L(FvLocale.Lng_Spanish),
            NacpLanguage.TraditionalChinese => L(FvLocale.Lng_TraditionalChinese),
            NacpLanguage.BrazilianPortuguese => L(FvLocale.Lng_BrazilianPortuguese),
            _ => L(FvLocale.Lng_Unknown)
        };
    }

    public static string ToDisplayName(NcasIntegrity integrity)
    {
        return integrity switch {
            NcasIntegrity.NoNca => L(FvLocale.NcasIntegrity_NoNca),
            NcasIntegrity.Unchecked => L(FvLocale.NcasIntegrity_Unchecked),
            NcasIntegrity.InProgress => L(FvLocale.NcasIntegrity_InProgress),
            NcasIntegrity.Original => L(FvLocale.NcasIntegrity_Original),
            NcasIntegrity.Incomplete => L(FvLocale.NcasIntegrity_Incomplete),
            NcasIntegrity.Modified => L(FvLocale.NcasIntegrity_Modified),
            NcasIntegrity.Corrupted => L(FvLocale.NcasIntegrity_Corrupted),
            NcasIntegrity.Error => L(FvLocale.NcasIntegrity_Error),
            _ => L(FvLocale.NcasIntegrity_Unknown)
        };
    }

    private static void AddDirectoryRows(List<ItemPropertyRow> rows, DirectoryEntryItem directory)
    {
        rows.Add(new(L(FvLocale.Prop_Size), directory.Size.ToFileSize()));
        rows.Add(new(L(FvLocale.Prop_EntryType), directory.DirectoryEntryType));
    }

    private static void AddIfNotNull(List<ItemPropertyRow> rows, string name, object? value)
    {
        if (value != null) {
            rows.Add(new(name, value));
        }
    }

    private static string L(FvLocale key) => Locale[key];
}

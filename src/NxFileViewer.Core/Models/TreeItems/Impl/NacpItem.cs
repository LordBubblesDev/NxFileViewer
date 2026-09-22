using Emignatik.NxFileViewer.Utils;
using LibHac.Ns;
using LibHac.Tools.Fs;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public class NacpItem : DirectoryEntryItem
{
    public const string NacpFileName = "control.nacp";

    public NacpItem(ApplicationControlProperty nacp, SectionItem parentItem, DirectoryEntryEx directoryEntry) : base(parentItem, directoryEntry)
    {
        Nacp = nacp;
        AddOnContentBaseId = Nacp.AddOnContentBaseId.ToStrId();
        PresenceGroupId = Nacp.PresenceGroupId.ToStrId();
        SaveDataOwnerId = Nacp.SaveDataOwnerId.ToStrId();
    }


    public ApplicationControlProperty Nacp { get; }

    public override string Format => nameof(Nacp);

    public ApplicationControlProperty.StartupUserAccountValue StartupUserAccount => Nacp.StartupUserAccount;

    public ApplicationControlProperty.UserAccountSwitchLockValue UserAccountSwitchLock => Nacp.UserAccountSwitchLock;

    public ApplicationControlProperty.AddOnContentRegistrationTypeValue AddOnContentRegistrationType => Nacp.AddOnContentRegistrationType;

    public ApplicationControlProperty.AttributeFlagValue Attribute => Nacp.AttributeFlag;
    
    public ApplicationControlProperty.ParentalControlFlagValue ParentalControl => Nacp.ParentalControlFlag;

    public ApplicationControlProperty.ScreenshotValue Screenshot => Nacp.Screenshot;

    public ApplicationControlProperty.VideoCaptureValue VideoCapture => Nacp.VideoCapture;

    public string PresenceGroupId { get; }

    public string DisplayVersion => Nacp.DisplayVersionString.ToString();
    
    public string AddOnContentBaseId { get; }

    public string SaveDataOwnerId { get; }

    public string Isbn => Nacp.IsbnString.ToString();
}
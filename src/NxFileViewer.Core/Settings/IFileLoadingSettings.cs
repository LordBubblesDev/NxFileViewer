using System.ComponentModel;

namespace Emignatik.NxFileViewer.Settings;

public interface IFileLoadingSettings : INotifyPropertyChanged
{
    string ProdKeysFilePath { get; set; }
    string TitleKeysFilePath { get; set; }
    bool AlwaysReloadKeysBeforeOpen { get; }
    bool InjectTicketKeys { get; }
    bool OpenBlocklessCompressionNcz { get; }
    bool IgnoreMissingDeltaFragments { get; }
    int ProgressBufferSize { get; }
}

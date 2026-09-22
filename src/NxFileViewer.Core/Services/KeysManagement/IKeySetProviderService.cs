using System.ComponentModel;
using LibHac.Common.Keys;

namespace Emignatik.NxFileViewer.Services.KeysManagement;

public interface IKeySetProviderService : INotifyPropertyChanged
{
    public const string DefaultProdKeysFileName = "prod.keys";
    public const string DefaultTitleKeysFileName = "title.keys";
    public string? ActualProdKeysFilePath { get; }
    KeySet GetKeySet(bool forceReload = false);
}
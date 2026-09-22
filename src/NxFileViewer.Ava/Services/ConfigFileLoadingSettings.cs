using System.ComponentModel;
using Emignatik.NxFileViewer.Settings;

namespace NxFileViewer.Ava.Services;

public sealed class ConfigFileLoadingSettings : IFileLoadingSettings
{
    public ConfigFileLoadingSettings()
    {
        Config.Shared.PropertyChanged += OnConfigChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProdKeysFilePath
    {
        get => Config.Shared.ProductionKeysPath ?? string.Empty;
        set => Config.Shared.ProductionKeysPath = value;
    }

    public string TitleKeysFilePath
    {
        get => Config.Shared.TitleKeysPath ?? string.Empty;
        set => Config.Shared.TitleKeysPath = value;
    }

    public bool AlwaysReloadKeysBeforeOpen => Config.Shared.AlwaysReloadKeys;

    public bool InjectTicketKeys => Config.Shared.InjectTicketFileKeys;

    public bool OpenBlocklessCompressionNcz => Config.Shared.OpenNczCompressedWithoutBlock;

    public bool IgnoreMissingDeltaFragments => Config.Shared.IgnoreMissingDeltaFragments;

    public int ProgressBufferSize => 4 * 1024 * 1024;

    private void OnConfigChanged(object? sender, PropertyChangedEventArgs e)
    {
        var mapped = e.PropertyName switch {
            nameof(Config.ProductionKeysPath) => nameof(ProdKeysFilePath),
            nameof(Config.TitleKeysPath) => nameof(TitleKeysFilePath),
            nameof(Config.AlwaysReloadKeys) => nameof(AlwaysReloadKeysBeforeOpen),
            nameof(Config.InjectTicketFileKeys) => nameof(InjectTicketKeys),
            nameof(Config.OpenNczCompressedWithoutBlock) => nameof(OpenBlocklessCompressionNcz),
            nameof(Config.IgnoreMissingDeltaFragments) => nameof(IgnoreMissingDeltaFragments),
            _ => null
        };

        if (mapped != null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(mapped));
        }
    }
}

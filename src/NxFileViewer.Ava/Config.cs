using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ConfigFactory.Core;
using ConfigFactory.Core.Attributes;
using NxFileViewer.Ava.Models;

namespace NxFileViewer.Ava;

public partial class Config : ConfigModule<Config>
{
    public override string Name => "NxFileViewer";

    [ObservableProperty]
    [Config(Header = "Theme", Description = "", Group = "Application")]
    [DropdownConfig("Dark", "Light")]
    public partial string Theme { get; set; } = "Dark";

    partial void OnThemeChanged(string value)
    {
        Application.Current?.RequestedThemeVariant = value switch {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
    
    [ObservableProperty]
    [Config(
        Header = "System Language",
        Description = "The language to use in the user interface (restart required)",
        Group = "Application")]
    [DropdownConfig(
        DisplayMemberPath = nameof(SystemLanguage.DisplayName),
        RuntimeItemsSourceMethodName = nameof(GetLanguagesInternal))]
    public partial SystemLanguage CultureName { get; set; } = "en_US";
    
    [ObservableProperty]
    [Config(Header = "Title Page URL", Description = "", Group = "Application")]
    public partial string TitlePageUrl { get; set; } = "https://tinfoil.media/Title/{TitleId}";
    
    [ObservableProperty]
    [Config(
        Header = "Always Reload Keys",
        Description = "Ensure the keys are always reloaded before opening a file.",
        Group = "Keys")]
    public partial bool AlwaysReloadKeys { get; set; }

    [ObservableProperty]
    [Config(
        Header = "Inject Keys from Ticket Files",
        Description = "",
        Group = "Keys")]
    public partial bool InjectTicketFileKeys { get; set; } = true;

    [ObservableProperty]
    [Config(
        Header = "Production Keys Path",
        Description = "The absolute path to your dumped prod.keys file",
        Group = "Keys")]
    [BrowserConfig(BrowserMode = BrowserMode.OpenFile, Filter = "Keys File:*.keys|Any File:*.*")]
    public partial string? ProductionKeysPath { get; set; }

    [ObservableProperty]
    [Config(
        Header = "Title Keys Path",
        Description = "The absolute path to your dumped title.keys file",
        Group = "Keys")]
    [BrowserConfig(BrowserMode = BrowserMode.OpenFile, Filter = "Keys File:*.keys|Any File:*.*")]
    public partial string? TitleKeysPath { get; set; }

    [ObservableProperty]
    [Config(Header = "Open NCZ Compressed Without Block", Description = "", Group = "NSZ/XCZ")]
    public partial bool OpenNczCompressedWithoutBlock { get; set; } = true;

    [ObservableProperty]
    [Config(Header = "Ignore Missing Delta Fragments", Description = "", Group = "Integrity")]
    public partial bool IgnoreMissingDeltaFragments { get; set; } = true;

    public static List<SystemLanguage> GetLanguagesInternal()
        => Locale.Languages.Select(x => new SystemLanguage(x)).ToList();
}

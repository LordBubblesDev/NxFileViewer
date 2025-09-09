using CommunityToolkit.Mvvm.ComponentModel;
using ConfigFactory.Core;
using ConfigFactory.Core.Attributes;
using NxFileViewer.Ava.Models;

namespace NxFileViewer.Ava;

public partial class Config : ConfigModule<Config>
{
    public override string Name { get; } = "CafeEventEditor";

    public event Action<string> ThemeChanged = delegate { };

    [ObservableProperty]
    [Config(Header = "Theme", Description = "", Group = "Application")]
    [DropdownConfig("Dark", "Light")]
    public partial string Theme { get; set; } = "Dark";

    partial void OnThemeChanged(string value)
    {
        ThemeChanged?.Invoke(value);
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

    public static List<SystemLanguage> GetLanguagesInternal()
        => Locale.Languages.Select(x => new SystemLanguage(x)).ToList();
}

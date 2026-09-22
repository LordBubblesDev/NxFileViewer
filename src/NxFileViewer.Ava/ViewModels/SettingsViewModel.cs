using ConfigFactory;
using ConfigFactory.Avalonia;
using ConfigFactory.Models;
using FluentAvalonia.UI.Controls;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.Models;

namespace NxFileViewer.Ava.ViewModels;

public class SettingsViewModel : Document
{
    private static readonly ConfigPage configPage = new();

    static SettingsViewModel()
    {
        if (configPage.DataContext is not ConfigPageModel context) {
            return;
        }
        
        context.SecondaryButtonIsEnabled = false;
        context.Append<Config>();
    }

    public SettingsViewModel() : base(Locale[FvLocale.SettingsView_Title], FASymbol.Settings)
    {
        Content = configPage;
    }
}

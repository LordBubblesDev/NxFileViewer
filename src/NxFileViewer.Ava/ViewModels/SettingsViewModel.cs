using ConfigFactory;
using ConfigFactory.Avalonia;
using ConfigFactory.Models;
using FluentAvalonia.UI.Controls;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.Models;

namespace NxFileViewer.Ava.ViewModels;

public class SettingsViewModel : Document
{
    public SettingsViewModel() : base(Locale[FvLocale.SettingsView_Title], FASymbol.Settings)
    {
        var configPage = new ConfigPage();
        if (configPage.DataContext is ConfigPageModel context) {
            context.SecondaryButtonIsEnabled = false;
            context.Append<Config>();
        }

        Content = configPage;
    }
}

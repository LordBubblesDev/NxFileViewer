using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ConfigFactory.Avalonia.Helpers;
using NxFileViewer.Ava.ViewModels;
using NxFileViewer.Ava.Views;
using NxFileViewer.Ava.Localization;

namespace NxFileViewer.Ava;

public class App : Application
{
    public static readonly string Version = typeof(App).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? Locale[FvLocale.UndefinedVersion];
    
    public static string Title => "NX File Viewer";
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
            return;
        }
        
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);
        
        desktop.MainWindow = new ShellView {
            DataContext = new ShellViewModel(),
        };
        
        BrowserDialog.StorageProvider = desktop.MainWindow.StorageProvider;

        base.OnFrameworkInitializationCompleted();
    }

    private void SettingsMenu_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow.DataContext: ShellViewModel vm }) {
            return;
        }

        vm.OpenSettings();
    }
}
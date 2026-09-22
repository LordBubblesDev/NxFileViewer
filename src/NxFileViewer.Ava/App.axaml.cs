using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ConfigFactory.Avalonia.Helpers;
using Emignatik.NxFileViewer.Localization;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.Services;
using NxFileViewer.Ava.ViewModels;
using NxFileViewer.Ava.Views;

namespace NxFileViewer.Ava;

public class App : Application
{
    public static readonly string Version = typeof(App).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? Locale[FvLocale.UndefinedVersion];
    
    public static string Title => "NX File Viewer";
    
    public override void Initialize()
    {
        LocalizationManager.Resolve = key => Locale[key, failSoftly: true];
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
            return;
        }

        RequestedThemeVariant = Config.Shared.Theme switch {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        _ = AppServices.Current; // load keys and warn if prod.keys is missing

        var viewModel = new ShellViewModel();
        var shellView = new ShellView
        {
            DataContext = viewModel
        };

        viewModel.StorageProvider = shellView.StorageProvider;
        desktop.MainWindow = shellView;
        BrowserDialog.StorageProvider = shellView.StorageProvider;

        if (desktop.Args is { Length: > 0 } args) {
            var filePath = args[0];

            if (File.Exists(filePath)) {
                shellView.Opened += async (_, _) => await viewModel.OpenFilePath(filePath);
            }
        }

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

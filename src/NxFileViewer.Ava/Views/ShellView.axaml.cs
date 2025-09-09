using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using NxFileViewer.Ava.Models;
using NxFileViewer.Ava.ViewModels;

namespace NxFileViewer.Ava.Views;

public partial class ShellView : AppWindow
{
    public ShellView()
    {
        InitializeComponent();
        
        Bitmap bitmap = new(AssetLoader.Open(new Uri("avares://NxFileViewer.Ava/Assets/icon.ico")));
        Icon = bitmap.CreateScaledBitmap(new PixelSize(48, 48));
    }

    private async void TabViewItem_OnCloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
    {
        try {
            if (DataContext is not ShellViewModel vm) {
                return;
            }
        
            if (args.Item is Document doc && await doc.CloseRequested()) {
                int index = vm.Documents.IndexOf(doc);
                vm.Documents.RemoveAt(index);
                vm.CurrentDocument = null;

                if (vm.Documents.Count == 0) {
                    return;
                }

                vm.CurrentDocument = index == vm.Documents.Count
                    ? vm.Documents[--index] : vm.Documents[index];
            }
        }
        catch (Exception ex) {
            await new ContentDialog {
                Title = ex.Message,
                Content = ex.ToString(),
                PrimaryButtonText = "ok"
            }.ShowAsync();
        }
    }
}
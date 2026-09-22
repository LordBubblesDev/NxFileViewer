using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.Models;
using NxFileViewer.Ava.ViewModels;

namespace NxFileViewer.Ava.Views;

public partial class ShellView : FAAppWindow
{
    public ShellView()
    {
        InitializeComponent();

        if (!OperatingSystem.IsMacOS()) {
            TitleBar.ExtendsContentIntoTitleBar = true;
            ExtendClientAreaToDecorationsHint = true;
        }

        Bitmap bitmap = new(AssetLoader.Open(new Uri("avares://NxFileViewer.Ava/Assets/icon.ico")));
        Icon = bitmap.CreateScaledBitmap(new PixelSize(48, 48));

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File) != true) {
            return;
        }

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ShellViewModel vm || e.DataTransfer.Contains(DataFormat.File) != true) {
            return;
        }

        var storageItems = e.DataTransfer.TryGetFiles();
        if (storageItems == null) {
            return;
        }

        var paths = storageItems
            .Select(item => item.TryGetLocalPath())
            .Where(path => !string.IsNullOrEmpty(path))
            .Cast<string>()
            .ToArray();

        if (paths.Length > 0) {
            await vm.OnFilesDropped(paths);
        }
    }

    private async void MainTabView_OnTabCloseRequested(FATabView sender, FATabViewTabCloseRequestedEventArgs args)
    {
        try {
            if (DataContext is not ShellViewModel vm) {
                return;
            }

            if (args.Item is Document doc && await doc.CloseRequested()) {
                vm.RemoveDocument(doc);
            }
        }
        catch (Exception ex) {
            await new FAContentDialog
            {
                Title = ex.Message,
                Content = ex.ToString(),
                PrimaryButtonText = Locale[FvLocale.Dialog_OK]
            }.ShowAsync();
        }
    }
}

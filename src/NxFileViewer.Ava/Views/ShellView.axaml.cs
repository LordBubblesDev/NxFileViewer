using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;

namespace NxFileViewer.Ava.Views;

public partial class ShellView : AppWindow
{
    public ShellView()
    {
        InitializeComponent();
        
        Bitmap bitmap = new(AssetLoader.Open(new Uri("avares://NxFileViewer.Ava/Assets/icon.ico")));
        Icon = bitmap.CreateScaledBitmap(new PixelSize(48, 48));
    }
}
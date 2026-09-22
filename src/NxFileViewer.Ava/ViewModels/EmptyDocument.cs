using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.Models;

namespace NxFileViewer.Ava.ViewModels;

public sealed class EmptyDocument : Document
{
    public EmptyDocument(ICommand openFileCommand)
        : base(Locale[FvLocale.EmptyTab_Title], Symbol.OpenFile)
    {
        OpenFileCommand = openFileCommand;
        Content = CreateContent();
    }

    private ICommand OpenFileCommand { get; }

    private Control CreateContent()
    {
        var hint = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Opacity = 0.7,
            FontSize = 16,
            Text = Locale[FvLocale.DragMeAFile]
        };

        var openButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = Locale[FvLocale.MenuItem_OpenFile],
            Command = OpenFileCommand
        };

        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 12,
            Children = { hint, openButton }
        };
    }
}

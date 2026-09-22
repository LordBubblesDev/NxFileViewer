using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using NxFileViewer.Ava.Localization;
using NxFileViewer.Ava.ViewModels;

namespace NxFileViewer.Ava.Views;

public partial class TreeNodeView : UserControl
{
    public TreeNodeView()
    {
        InitializeComponent();
        AddHandler(ContextRequestedEvent, OnRowContextRequested, RoutingStrategies.Bubble, handledEventsToo: true);
        RowBorder.PointerPressed += (_, _) => {
            if (DataContext is TreeNodeViewModel node) {
                node.SelectCommand.Execute(null);
            }
        };
    }

    private void OnRowContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (DataContext is not TreeNodeViewModel node) {
            return;
        }

        if (e.Source is not Visual source || !IsFromThisRow(source)) {
            return;
        }

        node.SelectCommand.Execute(null);

        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem
        {
            Header = Locale[FvLocale.ContextMenu_ShowItemErrors],
            Command = node.ShowErrorsCommand,
            IsEnabled = node.CanShowErrors
        });

        AddSaveItem(flyout, Locale[FvLocale.ContextMenu_SaveNcaFileRaw], node.SaveNcaRawCommand, node.CanSaveNca);
        AddSaveItem(flyout, Locale[FvLocale.ContextMenu_SaveNcaFilePlaintext], node.SaveNcaPlaintextCommand, node.CanSaveNca);
        AddSaveItem(flyout, Locale[FvLocale.ContextMenu_SavePartitionFileItem], node.SavePartitionCommand, node.CanSavePartition);
        AddSaveItem(flyout, Locale[FvLocale.ContextMenu_SaveSectionItem], node.SaveSectionCommand, node.CanSaveSection);
        AddSaveItem(flyout, Locale[FvLocale.ContextMenu_SaveDirectoryItem], node.SaveEntryCommand, node.CanSaveDirectory);
        AddSaveItem(flyout, Locale[FvLocale.ContextMenu_SaveFileItem], node.SaveEntryCommand, node.CanSaveFile);

        flyout.ShowAt(RowBorder, showAtPointer: true);

        e.Handled = true;
    }

    private bool IsFromThisRow(Visual source)
    {
        foreach (var ancestor in source.GetSelfAndVisualAncestors()) {
            if (ReferenceEquals(ancestor, ChildItems)) {
                return false;
            }
            if (ReferenceEquals(ancestor, RowBorder)) {
                return true;
            }
        }

        return false;
    }

    private static void AddSaveItem(MenuFlyout flyout, string header, System.Windows.Input.ICommand command, bool isVisible)
    {
        if (!isVisible) {
            return;
        }

        flyout.Items.Add(new MenuItem
        {
            Header = header,
            Command = command
        });
    }
}

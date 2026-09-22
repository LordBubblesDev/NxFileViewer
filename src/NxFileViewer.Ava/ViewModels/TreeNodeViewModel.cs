using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emignatik.NxFileViewer.Models.TreeItems;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using LibHac.Fs;

namespace NxFileViewer.Ava.ViewModels;

public sealed partial class TreeNodeViewModel : ObservableObject, IDisposable
{
    private readonly OpenedFileViewModel _owner;

    public TreeNodeViewModel(IItem item, OpenedFileViewModel owner, bool expand = false)
    {
        Item = item;
        _owner = owner;
        DisplayName = item.DisplayName;
        Format = item.Format;
        Children = item.ChildItems.Select(child => new TreeNodeViewModel(child, owner)).ToArray();
        HasChildren = Children.Count > 0;
        IsExpanded = expand || !HasChildren;
        CanShowErrors = item.Errors.Count > 0;

        switch (item) {
            case NcaItem:
                CanSaveNca = true;
                break;
            case PartitionFileEntryItemBase:
                CanSavePartition = true;
                break;
            case SectionItem { ChildItems.Length: > 0 }:
                CanSaveSection = true;
                break;
            case DirectoryEntryItem { DirectoryEntryType: DirectoryEntryType.Directory }:
                CanSaveDirectory = true;
                break;
            case DirectoryEntryItem:
                CanSaveFile = true;
                break;
        }

        _owner.PropertyChanged += OnOwnerPropertyChanged;
        UpdateSelected();
    }

    private IItem Item { get; }

    public string DisplayName { get; }

    public string? Format { get; }

    public IReadOnlyList<TreeNodeViewModel> Children { get; }

    public bool HasChildren { get; }

    public bool CanShowErrors { get; }

    public bool CanSaveNca { get; }

    public bool CanSavePartition { get; }

    public bool CanSaveSection { get; }

    public bool CanSaveDirectory { get; }

    public bool CanSaveFile { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string ExpanderGlyph => HasChildren ? (IsExpanded ? "▾" : "▸") : " ";

    [RelayCommand]
    private void Select()
    {
        _owner.SelectedItem = Item;
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        if (HasChildren) {
            IsExpanded = !IsExpanded;
        }
    }

    [RelayCommand]
    private async Task ShowErrors()
    {
        _owner.SelectedItem = Item;
        await _owner.ShowItemErrorsCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task SaveNcaRaw()
    {
        _owner.SelectedItem = Item;
        await _owner.SaveNcaRawCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task SaveNcaPlaintext()
    {
        _owner.SelectedItem = Item;
        await _owner.SaveNcaPlaintextCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task SavePartition()
    {
        _owner.SelectedItem = Item;
        await _owner.SavePartitionFileCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task SaveSection()
    {
        _owner.SelectedItem = Item;
        await _owner.SaveSectionCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task SaveEntry()
    {
        _owner.SelectedItem = Item;
        await _owner.SaveDirectoryEntryCommand.ExecuteAsync(null);
    }

    public void Dispose()
    {
        _owner.PropertyChanged -= OnOwnerPropertyChanged;
        foreach (var child in Children) {
            child.Dispose();
        }
    }

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ExpanderGlyph));
    }

    private void OnOwnerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OpenedFileViewModel.SelectedItem)) {
            UpdateSelected();
        }
    }

    private void UpdateSelected()
    {
        IsSelected = ReferenceEquals(_owner.SelectedItem, Item);
    }
}

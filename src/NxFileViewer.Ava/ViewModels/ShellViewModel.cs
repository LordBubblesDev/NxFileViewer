using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NxFileViewer.Ava.Models;

namespace NxFileViewer.Ava.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private static readonly SettingsViewModel _settings = new();
    
    public ObservableCollection<Document> Documents { get; } = [];
    
    [ObservableProperty]
    public partial Document? CurrentDocument { get; set; }

    [RelayCommand]
    public void OpenSettings()
    {
        if (Documents.IndexOf(_settings) == -1) {
            Documents.Add(_settings);
        }

        CurrentDocument = _settings;
    }
}
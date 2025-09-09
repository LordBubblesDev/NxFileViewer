using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;

namespace NxFileViewer.Ava.Models;

public partial class Document : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial Symbol Icon { get; set; }

    [ObservableProperty]
    public partial object? Content { get; set; }
    
    public virtual Task<bool> CloseRequested()
    {
        return Task.FromResult(true);
    }

    protected Document(string title, Symbol icon = Symbol.Document)
    {
        Title = title;
        Icon = icon;
    }
}
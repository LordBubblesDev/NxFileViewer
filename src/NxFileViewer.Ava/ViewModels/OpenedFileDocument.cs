using Emignatik.NxFileViewer.Models;
using FluentAvalonia.UI.Controls;
using NxFileViewer.Ava.Models;
using NxFileViewer.Ava.Views;

namespace NxFileViewer.Ava.ViewModels;

public sealed class OpenedFileDocument : Document
{
    public OpenedFileDocument(NxFile nxFile)
        : base(nxFile.FileName)
    {
        ViewModel = new OpenedFileViewModel(nxFile);
        Content = new OpenedFileView
        {
            DataContext = ViewModel
        };
    }

    public OpenedFileViewModel ViewModel { get; }

    public override Task<bool> CloseRequested()
    {
        ViewModel.Dispose();
        return Task.FromResult(true);
    }
}

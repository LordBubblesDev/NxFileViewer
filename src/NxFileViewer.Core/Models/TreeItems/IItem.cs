using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Emignatik.NxFileViewer.Models.TreeItems;

public interface IItem : INotifyPropertyChanged, IDisposable
{
    string DisplayName { get; }
    string Name { get; }
    IItem? ParentItem { get; }
    IReadOnlyList<IItem> ChildItems { get; }
    string LibHacTypeName { get; }
    string? Format { get; }
    IItemErrors Errors { get; }
}
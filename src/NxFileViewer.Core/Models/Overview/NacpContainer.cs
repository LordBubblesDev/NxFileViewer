using System;
using System.Collections.Generic;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;

namespace Emignatik.NxFileViewer.Models.Overview;

public class NacpContainer(NacpItem nacpItem)
{
    public NacpItem NacpItem { get; } = nacpItem ?? throw new ArgumentNullException(nameof(nacpItem));
    public List<TitleInfo> Titles { get; } = [];
}
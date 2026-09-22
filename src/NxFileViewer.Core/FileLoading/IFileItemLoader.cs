using System;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using LibHac.Common.Keys;

namespace Emignatik.NxFileViewer.FileLoading;

public interface IFileItemLoader
{
    public event MissingKeyExceptionHandler? MissingKey;
    
    XciItem LoadXci(string xciFilePath);
    NspItem LoadNsp(string nspFilePath);
}

public delegate void MissingKeyExceptionHandler(object sender, MissingKeyExceptionHandlerArgs args);

public class MissingKeyExceptionHandlerArgs(MissingKeyException ex)
{
    public MissingKeyException Exception { get; } = ex ?? throw new ArgumentNullException(nameof(ex));
}
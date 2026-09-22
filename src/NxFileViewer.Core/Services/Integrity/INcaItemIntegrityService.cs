using System;
using System.Threading;
using System.Threading.Tasks;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using Emignatik.NxFileViewer.Services.BackgroundTask;

namespace Emignatik.NxFileViewer.Services.Integrity;

public interface INcaItemIntegrityService
{
    Task<NcaIntegrity> SafeCheckAsync(
        NcaItem ncaItem,
        byte[]? expectedHash,
        CancellationToken? cancellationToken = null,
        IProgressReporter? progressReporter = null,
        int? bufferSize = null,
        IntegrityCheckKind kind = IntegrityCheckKind.All);
}

[Flags]
public enum IntegrityCheckKind
{
    Signature = 1,
    Hash = 2,
    All = Signature | Hash
}


public enum NcaIntegrity
{
    /// <summary>
    /// Original Nintendo Content Archive (NCA) file.
    /// </summary>
    Original,
    /// <summary>
    /// File has been modified and is not original.
    /// </summary>
    Modified,
    /// <summary>
    /// File is corrupted.
    /// </summary>
    Corrupted,
    /// <summary>
    /// An error occurred while checking the file.
    /// </summary>
    Error,
}
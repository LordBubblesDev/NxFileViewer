using System.Threading;
using System.Threading.Tasks;
using Emignatik.NxFileViewer.Services.BackgroundTask;
using LibHac.Tools.FsSystem.NcaUtils;

namespace Emignatik.NxFileViewer.Services.Integrity;

public interface INcaHashService
{
    Task<byte[]> ComputeSha256Async(Nca nca, CancellationToken? cancellationToken = null, IProgressReporter? progressReporter = null, int? bufferSize = null);
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emignatik.NxFileViewer.Localization;
using Emignatik.NxFileViewer.Models.Overview;
using Emignatik.NxFileViewer.Models.TreeItems;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using Emignatik.NxFileViewer.Services.BackgroundTask;
using Emignatik.NxFileViewer.Settings;
using LibHac.Ncm;
using Microsoft.Extensions.Logging;

namespace Emignatik.NxFileViewer.Services.Integrity;

public interface INcasIntegrityVerifier
{
    Task VerifyAsync(FileOverview fileOverview, IProgressReporter progressReporter, CancellationToken cancellationToken, IntegrityCheckKind kind = IntegrityCheckKind.All);
}

public sealed class NcasIntegrityVerifier(
    IFileLoadingSettings settings,
    INcaItemIntegrityService ncaItemIntegrityService,
    ILogger<NcasIntegrityVerifier> logger)
    : INcasIntegrityVerifier
{
    private readonly ILogger _logger = logger;

    public async Task VerifyAsync(FileOverview fileOverview, IProgressReporter progressReporter, CancellationToken cancellationToken, IntegrityCheckKind kind = IntegrityCheckKind.All)
    {
        _logger.LogInformation(LoadingLocalizationKeys.NcaHashVerificationStartLog);
        try {
            await Verify(fileOverview, progressReporter, cancellationToken, kind);
        }
        finally {
            _logger.LogInformation(LoadingLocalizationKeys.NcaHashVerificationEndLog);
        }
    }

    private async Task Verify(FileOverview fileOverview, IProgressReporter progressReporter, CancellationToken cancellationToken, IntegrityCheckKind kind)
    {
        var ignoreMissingDeltaFragments = settings.IgnoreMissingDeltaFragments;
        var atLeastOneNcaMissing = false;
        var ncaItemsToProcess = new List<(NcaItem NcaItem, byte[]? ExpectedHash)>();

        foreach (var cnmtItem in fileOverview.CnmtContainers.Select(container => container.CnmtItem)) {
            ncaItemsToProcess.Add((cnmtItem.ParentItem.ParentItem, null));

            foreach (var cnmtContentEntryItem in cnmtItem.ChildItems) {
                cnmtContentEntryItem.Errors.RemoveAllOfCategory(Category.IntegrityCheck);

                var ncaItem = cnmtContentEntryItem.FindReferencedNcaItem();
                if (ncaItem == null) {
                    if (cnmtContentEntryItem.NcaContentType == ContentType.DeltaFragment && ignoreMissingDeltaFragments) {
                        continue;
                    }

                    atLeastOneNcaMissing = true;
                    cnmtContentEntryItem.Errors.Add(Category.IntegrityCheck, LoadingLocalizationKeys.NcasIntegrityErrorNcaMissing.SafeFormat(cnmtContentEntryItem.NcaId));
                    continue;
                }

                ncaItemsToProcess.Add((ncaItem, cnmtContentEntryItem.NcaHash));
            }
        }

        var atLeastOneModified = false;
        var atLeastOneCorrupted = false;
        var atLeastOneError = false;

        try {
            SetStatus(fileOverview, kind, NcasIntegrity.InProgress);

            var processedItem = 0;
            foreach (var (ncaItem, expectedHash) in ncaItemsToProcess) {
                cancellationToken.ThrowIfCancellationRequested();
                progressReporter.SetPercentage(0.0);
                var progressKey = kind == IntegrityCheckKind.Signature
                    ? LoadingLocalizationKeys.NcaSignatureProgressText
                    : LoadingLocalizationKeys.NcaHashProgressText;
                progressReporter.SetText(progressKey.SafeFormat(++processedItem, ncaItemsToProcess.Count));

                var ncaIntegrity = await ncaItemIntegrityService.SafeCheckAsync(ncaItem, expectedHash, cancellationToken, progressReporter, settings.ProgressBufferSize, kind);
                switch (ncaIntegrity) {
                    case NcaIntegrity.Modified:
                        atLeastOneModified = true;
                        break;
                    case NcaIntegrity.Corrupted:
                        atLeastOneCorrupted = true;
                        break;
                    case NcaIntegrity.Error:
                        atLeastOneError = true;
                        break;
                }
            }

            NcasIntegrity result;
            if (ncaItemsToProcess.Count <= 0) {
                result = NcasIntegrity.NoNca;
            }
            else if (atLeastOneError) {
                result = NcasIntegrity.Error;
            }
            else if (atLeastOneCorrupted) {
                result = NcasIntegrity.Corrupted;
            }
            else if (atLeastOneModified) {
                result = NcasIntegrity.Modified;
            }
            else if (atLeastOneNcaMissing) {
                result = NcasIntegrity.Incomplete;
            }
            else {
                result = NcasIntegrity.Original;
            }

            SetStatus(fileOverview, kind, result);
        }
        catch (OperationCanceledException) {
            _logger.LogWarning(LoadingLocalizationKeys.LogNcasIntegrityCanceled);
            SetStatus(fileOverview, kind, NcasIntegrity.Unchecked);
        }
        catch (Exception ex) {
            _logger.LogError(ex, LoadingLocalizationKeys.NcasIntegrityErrorLog.SafeFormat(ex.Message));
            SetStatus(fileOverview, kind, NcasIntegrity.Error);
        }
    }

    private static void SetStatus(FileOverview fileOverview, IntegrityCheckKind kind, NcasIntegrity status)
    {
        if (kind.HasFlag(IntegrityCheckKind.Signature)) {
            fileOverview.SignatureIntegrity = status;
        }
        if (kind.HasFlag(IntegrityCheckKind.Hash)) {
            fileOverview.HashIntegrity = status;
        }

        if (fileOverview.SignatureIntegrity == NcasIntegrity.InProgress || fileOverview.HashIntegrity == NcasIntegrity.InProgress) {
            fileOverview.NcasIntegrity = NcasIntegrity.InProgress;
        }
        else if (fileOverview.HashIntegrity != NcasIntegrity.Unchecked) {
            fileOverview.NcasIntegrity = fileOverview.HashIntegrity;
        }
        else {
            fileOverview.NcasIntegrity = fileOverview.SignatureIntegrity;
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emignatik.NxFileViewer.Localization;
using Emignatik.NxFileViewer.Models.TreeItems;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using Emignatik.NxFileViewer.Services.BackgroundTask;
using LibHac.Common;
using LibHac.Tools.FsSystem.NcaUtils;
using Microsoft.Extensions.Logging;
using Category = Emignatik.NxFileViewer.Models.TreeItems.Category;

namespace Emignatik.NxFileViewer.Services.Integrity;

public class NcaItemIntegrityService(INcaHashService ncaHashService, ILogger<NcaItemIntegrityService> logger)
    : INcaItemIntegrityService
{
    private readonly INcaHashService _ncaHashService = ncaHashService ?? throw new ArgumentNullException(nameof(ncaHashService));
    private readonly ILogger<NcaItemIntegrityService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<NcaIntegrity> SafeCheckAsync(
        NcaItem ncaItem,
        byte[]? expectedHash,
        CancellationToken? cancellationToken = null,
        IProgressReporter? progressReporter = default,
        int? bufferSize = null,
        IntegrityCheckKind kind = IntegrityCheckKind.All)
    {
        ArgumentNullException.ThrowIfNull(ncaItem);

        var checkSignature = kind.HasFlag(IntegrityCheckKind.Signature);
        var checkHash = kind.HasFlag(IntegrityCheckKind.Hash);

        ncaItem.Errors.RemoveAllOfCategory(Category.IntegrityCheck);

        Nca nca;
        try {
            nca = ncaItem.GetOriginalNca();
        }
        catch (Exception ex) {
            ncaItem.Errors.Add(Category.IntegrityCheck, LoadingLocalizationKeys.NcaIntegrityGetOriginalNcaError.SafeFormat(ex.Message));
            _logger.LogError(LoadingLocalizationKeys.NcaIntegrityGetOriginalNcaErrorLog.SafeFormat(ncaItem.DisplayName, ex.Message));
            return NcaIntegrity.Error;
        }

        cancellationToken?.ThrowIfCancellationRequested();

        bool signatureValid;
        Exception? signatureEx = null;
        if (!checkSignature) {
            signatureValid = ncaItem.HeaderSignatureValidity is Validity.Valid or Validity.Unchecked;
        }
        else {
            try {
                var signatureValidity = nca.VerifyHeaderSignature();
                ncaItem.HeaderSignatureValidity = signatureValidity;

                switch (signatureValidity) {
                    case Validity.Valid:
                        signatureValid = true;
                        _logger.LogInformation(LoadingLocalizationKeys.NcaHeaderSignatureValidLog.SafeFormat(ncaItem.DisplayName, signatureValidity.ToString()));
                        break;
                    default:
                        signatureValid = false;
                        ncaItem.Errors.Add(Category.IntegrityCheck, LoadingLocalizationKeys.NcaHeaderSignatureInvalid.SafeFormat(signatureValidity.ToString()));
                        _logger.LogError(LoadingLocalizationKeys.NcaHeaderSignatureInvalidLog.SafeFormat(ncaItem.DisplayName, signatureValidity.ToString()));
                        break;
                }
            }
            catch (Exception ex) {
                signatureValid = false;
                signatureEx = ex;
                ncaItem.Errors.Add(Category.IntegrityCheck, LoadingLocalizationKeys.NcaHeaderSignatureError.SafeFormat(ex.Message));
                _logger.LogError(ex, LoadingLocalizationKeys.NcaHeaderSignatureErrorLog.SafeFormat(ncaItem.DisplayName, ex.Message));
            }
        }

        cancellationToken?.ThrowIfCancellationRequested();

        bool hashValid = ncaItem.HashValid != false;
        Exception? hashEx = null;
        if (checkHash) {
            try {
                var actualHash = await _ncaHashService.ComputeSha256Async(nca, cancellationToken, progressReporter, bufferSize);

                if (expectedHash == null) {
                    if (ncaItem.TryGetExpectedHashFromId(out var expectedHashFromId)) {
                        // Hash from name is shorter than the real hash
                        var actualHashShort = actualHash[new Range(0, expectedHashFromId.Length)];
                        hashValid = actualHashShort.SequenceEqual(expectedHashFromId);
                    }
                    else {
                        ncaItem.Errors.Add(Category.IntegrityCheck, LoadingLocalizationKeys.NcaHashNcaItemCantExtractHashFromName);
                        _logger.LogError(LoadingLocalizationKeys.NcaHashCantExtractHashFromNameLog.SafeFormat(ncaItem.DisplayName));
                        hashValid = false;
                    }
                }
                else {
                    hashValid = expectedHash.SequenceEqual(actualHash);
                }

                ncaItem.HashValid = hashValid;
                if (!hashValid) {
                    ncaItem.Errors.Add(Category.IntegrityCheck, LoadingLocalizationKeys.NcaHashNcaItemInvalid);
                    _logger.LogError(LoadingLocalizationKeys.NcaHashInvalidLog.SafeFormat(ncaItem.DisplayName));
                }
                else {
                    _logger.LogInformation(LoadingLocalizationKeys.NcaHashValidLog.SafeFormat(ncaItem.DisplayName));
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex) {
                hashValid = false;
                hashEx = ex;
                ncaItem.Errors.Add(Category.IntegrityCheck, LoadingLocalizationKeys.NcaHashNcaItemException.SafeFormat(ex.Message));
                _logger.LogError(ex, LoadingLocalizationKeys.NcaHashExceptionLog.SafeFormat(ncaItem.DisplayName, ex.Message));
            }
        }

        if (signatureEx != null || hashEx != null) {
            return NcaIntegrity.Error;
        }

        if (checkSignature && !checkHash) {
            return signatureValid ? NcaIntegrity.Original : NcaIntegrity.Modified;
        }

        if (checkHash && !checkSignature) {
            return hashValid ? NcaIntegrity.Original : NcaIntegrity.Corrupted;
        }

        return (signatureValid, hashValid) switch {
            (false, false) => NcaIntegrity.Corrupted,
            (true, false) => NcaIntegrity.Corrupted,
            (false, true) => NcaIntegrity.Modified,
            (true, true) => NcaIntegrity.Original
        };
    }
}

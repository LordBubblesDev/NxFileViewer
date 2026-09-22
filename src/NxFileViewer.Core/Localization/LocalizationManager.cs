using System;
using System.Runtime.CompilerServices;

namespace Emignatik.NxFileViewer.Localization;

public static class LocalizationManager
{
    public static Func<string, string>? Resolve { get; set; }

    internal static string Get([CallerMemberName] string key = "")
        => Resolve?.Invoke(key) ?? key;
}

public static class LoadingLocalizationKeys
{
    public static string SuspiciousFileExtension => LocalizationManager.Get();
    public static string LoadingErrorFailedToCheckIfXciPartitionExists => LocalizationManager.Get();
    public static string LoadingErrorFailedToOpenXciPartition => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadXciContent => LocalizationManager.Get();
    public static string LoadingErrorFailedToOpenPartitionFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadNcaFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadPartitionFileSystemContent => LocalizationManager.Get();
    public static string LoadingErrorFailedToCheckIfSectionCanBeOpened => LocalizationManager.Get();
    public static string LoadingErrorFailedToOpenNcaSectionFileSystem => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadSectionContent => LocalizationManager.Get();
    public static string LoadingErrorFailedToGetFileSystemDirectoryEntries => LocalizationManager.Get();
    public static string LoadingErrorFailedToOpenNacpFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadNacpFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToOpenCnmtFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadCnmtFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadNcaContent => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadDirectoryContent => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadIconLog => LocalizationManager.Get();
    public static string LoadingErrorNcaFileMissingLog => LocalizationManager.Get();
    public static string LoadingErrorNoCnmtFoundLog => LocalizationManager.Get();
    public static string LoadingErrorNacpFileMissingLog => LocalizationManager.Get();
    public static string LoadingErrorNcaMissingSectionLog => LocalizationManager.Get();
    public static string LoadingErrorMainFileMissingLog => LocalizationManager.Get();
    public static string LoadingErrorIconMissingLog => LocalizationManager.Get();
    public static string LoadingErrorXciSecurePartitionNotFoundLog => LocalizationManager.Get();
    public static string LoadingErrorFailedToGetNcaSectionFsHeader => LocalizationManager.Get();
    public static string LoadingErrorFailedToOpenMainFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadMainFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadTicketFile => LocalizationManager.Get();
    public static string LoadingErrorFailedToLoadTitleIdKey => LocalizationManager.Get();
    public static string LoadingErrorNczBlocklessCompressionDisabled => LocalizationManager.Get();
    public static string LoadingInfoTitleIdKeySuccessfullyInjected => LocalizationManager.Get();
    public static string LoadingWarningTitleIdKeyReplaced => LocalizationManager.Get();
    public static string LoadingDebugTitleIdKeyAlreadyExists => LocalizationManager.Get();
    public static string LogOpeningFile => LocalizationManager.Get();
    public static string KeysFileUsed => LocalizationManager.Get();
    public static string NoneKeysFile => LocalizationManager.Get();
    public static string KeysLoadingStartingLog => LocalizationManager.Get();
    public static string KeysLoadingSuccessfulLog => LocalizationManager.Get();
    public static string KeysLoadingError => LocalizationManager.Get();
    public static string WarnNoProdKeysFileFound => LocalizationManager.Get();
    public static string InvalidSettingKeysFileNotFound => LocalizationManager.Get();
    public static string ToolTipPatchNumber => LocalizationManager.Get();
    public static string CnmtOverviewBuildIdNotAvailableBecauseSectionIsSparse => LocalizationManager.Get();
    public static string NcasIntegrityErrorNcaMissing => LocalizationManager.Get();
    public static string NcasIntegrityErrorLog => LocalizationManager.Get();
    public static string NcaIntegrityGetOriginalNcaError => LocalizationManager.Get();
    public static string NcaIntegrityGetOriginalNcaErrorLog => LocalizationManager.Get();
    public static string NcaHeaderSignatureValidLog => LocalizationManager.Get();
    public static string NcaHeaderSignatureInvalid => LocalizationManager.Get();
    public static string NcaHeaderSignatureInvalidLog => LocalizationManager.Get();
    public static string NcaHeaderSignatureError => LocalizationManager.Get();
    public static string NcaHeaderSignatureErrorLog => LocalizationManager.Get();
    public static string NcaHashVerificationStartLog => LocalizationManager.Get();
    public static string NcaHashVerificationEndLog => LocalizationManager.Get();
    public static string NcaHashNcaItemCantExtractHashFromName => LocalizationManager.Get();
    public static string NcaHashCantExtractHashFromNameLog => LocalizationManager.Get();
    public static string NcaHashValidLog => LocalizationManager.Get();
    public static string NcaHashNcaItemInvalid => LocalizationManager.Get();
    public static string NcaHashInvalidLog => LocalizationManager.Get();
    public static string NcaHashNcaItemException => LocalizationManager.Get();
    public static string NcaHashExceptionLog => LocalizationManager.Get();
    public static string NcaHashProgressText => LocalizationManager.Get();
    public static string NcaSignatureProgressText => LocalizationManager.Get();
    public static string LogNcasIntegrityCanceled => LocalizationManager.Get();
    public static string ToolTipKeyMissing => LocalizationManager.Get();
}

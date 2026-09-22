using System;

namespace Emignatik.NxFileViewer.Localization;

public static class LocalizationManager
{
    public static Func<string, string>? Resolve { get; set; }

    internal static string Get(string key)
        => Resolve?.Invoke(key) ?? key;
}

public static class LoadingLocalizationKeys
{
    public static string SuspiciousFileExtension => LocalizationManager.Get("SuspiciousFileExtension");
    public static string LoadingErrorFailedToCheckIfXciPartitionExists => LocalizationManager.Get("LoadingError_FailedToCheckIfXciPartitionExists");
    public static string LoadingErrorFailedToOpenXciPartition => LocalizationManager.Get("LoadingError_FailedToOpenXciPartition");
    public static string LoadingErrorFailedToLoadXciContent => LocalizationManager.Get("LoadingError_FailedToLoadXciContent");
    public static string LoadingErrorFailedToOpenPartitionFile => LocalizationManager.Get("LoadingError_FailedToOpenPartitionFile");
    public static string LoadingErrorFailedToLoadNcaFile => LocalizationManager.Get("LoadingError_FailedToLoadNcaFile");
    public static string LoadingErrorFailedToLoadPartitionFileSystemContent => LocalizationManager.Get("LoadingError_FailedToLoadPartitionFileSystemContent");
    public static string LoadingErrorFailedToCheckIfSectionCanBeOpened => LocalizationManager.Get("LoadingError_FailedToCheckIfSectionCanBeOpened");
    public static string LoadingErrorFailedToOpenNcaSectionFileSystem => LocalizationManager.Get("LoadingError_FailedToOpenNcaSectionFileSystem");
    public static string LoadingErrorFailedToLoadSectionContent => LocalizationManager.Get("LoadingError_FailedToLoadSectionContent");
    public static string LoadingErrorFailedToGetFileSystemDirectoryEntries => LocalizationManager.Get("LoadingError_FailedToGetFileSystemDirectoryEntries");
    public static string LoadingErrorFailedToOpenNacpFile => LocalizationManager.Get("LoadingError_FailedToOpenNacpFile");
    public static string LoadingErrorFailedToLoadNacpFile => LocalizationManager.Get("LoadingError_FailedToLoadNacpFile");
    public static string LoadingErrorFailedToOpenCnmtFile => LocalizationManager.Get("LoadingError_FailedToOpenCnmtFile");
    public static string LoadingErrorFailedToLoadCnmtFile => LocalizationManager.Get("LoadingError_FailedToLoadCnmtFile");
    public static string LoadingErrorFailedToLoadNcaContent => LocalizationManager.Get("LoadingError_FailedToLoadNcaContent");
    public static string LoadingErrorFailedToLoadDirectoryContent => LocalizationManager.Get("LoadingError_FailedToLoadDirectoryContent");
    public static string LoadingErrorFailedToLoadIconLog => LocalizationManager.Get("LoadingError_FailedToLoadIcon_Log");
    public static string LoadingErrorNcaFileMissingLog => LocalizationManager.Get("LoadingError_NcaFileMissing_Log");
    public static string LoadingErrorNoCnmtFoundLog => LocalizationManager.Get("LoadingError_NoCnmtFound_Log");
    public static string LoadingErrorNacpFileMissingLog => LocalizationManager.Get("LoadingError_NacpFileMissing_Log");
    public static string LoadingErrorNcaMissingSectionLog => LocalizationManager.Get("LoadingError_NcaMissingSection_Log");
    public static string LoadingErrorMainFileMissingLog => LocalizationManager.Get("LoadingError_MainFileMissing_Log");
    public static string LoadingErrorIconMissingLog => LocalizationManager.Get("LoadingError_IconMissing_Log");
    public static string LoadingErrorXciSecurePartitionNotFoundLog => LocalizationManager.Get("LoadingError_XciSecurePartitionNotFound_Log");
    public static string LoadingErrorFailedToGetNcaSectionFsHeader => LocalizationManager.Get("LoadingError_FailedToGetNcaSectionFsHeader");
    public static string LoadingErrorFailedToOpenMainFile => LocalizationManager.Get("LoadingError_FailedToOpenMainFile");
    public static string LoadingErrorFailedToLoadMainFile => LocalizationManager.Get("LoadingError_FailedToLoadMainFile");
    public static string LoadingErrorFailedToLoadTicketFile => LocalizationManager.Get("LoadingError_FailedToLoadTicketFile");
    public static string LoadingErrorFailedToLoadTitleIdKey => LocalizationManager.Get("LoadingError_FailedToLoadTitleIdKey");
    public static string LoadingErrorNczBlocklessCompressionDisabled => LocalizationManager.Get("LoadingError_NczBlocklessCompressionDisabled");
    public static string LoadingInfoTitleIdKeySuccessfullyInjected => LocalizationManager.Get("LoadingInfo_TitleIdKeySuccessfullyInjected");
    public static string LoadingWarningTitleIdKeyReplaced => LocalizationManager.Get("LoadingWarning_TitleIdKeyReplaced");
    public static string LoadingDebugTitleIdKeyAlreadyExists => LocalizationManager.Get("LoadingDebug_TitleIdKeyAlreadyExists");
    public static string LogOpeningFile => LocalizationManager.Get("Log_OpeningFile");
    public static string KeysFileUsed => LocalizationManager.Get("KeysFileUsed");
    public static string NoneKeysFile => LocalizationManager.Get("NoneKeysFile");
    public static string KeysLoadingStartingLog => LocalizationManager.Get("KeysLoading_Starting_Log");
    public static string KeysLoadingSuccessfulLog => LocalizationManager.Get("KeysLoading_Successful_Log");
    public static string KeysLoadingError => LocalizationManager.Get("KeysLoading_Error");
    public static string WarnNoProdKeysFileFound => LocalizationManager.Get("WarnNoProdKeysFileFound");
    public static string InvalidSettingKeysFileNotFound => LocalizationManager.Get("InvalidSetting_KeysFileNotFound");
    public static string ToolTipPatchNumber => LocalizationManager.Get("ToolTip_PatchNumber");
    public static string CnmtOverviewBuildIdNotAvailableBecauseSectionIsSparse => LocalizationManager.Get("CnmtOverview_BuildID_NotAvailableBecauseSectionIsSparse");
    public static string NcasIntegrityErrorNcaMissing => LocalizationManager.Get("NcasIntegrity_Error_NcaMissing");
    public static string NcasIntegrityErrorLog => LocalizationManager.Get("NcasIntegrity_Error_Log");
    public static string NcaIntegrityGetOriginalNcaError => LocalizationManager.Get("NcaIntegrity_GetOriginalNcaError");
    public static string NcaIntegrityGetOriginalNcaErrorLog => LocalizationManager.Get("NcaIntegrity_GetOriginalNcaError_Log");
    public static string NcaHeaderSignatureValidLog => LocalizationManager.Get("NcaHeaderSignature_Valid_Log");
    public static string NcaHeaderSignatureInvalid => LocalizationManager.Get("NcaHeaderSignature_Invalid");
    public static string NcaHeaderSignatureInvalidLog => LocalizationManager.Get("NcaHeaderSignature_Invalid_Log");
    public static string NcaHeaderSignatureError => LocalizationManager.Get("NcaHeaderSignature_Error");
    public static string NcaHeaderSignatureErrorLog => LocalizationManager.Get("NcaHeaderSignature_Error_log");
    public static string NcaHashVerificationStartLog => LocalizationManager.Get("NcaHash_VerificationStart_Log");
    public static string NcaHashVerificationEndLog => LocalizationManager.Get("NcaHash_VerificationEnd_Log");
    public static string NcaHashNcaItemCantExtractHashFromName => LocalizationManager.Get("NcaHash_NcaItem_CantExtractHashFromName");
    public static string NcaHashCantExtractHashFromNameLog => LocalizationManager.Get("NcaHash_CantExtractHashFromName_Log");
    public static string NcaHashValidLog => LocalizationManager.Get("NcaHash_Valid_Log");
    public static string NcaHashNcaItemInvalid => LocalizationManager.Get("NcaHash_NcaItem_Invalid");
    public static string NcaHashInvalidLog => LocalizationManager.Get("NcaHash_Invalid_Log");
    public static string NcaHashNcaItemException => LocalizationManager.Get("NcaHash_NcaItem_Exception");
    public static string NcaHashExceptionLog => LocalizationManager.Get("NcaHash_Exception_Log");
    public static string NcaHashProgressText => LocalizationManager.Get("NcaHash_ProgressText");
    public static string NcaSignatureProgressText => LocalizationManager.Get("NcaSignature_ProgressText");
    public static string LogNcasIntegrityCanceled => LocalizationManager.Get("Log_NcasIntegrityCanceled");
    public static string ToolTipKeyMissing => LocalizationManager.Get("ToolTip_KeyMissing");
}

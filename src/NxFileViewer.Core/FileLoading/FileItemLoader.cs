using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emignatik.NxFileViewer.Localization;
using Emignatik.NxFileViewer.Models.TreeItems;
using Emignatik.NxFileViewer.Models.TreeItems.Impl;
using Emignatik.NxFileViewer.Services.KeysManagement;
using Emignatik.NxFileViewer.Settings;
using Emignatik.NxFileViewer.Utils.LibHacExtensions;
using LibHac;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Loader;
using LibHac.Ns;
using LibHac.NSZ;
using LibHac.Spl;
using LibHac.Tools.Es;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHac.Tools.Ncm;
using Microsoft.Extensions.Logging;
using NcaFsHeader = LibHac.Tools.FsSystem.NcaUtils.NcaFsHeader;

namespace Emignatik.NxFileViewer.FileLoading;

public class FileItemLoader : IFileItemLoader
{
    private readonly IKeySetProviderService _keySetProviderService;
    private readonly IFileLoadingSettings _appSettings;
    private readonly ILogger _logger;

    public FileItemLoader(IKeySetProviderService keySetProviderService, ILoggerFactory loggerFactory, IFileLoadingSettings appSettings)
    {
        _keySetProviderService = keySetProviderService ?? throw new ArgumentNullException(nameof(keySetProviderService));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger(this.GetType());
    }

    public event MissingKeyExceptionHandler? MissingKey;

    public NspItem LoadNsp(string nspFilePath)
    {
        var keySet = _keySetProviderService.GetKeySet(_appSettings.AlwaysReloadKeysBeforeOpen);
        var nspItem = NspItem.FromFile(nspFilePath, keySet);
        BuildPartitionChildItems(nspItem);
        return nspItem;
    }

    public XciItem LoadXci(string xciFilePath)
    {
        var keySet = _keySetProviderService.GetKeySet(_appSettings.AlwaysReloadKeysBeforeOpen);
        var xciItem = XciItem.FromFile(xciFilePath, keySet);
        BuildXciChildItems(xciItem);
        return xciItem;
    }

    private void BuildXciChildItems(XciItem parentItem)
    {
        var xci = parentItem.Xci;

        try {
            foreach (var xciPartitionType in Enum.GetValues<XciPartitionType>()) {
                try {
                    if (!xci.HasPartition(xciPartitionType)) {
                        continue;
                    }
                }
                catch (Exception ex) {
                    OnLoadingException(ex);
                    var message = LoadingLocalizationKeys.LoadingErrorFailedToCheckIfXciPartitionExists.SafeFormat(ex.Message);
                    parentItem.Errors.Add(Category.Loading, message);
                    _logger.LogError(ex, message);
                    continue;
                }

                XciPartition xciPartition;
                try {
                    xciPartition = xci.OpenPartition(xciPartitionType);
                }
                catch (Exception ex) {
                    OnLoadingException(ex);
                    var message = LoadingLocalizationKeys.LoadingErrorFailedToOpenXciPartition.SafeFormat(ex.Message);
                    parentItem.Errors.Add(Category.Loading, message);
                    _logger.LogError(ex, message);
                    continue;
                }

                var xciPartitionItem = new XciPartitionItem(xciPartition, xciPartitionType, parentItem);
                BuildPartitionChildItems(xciPartitionItem);
            }
        }
        catch (Exception ex) {
            OnLoadingException(ex);
            var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadXciContent.SafeFormat(ex.Message);
            parentItem.Errors.Add(Category.Loading, message);
            _logger.LogError(ex, message);
        }

    }

    private void BuildPartitionChildItems(PartitionFileSystemItemBase parentItem)
    {
        try {
            var partitionFileSystem = parentItem.PartitionFileSystem;

            var remainingEntries = new List<DirectoryEntryEx>();

            // First loop on *.tik files to inject title keys in KeySet
            foreach (var partitionFileEntry in partitionFileSystem.EnumerateEntries().Where(e => e.Type == DirectoryEntryType.File)) {
                var fileName = partitionFileEntry.Name;
                if (!fileName.EndsWith(".tik", StringComparison.OrdinalIgnoreCase)) {
                    remainingEntries.Add(partitionFileEntry);
                    continue;
                }

                IFile file;
                try {
                    file = partitionFileSystem.LoadFile(partitionFileEntry);
                }
                catch (Exception ex) {
                    OnLoadingException(ex);
                    var partitionFileEntryItem = new PartitionFileEntryItem(partitionFileEntry, parentItem);
                    var message = LoadingLocalizationKeys.LoadingErrorFailedToOpenPartitionFile.SafeFormat(ex.Message);
                    partitionFileEntryItem.Errors.Add(Category.Loading, message);
                    _logger.LogError(ex, message);
                    continue;
                }

                Ticket ticket;
                try {
                    // According to https://github.com/Thealexbarney/LibHac/pull/304, in order to avoid reading issues with Sha256PartitionFileSystem,
                    // we need to copy the whole ticket file to a MemoryStream before reading it (same related issue: https://github.com/Myster-Tee/NxFileViewer/issues/32)
                    using var ms = new MemoryStream();
                    file.AsStream().CopyTo(ms);
                    ms.Position = 0;

                    ticket = new Ticket(ms);
                }
                catch (Exception ex) {
                    OnLoadingException(ex);
                    var partitionFileEntryItem = new PartitionFileEntryItem(partitionFileEntry, parentItem);
                    var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadTicketFile.SafeFormat(ex.Message);
                    partitionFileEntryItem.Errors.Add(Category.Loading, message);
                    _logger.LogError(ex, message);
                    continue;
                }

                var ticketItem = new TicketItem(ticket, partitionFileEntry, parentItem);

                if (_appSettings.InjectTicketKeys) {
                    try {

                        var rightsId = new RightsId(ticket.RightsId);
                        var accessKey = new AccessKey(ticket.TitleKeyBlock);

                        ticketItem.RightsId = rightsId;
                        ticketItem.AccessKey = accessKey;

                        if (parentItem.KeySet.ExternalKeySet.Get(rightsId, out var existingAccessKey) == Result.Success) {
                            // Here RightID key is already defined
                            if (existingAccessKey != accessKey) {
                                // Replaces the RightID key with the one defined in the ticket
                                parentItem.KeySet.ExternalKeySet.Remove(rightsId);
                                parentItem.KeySet.ExternalKeySet.Add(rightsId, accessKey).ThrowIfFailure();
                                _logger.LogWarning(LoadingLocalizationKeys.LoadingWarningTitleIdKeyReplaced.SafeFormat(rightsId.ToString(), accessKey.ToString(), fileName, existingAccessKey));
                            }
                            else {
                                _logger.LogDebug(LoadingLocalizationKeys.LoadingDebugTitleIdKeyAlreadyExists.SafeFormat(rightsId.ToString(), accessKey.ToString(), fileName));
                            }
                        }
                        else {
                            parentItem.KeySet.ExternalKeySet.Add(rightsId, accessKey).ThrowIfFailure();
                            _logger.LogInformation(LoadingLocalizationKeys.LoadingInfoTitleIdKeySuccessfullyInjected.SafeFormat(rightsId.ToString(), accessKey.ToString(), fileName));
                        }
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, LoadingLocalizationKeys.LoadingErrorFailedToLoadTitleIdKey.SafeFormat(fileName, ex.Message));
                    }
                }
            }

            foreach (var partitionFileEntry in remainingEntries) {
                IFile partitionFile;
                try {
                    partitionFile = partitionFileSystem.LoadFile(partitionFileEntry);
                }
                catch (Exception ex) {
                    OnLoadingException(ex);
                    var partitionFileEntryItem = new PartitionFileEntryItem(partitionFileEntry, parentItem);
                    var message = LoadingLocalizationKeys.LoadingErrorFailedToOpenPartitionFile.SafeFormat(ex.Message);
                    partitionFileEntryItem.Errors.Add(Category.Loading, message);
                    _logger.LogError(ex, message);
                    continue;
                }

                var fileName = partitionFileEntry.Name;
                if (fileName.EndsWith(".ncz", StringComparison.OrdinalIgnoreCase)) {
                    Ncz ncz;
                    try {
                        ncz = new Ncz(parentItem.KeySet, partitionFile.AsStream(), NczReadMode.Fast);
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var partitionFileEntryItem = new PartitionFileEntryItem(partitionFileEntry, parentItem);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadNcaFile.SafeFormat(ex.Message);
                        partitionFileEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }
                    var nczItem = new NczItem(ncz, partitionFileEntry, parentItem);
                    if (ncz.NczHeader.IsUsingBlockCompression || _appSettings.OpenBlocklessCompressionNcz) {
                        BuildNcaChildItems(nczItem);
                    }
                    else {
                        nczItem.Errors.Add(Category.Loading, LoadingLocalizationKeys.LoadingErrorNczBlocklessCompressionDisabled);
                    }
                }
                else if (fileName.EndsWith(".nca", StringComparison.OrdinalIgnoreCase)) {
                    Nca nca;
                    try {
                        var fileStorage = new FileStorage(partitionFile);
                        nca = new Nca(parentItem.KeySet, fileStorage);
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var partitionFileEntryItem = new PartitionFileEntryItem(partitionFileEntry, parentItem);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadNcaFile.SafeFormat(ex.Message);
                        partitionFileEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }

                    var ncaItem = new NcaItem(nca, partitionFileEntry, parentItem);
                    BuildNcaChildItems(ncaItem);
                }
                else {
                    _ = new PartitionFileEntryItem(partitionFileEntry, parentItem);
                }
            }
        }
        catch (Exception ex) {
            OnLoadingException(ex);

            var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadPartitionFileSystemContent.SafeFormat(ex.Message);
            parentItem.Errors.Add(Category.Loading, message);
            _logger.LogError(ex, message);
        }
    }

    private void BuildNcaChildItems(NcaItem parentItem)
    {
        try {
            var nca = parentItem.Nca;
            for (var sectionIndex = 0; sectionIndex < NcaItem.MaxSections; sectionIndex++) {
                try {
                    if (!nca.Header.IsSectionEnabled(sectionIndex)) {
                        continue;
                    }
                }
                catch (Exception ex) {
                    OnLoadingException(ex);

                    var message = LoadingLocalizationKeys.LoadingErrorFailedToCheckIfSectionCanBeOpened.SafeFormat(ex.Message);
                    parentItem.Errors.Add(Category.Loading, message);
                    _logger.LogError(ex, message);
                    continue;
                }

                NcaFsHeader ncaFsHeader;
                try {
                    ncaFsHeader = nca.GetFsHeader(sectionIndex);
                }
                catch (Exception ex) {
                    OnLoadingException(ex);

                    var message = LoadingLocalizationKeys.LoadingErrorFailedToGetNcaSectionFsHeader.SafeFormat(sectionIndex, ex.Message);
                    parentItem.Errors.Add(Category.Loading, message);
                    _logger.LogError(ex, message);
                    continue;
                }


                var existsSparseLayer = ncaFsHeader.ExistsSparseLayer();
                var ncaSparseInfo = existsSparseLayer ? ncaFsHeader.GetSparseInfo() : (NcaSparseInfo?)null;

                var isPatchSection = ncaFsHeader.IsPatchSection();
                var ncaFsPatchInfo = isPatchSection ? ncaFsHeader.GetPatchInfo() : (NcaFsPatchInfo?)null;

                var sectionItem = new SectionItem(sectionIndex, ncaFsHeader, parentItem, ncaFsPatchInfo, ncaSparseInfo);
                if (!existsSparseLayer && !isPatchSection) {
                    IFileSystem? fileSystem = null;
                    try {
                        fileSystem = nca.OpenFileSystem(sectionIndex, IntegrityCheckLevel.ErrorOnInvalid);
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);

                        var message = LoadingLocalizationKeys.LoadingErrorFailedToOpenNcaSectionFileSystem.SafeFormat(sectionIndex, ex.Message);
                        sectionItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                    }

                    sectionItem.FileSystem = fileSystem;

                    BuildSectionChildItems(sectionItem);
                }
            }
        }
        catch (Exception ex) {
            OnLoadingException(ex);

            var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadNcaContent.SafeFormat(ex.Message);
            parentItem.Errors.Add(Category.Loading, message);
            _logger.LogError(ex, message);
        }
    }

    private void BuildSectionChildItems(SectionItem parentItem)
    {
        try {
            const string? ROOT_PATH = "/";

            var fileSystem = parentItem.FileSystem;
            if (fileSystem == null) {
                return;
            }

            var directoryEntries = SafeGetChildDirectoryEntries(fileSystem, ROOT_PATH, parentItem).ToArray();

            foreach (var directoryEntry in directoryEntries) {
                var entryName = directoryEntry.Name;
                var entryPath = directoryEntry.FullPath;

                // NACP File
                if (parentItem.ParentItem.ContentType == NcaContentType.Control && string.Equals(entryName, NacpItem.NacpFileName, StringComparison.OrdinalIgnoreCase) && directoryEntry.Type == DirectoryEntryType.File) {
                    IFile nacpFile;
                    try {
                        using var uniqueRefFile = new UniqueRef<IFile>();
                        fileSystem.OpenFile(ref uniqueRefFile.Ref, entryPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                        nacpFile = uniqueRefFile.Release();
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var directoryEntryItem = new DirectoryEntryItem(parentItem, directoryEntry);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToOpenNacpFile.SafeFormat(ex.Message);
                        directoryEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }

                    ApplicationControlProperty nacp;
                    try {
                        var blitStruct = new BlitStruct<ApplicationControlProperty>(1);
                        nacpFile.Read(out _, 0, blitStruct.ByteSpan).ThrowIfFailure();
                        nacp = blitStruct.Value;
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var directoryEntryItem = new DirectoryEntryItem(parentItem, directoryEntry);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadNacpFile.SafeFormat(ex.Message);
                        directoryEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }

                    _ = new NacpItem(nacp, parentItem, directoryEntry);
                }
                // CNMT File
                else if (parentItem.ParentItem.ContentType == NcaContentType.Meta && entryName.EndsWith(".cnmt", StringComparison.OrdinalIgnoreCase) && directoryEntry.Type == DirectoryEntryType.File) {
                    IFile cnmtFile;
                    try {
                        using var uniqueRefFile = new UniqueRef<IFile>();
                        fileSystem.OpenFile(ref uniqueRefFile.Ref, entryPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                        cnmtFile = uniqueRefFile.Release();
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var directoryEntryItem = new DirectoryEntryItem(parentItem, directoryEntry);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToOpenCnmtFile.SafeFormat(ex.Message);
                        directoryEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }

                    Cnmt cnmt;
                    try {
                        cnmt = new Cnmt(cnmtFile.AsStream());
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var directoryEntryItem = new DirectoryEntryItem(parentItem, directoryEntry);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadCnmtFile.SafeFormat(ex.Message);
                        directoryEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }

                    var cnmtItem = new CnmtItem(cnmt, parentItem, directoryEntry);

                    for (var index = 0; index < cnmt.ContentEntries.Length; index++) {
                        var cnmtContentEntry = cnmt.ContentEntries[index];
                        _ = new CnmtContentEntryItem(cnmtContentEntry, cnmtItem, index);
                    }
                }
                // MAIN file
                else if (parentItem.ParentItem.ContentType == NcaContentType.Program && string.Equals(entryName, MainItem.MainFileName, StringComparison.OrdinalIgnoreCase) && directoryEntry.Type == DirectoryEntryType.File) {
                    IFile nsoFile;
                    try {
                        using var uniqueRefFile = new UniqueRef<IFile>();
                        fileSystem.OpenFile(ref uniqueRefFile.Ref, entryPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                        nsoFile = uniqueRefFile.Release();
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var directoryEntryItem = new DirectoryEntryItem(parentItem, directoryEntry);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToOpenMainFile.SafeFormat(ex.Message);
                        directoryEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }

                    NsoHeader? nsoHeader;
                    try {
                        var nsoReader = new NsoReader();
                        nsoReader.Initialize(nsoFile).ThrowIfFailure();
                        nsoHeader = nsoReader.Header;
                    }
                    catch (Exception ex) {
                        OnLoadingException(ex);
                        var directoryEntryItem = new DirectoryEntryItem(parentItem, directoryEntry);
                        var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadMainFile.SafeFormat(ex.Message);
                        directoryEntryItem.Errors.Add(Category.Loading, message);
                        _logger.LogError(ex, message);
                        continue;
                    }

                    _ = new MainItem(nsoHeader.Value, parentItem, directoryEntry);
                }
                else {
                    var directoryEntryItem = new DirectoryEntryItem(parentItem, directoryEntry);
                    BuildDirectoryEntryChildItems(directoryEntryItem);
                }
            }
        }
        catch (Exception ex) {
            OnLoadingException(ex);

            var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadSectionContent.SafeFormat(ex.Message);
            parentItem.Errors.Add(Category.Loading, message);
            _logger.LogError(ex, message);
        }
    }

    private void BuildDirectoryEntryChildItems(DirectoryEntryItem parentItem)
    {
        try {
            if (parentItem.DirectoryEntryType == DirectoryEntryType.File) {
                return;
            }

            var currentPath = parentItem.Path;
            var directoryEntries = SafeGetChildDirectoryEntries(parentItem.ContainerSectionItem.FileSystem!, currentPath, parentItem);

            foreach (var directoryEntry in directoryEntries) {
                var directoryEntryItem = new DirectoryEntryItem(parentItem.ContainerSectionItem, directoryEntry, parentItem);
                BuildDirectoryEntryChildItems(directoryEntryItem);
            }
        }
        catch (Exception ex) {
            OnLoadingException(ex);

            var message = LoadingLocalizationKeys.LoadingErrorFailedToLoadDirectoryContent.SafeFormat(ex.Message);
            parentItem.Errors.Add(Category.Loading, message);
            _logger.LogError(ex, message);
        }
    }

    private IEnumerable<DirectoryEntryEx> SafeGetChildDirectoryEntries(IFileSystem fileSystem, string currentPath, IItem parentItem)
    {
        try {
            return fileSystem.EnumerateEntries(currentPath, "*", SearchOptions.Default);
        }
        catch (Exception ex) {
            OnLoadingException(ex);

            var message = LoadingLocalizationKeys.LoadingErrorFailedToGetFileSystemDirectoryEntries.SafeFormat(ex.Message);
            parentItem.Errors.Add(Category.Loading, message);
            _logger.LogError(ex, message);
            return Array.Empty<DirectoryEntryEx>();
        }
    }

    private void OnLoadingException(Exception ex)
    {
        if (ex is MissingKeyException missingKeyException) {
            NotifyMissingKey(missingKeyException);
        }
    }


    private void NotifyMissingKey(MissingKeyException ex)
    {
        MissingKey?.Invoke(this, new MissingKeyExceptionHandlerArgs(ex));
    }
}

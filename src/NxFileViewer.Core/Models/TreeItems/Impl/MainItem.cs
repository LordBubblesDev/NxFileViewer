using System;
using Emignatik.NxFileViewer.Utils;
using LibHac.Loader;
using LibHac.Tools.Fs;

namespace Emignatik.NxFileViewer.Models.TreeItems.Impl;

public class MainItem(NsoHeader nsoHeader, SectionItem parentItem, DirectoryEntryEx directoryEntry)
    : DirectoryEntryItem(parentItem, directoryEntry)
{
    public const string MainFileName = "main";

    private NsoHeader NsoHeader { get; } = nsoHeader;

    public sealed override string Format => "Nso";

    public string ModuleId => FormatModuleId(NsoHeader.ModuleId);

    private static string FormatModuleId(LibHac.Common.FixedArrays.Array32<byte> moduleId)
    {
        var bytes = new byte[32];

        for (var i = 0; i < bytes.Length; i++) {
            bytes[i] = moduleId[i];
        }

        var length = bytes.Length;
        
        while (length > 0 && bytes[length - 1] == 0) {
            length--;
        }

        return length == 0 ? string.Empty : bytes.AsSpan(0, length).ToStrId();
    }
}
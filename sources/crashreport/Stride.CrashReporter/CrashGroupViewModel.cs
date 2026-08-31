// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.CrashReport;

namespace Stride.CrashReporter;

/// <summary>One deduped crash in the run: what it was, how often it happened, and the two choices the user
/// makes about it (send it, and whether to be asked again).</summary>
internal sealed class CrashGroupViewModel : ObservableObject
{
    private bool send = true;
    private bool dontShowAgain;
    private bool includeDump = true;
    private bool includeAssetDefinition; // default off: the definition is the user's project content

    public CrashGroupViewModel(StoredCrash crash, long dumpSize)
    {
        Crash = crash;
        Title = crash.Title();
        Detail = ComputeDetail(crash);
        ReportText = crash.ToReportData().ToString();
        HasDump = dumpSize > 0;
        DumpLabel = HasDump ? $"Include crash dump — call stacks and module list only, no memory ({FormatSize(dumpSize)})" : null;

        var definition = crash.AssetDefinitionPath;
        if (!string.IsNullOrEmpty(definition) && File.Exists(definition)
            && new FileInfo(definition).Length is var size && size <= CrashSession.MaxAssetAttachmentBytes)
        {
            HasAssetDefinition = true;
            AssetDefinitionLabel = $"Attach the asset definition — {Path.GetFileName(definition)} ({FormatSize(size)})";
        }
    }

    public StoredCrash Crash { get; }

    /// <summary>The exception's first line, else the signature.</summary>
    public string Title { get; }

    /// <summary>Occurrence count and affected assets, e.g. "3 times · Hero (Model), Tree (Model)".</summary>
    public string Detail { get; }

    /// <summary>The full report text, shown when the user expands the report.</summary>
    public string ReportText { get; }

    /// <summary>Send this group. Defaults on; unchecking dismisses it instead.</summary>
    public bool Send
    {
        get => send;
        set => SetProperty(ref send, value);
    }

    /// <summary>Suppress this signature's future popups even without sending it.</summary>
    public bool DontShowAgain
    {
        get => dontShowAgain;
        set => SetProperty(ref dontShowAgain, value);
    }

    /// <summary>Whether this crash has a memory dump on disk (native crashes do; managed ones don't).</summary>
    public bool HasDump { get; }

    /// <summary>Checkbox label describing the dump and its size; null when there is no dump.</summary>
    public string? DumpLabel { get; }

    /// <summary>Attach the dump when sending. Default on: for a native crash it is the main diagnostic.</summary>
    public bool IncludeDump
    {
        get => includeDump;
        set => SetProperty(ref includeDump, value);
    }

    /// <summary>Whether the failing asset's definition file exists and is small enough to attach.</summary>
    public bool HasAssetDefinition { get; }

    /// <summary>Checkbox label naming the definition file and its size; null when there is none.</summary>
    public string? AssetDefinitionLabel { get; }

    /// <summary>Attach the asset definition when sending. Default off: it is the user's project content.</summary>
    public bool IncludeAssetDefinition
    {
        get => includeAssetDefinition;
        set => SetProperty(ref includeAssetDefinition, value);
    }

    private static string ComputeDetail(StoredCrash crash)
    {
        var count = crash.Count == 1 ? "1 time" : $"{crash.Count} times";
        return crash.AffectedAssets.Count > 0
            ? $"{count} · {string.Join(", ", crash.AffectedAssets)}"
            : count;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):0.#} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes} B";
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace Stride.CrashReport;

/// <summary>
/// A crash captured to disk by a headless tool (the asset compiler, the CLI), for a separate reporter
/// process or <c>stride crash send</c> to submit later. The sender runs in a different process than the
/// one that crashed, so everything it needs — app identity, version, environment, tags — travels in the
/// file rather than being read from the reporter's own assembly.
/// </summary>
public sealed class StoredCrash
{
    /// <summary>On-disk format version, so an older reporter can reject a newer file rather than misread it.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Application id, e.g. "AssetCompiler". Becomes the Sentry application tag and release package.</summary>
    public string Application { get; set; }

    /// <summary>The crashing app's informational version (the release string).</summary>
    public string Version { get; set; }

    /// <summary>Sentry environment (release / local / ci).</summary>
    public string Environment { get; set; }

    /// <summary>When the crash was first captured, round-trip-safe UTC ("o" format).</summary>
    public string TimestampUtc { get; set; }

    /// <summary>Dedup key (exception type + top frame + step kind). Identical crashes share it.</summary>
    public string Signature { get; set; }

    /// <summary>How many times this signature occurred; bumped on write when a repeat is stored.</summary>
    public int Count { get; set; } = 1;

    /// <summary>The report content (the <see cref="CrashReportData"/> key/value pairs), already anonymized.</summary>
    public List<CrashEntry> Report { get; set; } = new();

    /// <summary>Sibling minidump file name in the same run directory, if one was written.</summary>
    public string DumpFileName { get; set; }

    /// <summary>Scrubbed leaf names of the assets that hit this signature, for the "x N assets" display.</summary>
    public List<string> AffectedAssets { get; set; } = new();

    /// <summary>Local-only path of the failing asset's definition file, offered as an opt-in attachment.</summary>
    public string AssetDefinitionPath { get; set; }

    /// <summary>Local-only paths of the failing asset's direct source files (FBX, textures), for a future opt-in attachment.</summary>
    public List<string> AssetSourcePaths { get; set; } = new();

    /// <summary>Structured exception chain for a managed crash, so the reporter rebuilds a real Sentry stacktrace. Empty for native.</summary>
    public List<StoredException> Exceptions { get; set; } = new();

    /// <summary>Managed id of the crashing thread, when known; lets the reporter flag it in the thread list.</summary>
    public int? CrashedThreadId { get; set; }

    /// <summary>Name of the crashing thread, when known (often empty — threads are frequently unnamed).</summary>
    public string CrashedThreadName { get; set; }

    /// <summary>Other (non-crashing) threads' callstacks captured at a fatal crash. Empty otherwise.</summary>
    public List<StoredThread> Threads { get; set; } = new();

    /// <summary>One-line label: the exception's first line, else the signature.</summary>
    public string Title()
    {
        var exception = Report.FirstOrDefault(entry => entry.Key == "Exception")?.Value;
        if (!string.IsNullOrWhiteSpace(exception))
            return exception.Split('\n', 2)[0].Trim();
        return string.IsNullOrEmpty(Signature) ? "Unknown crash" : Signature;
    }

    /// <summary>Rebuilds the report content as a <see cref="CrashReportData"/> for the sender.</summary>
    public CrashReportData ToReportData()
    {
        var data = new CrashReportData();
        foreach (var entry in Report)
            data[entry.Key] = entry.Value;
        return data;
    }

    /// <summary>Captures a report's content into a new <see cref="StoredCrash"/> (metadata set by the caller).</summary>
    public static StoredCrash FromReportData(CrashReportData data)
    {
        var crash = new StoredCrash();
        foreach (var (key, value) in data.Data)
            crash.Report.Add(new CrashEntry { Key = key, Value = value });
        return crash;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static StoredCrash FromJson(string json) => JsonSerializer.Deserialize<StoredCrash>(json, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

/// <summary>One report key/value pair. Kept as an explicit object (not a dictionary) so insertion order survives the round-trip.</summary>
public sealed class CrashEntry
{
    public string Key { get; set; }
    public string Value { get; set; }
}

/// <summary>One exception in a captured chain: its type, message, and frames (newest-first, as .NET reports them).</summary>
public sealed class StoredException
{
    public string Type { get; set; }
    public string Message { get; set; }
    public List<StoredFrame> Frames { get; set; } = new();

    /// <summary>Captures a live exception + inner chain (with file/line from PDBs) into a serializable model at the crash site.</summary>
    public static List<StoredException> Capture(Exception exception)
    {
        var list = new List<StoredException>();
        for (var current = exception; current != null && list.Count < 10; current = current.InnerException)
        {
            var stored = new StoredException { Type = current.GetType().FullName, Message = current.Message };
            foreach (var frame in new StackTrace(current, fNeedFileInfo: true).GetFrames() ?? Array.Empty<StackFrame>())
            {
                var method = frame.GetMethod();
                stored.Frames.Add(new StoredFrame
                {
                    Function = method is null ? null
                        : method.DeclaringType is null ? method.Name : method.DeclaringType.FullName + "." + method.Name,
                    Module = method?.DeclaringType?.Assembly.GetName().Name,
                    File = frame.GetFileName(),
                    Line = frame.GetFileLineNumber(),
                });
            }
            list.Add(stored);
        }
        return list;
    }
}

/// <summary>One stack frame: fully-qualified function, declaring assembly, and file/line when the PDB had them.</summary>
public sealed class StoredFrame
{
    public string Function { get; set; }
    public string Module { get; set; }
    public string File { get; set; }
    public int Line { get; set; }
}

/// <summary>One non-crashing thread in a crash-time snapshot: its managed id/name and its callstack frames.</summary>
public sealed class StoredThread
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<StoredFrame> Frames { get; set; } = new();
}

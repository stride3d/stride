// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
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

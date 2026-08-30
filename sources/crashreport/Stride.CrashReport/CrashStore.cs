// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Stride.CrashReport;

/// <summary>
/// Reads and writes crash files under a per-app directory. Headless tools (the asset compiler, the CLI)
/// write here on a crash and a reporter or <c>stride crash send</c> reads here to submit. In-process
/// reporters (GameStudio, launcher) send directly and do not use this.
/// </summary>
/// <remarks>
/// Layout: <c>&lt;base&gt;/&lt;app&gt;/run-&lt;UTC&gt;-&lt;pid&gt;/crash-&lt;sig&gt;.json</c> (+ sibling
/// <c>.dmp</c>), one set per distinct signature. The base is <c>STRIDE_CRASH_DIR</c> or, by default,
/// <c>&lt;LocalApplicationData&gt;/stride/crash-reports</c> (matching Stride's other per-user dirs).
/// </remarks>
public sealed class CrashStore
{
    private const string EnvBaseDir = "STRIDE_CRASH_DIR";

    /// <summary>The resolved base directory holding every app's crash reports.</summary>
    public string BaseDirectory { get; }

    /// <summary>This app's directory under <see cref="BaseDirectory"/>.</summary>
    public string AppDirectory { get; }

    public CrashStore(string application, string baseDirectory = null)
    {
        BaseDirectory = ResolveBaseDirectory(baseDirectory);
        AppDirectory = Path.Combine(BaseDirectory, application.ToLowerInvariant());
    }

    /// <summary>Resolves the base directory: explicit override, else <c>STRIDE_CRASH_DIR</c>, else the per-user default.</summary>
    public static string ResolveBaseDirectory(string baseDirectory = null)
        => baseDirectory
            ?? Environment.GetEnvironmentVariable(EnvBaseDir)
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "crash-reports");

    /// <summary>The app ids (subdir names) that currently have a store, for a tool that lists every pending crash.</summary>
    public static IReadOnlyList<string> EnumerateApps(string baseDirectory = null)
    {
        var baseDir = ResolveBaseDirectory(baseDirectory);
        if (!Directory.Exists(baseDir))
            return Array.Empty<string>();
        return Directory.EnumerateDirectories(baseDir)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Creates a fresh per-run directory (<c>run-&lt;UTC&gt;-&lt;pid&gt;</c>) for this invocation's crashes.</summary>
    public CrashRun CreateRun()
    {
        var name = $"run-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-{Environment.ProcessId}";
        var dir = Path.Combine(AppDirectory, name);
        Directory.CreateDirectory(dir);
        return new CrashRun(dir);
    }

    /// <summary>All pending runs (unsent), newest first.</summary>
    public IReadOnlyList<CrashRun> ListRuns()
    {
        if (!Directory.Exists(AppDirectory))
            return Array.Empty<CrashRun>();
        return Directory.EnumerateDirectories(AppDirectory, "run-*")
            .OrderByDescending(d => d, StringComparer.Ordinal)
            .Select(d => new CrashRun(d))
            .ToList();
    }

    /// <summary>Opens a run by its directory path (for <c>stride crash send &lt;dir&gt;</c>).</summary>
    public static CrashRun OpenRun(string directory) => new(directory);

    /// <summary>Deletes runs older than <paramref name="maxAge"/> so the store never grows unbounded.</summary>
    public void Prune(TimeSpan maxAge)
    {
        if (!Directory.Exists(AppDirectory))
            return;
        var cutoff = DateTime.UtcNow - maxAge;
        foreach (var dir in Directory.EnumerateDirectories(AppDirectory, "run-*"))
        {
            try
            {
                if (Directory.GetLastWriteTimeUtc(dir) < cutoff)
                    Directory.Delete(dir, recursive: true);
            }
            catch (Exception)
            {
                // Best effort: a locked or vanished run is skipped, retried next prune.
            }
        }
    }

    // Suppression: signatures the user chose not to be asked about again, keyed by signature + app
    // version so an upgrade re-surfaces a still-present crash once. Persisted at the app dir, so it
    // outlives individual runs.

    private string SuppressedPath => Path.Combine(AppDirectory, "suppressed.json");

    public bool IsSuppressed(string signature, string version)
        => LoadSuppressed().Contains(SuppressKey(signature, version));

    public void Suppress(string signature, string version)
    {
        var set = LoadSuppressed();
        if (!set.Add(SuppressKey(signature, version)))
            return;
        Directory.CreateDirectory(AppDirectory);
        File.WriteAllText(SuppressedPath, JsonSerializer.Serialize(set));
    }

    private HashSet<string> LoadSuppressed()
    {
        try
        {
            if (File.Exists(SuppressedPath))
                return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(SuppressedPath)) ?? new();
        }
        catch (Exception)
        {
            // A corrupt suppression file just means nothing is suppressed; it is rewritten on the next Suppress.
        }
        return new HashSet<string>();
    }

    private static string SuppressKey(string signature, string version) => $"{version}\n{signature}";
}

/// <summary>One build/invocation's crashes: the files under a single <c>run-*</c> directory.</summary>
public sealed class CrashRun
{
    /// <summary>The run directory holding this run's crash files.</summary>
    public string Directory { get; }

    internal CrashRun(string directory) => Directory = directory;

    /// <summary>
    /// Stores a crash, deduping by signature: if the signature is already present, bumps its count and
    /// merges affected assets rather than writing a second file. The dump, if any, is kept only for the
    /// first occurrence of a signature.
    /// </summary>
    public void Add(StoredCrash crash, byte[] dump = null)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var stem = "crash-" + Hash(crash.Signature ?? string.Empty);
        var jsonPath = Path.Combine(Directory, stem + ".json");

        if (File.Exists(jsonPath))
        {
            var existing = StoredCrash.FromJson(File.ReadAllText(jsonPath));
            existing.Count += crash.Count;
            foreach (var asset in crash.AffectedAssets)
                if (!existing.AffectedAssets.Contains(asset))
                    existing.AffectedAssets.Add(asset);
            File.WriteAllText(jsonPath, existing.ToJson());
            return; // keep the first occurrence's dump and metadata
        }

        if (dump != null)
        {
            var dumpName = stem + ".dmp";
            File.WriteAllBytes(Path.Combine(Directory, dumpName), dump);
            crash.DumpFileName = dumpName;
        }
        File.WriteAllText(jsonPath, crash.ToJson());
    }

    /// <summary>All crashes stored in this run.</summary>
    public IReadOnlyList<StoredCrash> Read()
    {
        if (!System.IO.Directory.Exists(Directory))
            return Array.Empty<StoredCrash>();
        var list = new List<StoredCrash>();
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "crash-*.json"))
        {
            try
            {
                list.Add(StoredCrash.FromJson(File.ReadAllText(file)));
            }
            catch (Exception)
            {
                // A truncated or unreadable crash file is skipped rather than blocking the rest.
            }
        }
        return list;
    }

    /// <summary>Reads the dump bytes for a stored crash, if it has one.</summary>
    public byte[] ReadDump(StoredCrash crash)
    {
        if (string.IsNullOrEmpty(crash.DumpFileName))
            return null;
        var path = Path.Combine(Directory, crash.DumpFileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    /// <summary>
    /// Deletes a single group's files (its json and dump), leaving the rest of the run. Used when a reporter
    /// sends some groups and keeps others (e.g. a failed send held back for a later <c>stride crash send</c>).
    /// </summary>
    public void Remove(StoredCrash crash)
    {
        var stem = "crash-" + Hash(crash.Signature ?? string.Empty);
        TryDeleteFile(Path.Combine(Directory, stem + ".json"));
        if (!string.IsNullOrEmpty(crash.DumpFileName))
            TryDeleteFile(Path.Combine(Directory, crash.DumpFileName));
    }

    /// <summary>True when the run holds no crash files (e.g. after everything was pruned or removed).</summary>
    public bool IsEmpty => Read().Count == 0;

    /// <summary>Removes the whole run directory once it has been sent or dismissed.</summary>
    public void Delete()
    {
        try
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (Exception)
        {
            // Best effort: a locked run is left for the age-based prune.
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception)
        {
            // Best effort: a locked file is left for the age-based prune.
        }
    }

    // A signature is arbitrary text (type + frame + step); hash it to a short, filesystem-safe stem.
    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var sb = new StringBuilder(16);
        for (var i = 0; i < 8; i++)
            sb.Append(bytes[i].ToString("x2"));
        return sb.ToString();
    }
}

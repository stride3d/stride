// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets;

/// <summary>
/// Copy-on-write snapshot of the files an in-place upgrade is about to overwrite. Armed by
/// <see cref="PackageLoadParameters.BackupBeforeUpgrade"/> and called from each upgrade write point right
/// before it overwrites a file. The timestamped backup folder is created lazily on the first real snapshot
/// (so nothing is written when an upgrade modifies no files), and each original is copied at most once
/// (a file may be touched by several upgraders). <see cref="Restore"/> puts the files back as they were.
/// The folder lists the files it backs up, with the state the upgrade left them in (see <see cref="RecordState"/>), so that
/// <see cref="Open"/> gets the backup back in a later session.
/// </summary>
public sealed class UpgradeBackup
{
    /// <summary>The start of the name of a backup folder, which the time of the upgrade ends.</summary>
    public const string FolderNamePrefix = ".stride-backup-";

    // The folder of the backup that holds the files outside the root, by drive: C:\Shared\Lib.cs is outside\C\Shared\Lib.cs
    private const string OutsideFolderName = "outside";

    // The list of the files of the backup, in its folder: one JSON entry per line, added as the upgrade goes
    private const string ManifestFileName = ".stride-backup-files.jsonl";

    // The start of the name of the folder of the backup that keeps the files as they were before they were put back
    private const string RevertedFolderPrefix = "reverted-";

    private readonly string rootDirectory;
    private readonly string backupFolderName;
    private readonly ILogger? log;
    private readonly object lockObject = new();
    // The files the upgrade changes, each with its copy in the backup, or null for a file the upgrade creates
    private readonly Dictionary<string, string?> files = new(StringComparer.OrdinalIgnoreCase);
    // The files the upgrade creates by moving another file, with the file they were moved from
    private readonly Dictionary<string, string> movedFrom = new(StringComparer.OrdinalIgnoreCase);
    // The state the upgrade left each of its files in: the hash of the content, or empty for a file it deleted
    private readonly Dictionary<string, string> upgradedStates = new(StringComparer.OrdinalIgnoreCase);
    // The files snapshotted since the backup was made or opened: the upgrade, a retry or a save wrote them
    private readonly HashSet<string> written = new(StringComparer.OrdinalIgnoreCase);
    private string? backupDirectory;

    /// <param name="rootDirectory">
    /// The folder the backup mirrors — each file is stored under it preserving its path relative to this
    /// root. Usually the solution directory (the project directory for a standalone upgrade).
    /// </param>
    /// <param name="timestamp">The upgrade start time, used to name the backup folder.</param>
    /// <param name="log">Logger that receives a single notice when the backup folder is first created.</param>
    public UpgradeBackup(string rootDirectory, DateTime timestamp, ILogger? log = null)
        : this(rootDirectory, $"{FolderNamePrefix}{timestamp:yyyyMMdd-HHmmss}", log)
    {
    }

    private UpgradeBackup(string rootDirectory, string backupFolderName, ILogger? log)
    {
        this.rootDirectory = Path.GetFullPath(rootDirectory);
        this.backupFolderName = backupFolderName;
        this.log = log;
    }

    /// <summary>
    /// Whether <paramref name="folderPath"/> is a backup folder that <see cref="Open"/> reads.
    /// </summary>
    public static bool CanOpen(string folderPath) => File.Exists(Path.Combine(folderPath, ManifestFileName));

    /// <summary>
    /// The backup of an earlier upgrade, from the files its folder lists: it puts them back, and backs up new files in the same folder.
    /// </summary>
    /// <param name="folderPath">The backup folder, in the folder the backup mirrors.</param>
    /// <param name="log">Logger that receives the files that can't be put back.</param>
    /// <returns>The backup, or <c>null</c> when the folder lists no files.</returns>
    public static UpgradeBackup? Open(string folderPath, ILogger? log = null)
    {
        folderPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folderPath));
        if (!CanOpen(folderPath))
            return null;

        var backup = new UpgradeBackup(Path.GetDirectoryName(folderPath)!, Path.GetFileName(folderPath), log) { backupDirectory = folderPath };
        foreach (var line in File.ReadLines(Path.Combine(folderPath, ManifestFileName)))
        {
            if (string.IsNullOrWhiteSpace(line) || JsonSerializer.Deserialize<ManifestEntry>(line) is not { } entry)
                continue;
            var path = backup.FromManifestPath(entry.Path);
            if (entry.MovedFrom is not null)
                backup.movedFrom[path] = backup.FromManifestPath(entry.MovedFrom);
            else if (backup.GetBackupPath(path) is { } relative)
            {
                backup.files[path] = entry.Created ? null : Path.Combine(folderPath, relative);
                if (entry.Hash is not null)
                    backup.upgradedStates[path] = entry.Hash;
            }
        }
        return backup;
    }

    /// <summary>
    /// Records in the backup folder the state the upgrade leaves each of its files in, so that <see cref="GetChangedFiles"/>
    /// tells the files changed since. Called once the upgrade wrote its files; a later call records again the files written
    /// since the backup was made or opened. A file of an earlier upgrade that a retry doesn't write keeps its state: a change
    /// made to it since that upgrade still counts.
    /// </summary>
    public void RecordState()
    {
        lock (lockObject)
        {
            if (backupDirectory is null || !Directory.Exists(backupDirectory))
                return;
            foreach (var path in files.Keys.Where(written.Contains))
            {
                if (HashOf(path) is { } hash)
                    upgradedStates[path] = hash;
                else
                    upgradedStates.Remove(path);
            }
            try
            {
                WriteManifest();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                log?.Warning($"Could not record the files of the upgrade in [{backupDirectory}]: a revert will count them all as changed since the upgrade", e);
            }
        }
    }

    /// <summary>
    /// The files the upgrade changed or created that changed since it left them (see <see cref="RecordState"/>): putting the files
    /// back replaces these changes. A file whose state isn't recorded counts as changed.
    /// </summary>
    /// <returns>The full paths of the files.</returns>
    public IReadOnlyList<string> GetChangedFiles()
    {
        lock (lockObject)
            return files.Keys.Where(x => !upgradedStates.TryGetValue(x, out var state) || HashOf(x) != state).ToList();
    }

    /// <summary>
    /// Copies the current version of each file the backup puts back or deletes into a folder of the backup named after the
    /// time, so that putting the files back loses nothing.
    /// </summary>
    /// <returns>The full path of the folder, or <c>null</c> when none of the files exists.</returns>
    public string? KeepCurrentFiles()
    {
        lock (lockObject)
        {
            if (backupDirectory is null)
                return null;
            var folder = Path.Combine(backupDirectory, $"{RevertedFolderPrefix}{DateTime.Now:yyyyMMdd-HHmmss}");
            string? kept = null;
            foreach (var path in files.Keys.Where(File.Exists))
            {
                var target = Path.Combine(folder, GetBackupPath(path)!);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(path, target, overwrite: true);
                kept = folder;
            }
            return kept;
        }
    }

    /// <summary>
    /// The full path of the backup folder, or <c>null</c> while no file is backed up.
    /// </summary>
    public string? FolderPath
    {
        get
        {
            lock (lockObject)
                return backupDirectory;
        }
    }

    /// <summary>
    /// Writes a file of its own in the backup folder, such as the report of the upgrade it puts back, creating the folder if needed.
    /// </summary>
    /// <param name="name">The name of the file.</param>
    /// <param name="text">Its text.</param>
    /// <returns>The full path of the file.</returns>
    public string WriteFile(string name, string text)
    {
        lock (lockObject)
        {
            backupDirectory ??= Path.Combine(rootDirectory, backupFolderName);
            Directory.CreateDirectory(backupDirectory);
            var path = Path.Combine(backupDirectory, name);
            File.WriteAllText(path, text);
            return path;
        }
    }

    /// <summary>
    /// Whether a path, relative to a folder that may hold a backup, is in a backup folder: no upgrade step changes its files.
    /// </summary>
    public static bool IsInBackupFolder(string relativePath)
        => relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(x => x.StartsWith(FolderNamePrefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Copies <paramref name="originalFullPath"/> into the backup folder the first time it is seen: under its path relative
    /// to the backup root, or for a file outside the root, under <c>outside</c> and its full path. A file that doesn't
    /// exist yet is one the upgrade creates: <see cref="Restore"/> deletes it. No-op for a file under <c>bin/</c>,
    /// <c>obj/</c>, or the backup folder. Safe to call repeatedly for the same file.
    /// </summary>
    public void Snapshot(string originalFullPath)
    {
        if (string.IsNullOrEmpty(originalFullPath))
            return;

        var fullPath = Path.GetFullPath(originalFullPath);
        if (GetBackupPath(fullPath) is not { } relative)
            return;

        lock (lockObject)
        {
            written.Add(fullPath);
            if (files.ContainsKey(fullPath))
                return;

            if (backupDirectory is null)
            {
                // First file actually backed up: announce the folder once. Covers every upgrade write point
                // (source, project, assets) and entry point (compiler, Game Studio) since all funnel here.
                backupDirectory = Path.Combine(rootDirectory, backupFolderName);
                if (log is not null)
                    log.Info($"Upgrade: backing up the originals of modified files to [{backupDirectory}]. Review the changes before building.");
            }

            if (!File.Exists(fullPath))
            {
                files[fullPath] = null;
                AddToManifest(new ManifestEntry(ToManifestPath(fullPath), Created: true));
                return;
            }

            var target = Path.Combine(backupDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(fullPath, target, overwrite: true);
            files[fullPath] = target;
            AddToManifest(new ManifestEntry(ToManifestPath(fullPath)));
        }
    }

    /// <summary>
    /// Snapshots the two files of a move of <paramref name="sourceFullPath"/> to <paramref name="destinationFullPath"/>:
    /// <see cref="Restore"/> deletes a destination the move creates only once the source is back.
    /// </summary>
    public void SnapshotMove(string sourceFullPath, string destinationFullPath)
    {
        Snapshot(sourceFullPath);
        Snapshot(destinationFullPath);
        var destination = Path.GetFullPath(destinationFullPath);
        lock (lockObject)
        {
            var source = Path.GetFullPath(sourceFullPath);
            if (files.TryGetValue(destination, out var copy) && copy is null && movedFrom.TryAdd(destination, source))
                AddToManifest(new ManifestEntry(ToManifestPath(destination), MovedFrom: ToManifestPath(source)));
        }
    }

    /// <summary>
    /// Puts back as they were before the upgrade the files it changed for which <paramref name="include"/> is true: a file it
    /// changed gets its copy back, a file it created is deleted. They are no longer part of the backup. A file a move created
    /// stays while the file it was moved from isn't back: it holds the only copy.
    /// </summary>
    /// <param name="include">Whether to put back the file of this full path.</param>
    /// <returns>The full paths of the files that could not be put back.</returns>
    public IReadOnlyList<string> Restore(Func<string, bool> include)
    {
        ArgumentNullException.ThrowIfNull(include);
        var failed = new List<string>();
        lock (lockObject)
        {
            // The changed files first, so that a file a move created knows whether the file it was moved from is back
            foreach (var (path, copy) in files.Where(x => include(x.Key)).OrderBy(x => x.Value is null).ToList())
            {
                if (copy is null && movedFrom.TryGetValue(path, out var source) && files.ContainsKey(source))
                    continue;

                try
                {
                    if (copy is null)
                        File.Delete(path);
                    else
                        File.Copy(copy, path, overwrite: true);
                    files.Remove(path);
                    upgradedStates.Remove(path);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    log?.Error($"Could not put back [{path}] as it was before the upgrade", e);
                    failed.Add(path);
                }
            }

            // The folder lists only the files still to put back
            if (backupDirectory is not null && Directory.Exists(backupDirectory))
                WriteManifest();
        }
        return failed;
    }

    /// <summary>
    /// The full paths of the files the upgrade changed or created, which the backup puts back or deletes.
    /// </summary>
    public IReadOnlyList<string> GetFiles()
    {
        lock (lockObject)
            return files.Keys.ToList();
    }

    /// <summary>
    /// Whether the upgrade changed or created a file for which <paramref name="include"/> is true.
    /// </summary>
    /// <param name="include">Whether to count the file of this full path.</param>
    public bool HasChanges(Func<string, bool> include)
    {
        ArgumentNullException.ThrowIfNull(include);
        lock (lockObject)
            return files.Keys.Any(include);
    }

    // The path of the copy of a file under the backup folder, or null for a file the backup skips
    internal string? GetBackupPath(string fullPath)
    {
        var relative = Path.GetRelativePath(rootDirectory, fullPath);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            // C:\Shared\Lib.cs -> outside\C\Shared\Lib.cs, \\server\share\Lib.cs -> outside\server\share\Lib.cs. The root of a
            // network path ends without a separator: the rest must not start with one, or Combine takes it for a rooted path.
            var root = Path.GetPathRoot(fullPath) ?? "";
            var drive = root.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.VolumeSeparatorChar.ToString(), "", StringComparison.Ordinal);
            relative = Path.Combine(OutsideFolderName, drive, fullPath[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        else if (IsInBackupFolder(relative))
        {
            return null;
        }
        return ContainsSegment(relative, "bin") || ContainsSegment(relative, "obj") ? null : relative;
    }

    // Adds a file to the list in the backup folder, which backupDirectory names; called under the lock
    private void AddToManifest(ManifestEntry entry)
    {
        Directory.CreateDirectory(backupDirectory!);
        File.AppendAllText(Path.Combine(backupDirectory!, ManifestFileName), JsonSerializer.Serialize(entry, ManifestOptions) + "\n");
    }

    // Writes again the list of the files in the backup folder; called under the lock
    private void WriteManifest()
    {
        var entries = files.Select(x => new ManifestEntry(ToManifestPath(x.Key), Created: x.Value is null, Hash: upgradedStates.GetValueOrDefault(x.Key)))
            .Concat(movedFrom.Where(x => files.ContainsKey(x.Key)).Select(x => new ManifestEntry(ToManifestPath(x.Key), MovedFrom: ToManifestPath(x.Value))));
        File.WriteAllLines(Path.Combine(backupDirectory!, ManifestFileName), entries.Select(x => JsonSerializer.Serialize(x, ManifestOptions)));
    }

    // The hash of the content of a file, empty for a file that doesn't exist, or null for one that can't be read
    private static string? HashOf(string path)
    {
        try
        {
            if (!File.Exists(path))
                return "";
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // A file under the root is listed by its path relative to the root, so that the backup still opens once the root moved
    private string ToManifestPath(string fullPath)
    {
        var relative = Path.GetRelativePath(rootDirectory, fullPath);
        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative) ? fullPath : relative;
    }

    private string FromManifestPath(string path) => Path.GetFullPath(Path.Combine(rootDirectory, path));

    private static readonly JsonSerializerOptions ManifestOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

    // A file of the backup: one it copied or one the upgrade created, with the state the upgrade left it in when it is recorded;
    // or the file a created one was moved from
    private sealed record ManifestEntry(string Path, bool Created = false, string? MovedFrom = null, string? Hash = null);

    private static bool ContainsSegment(string relativePath, string segment)
    {
        foreach (var part in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (string.Equals(part, segment, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

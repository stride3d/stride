// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets;

/// <summary>
/// Copy-on-write snapshot of the files an in-place upgrade is about to overwrite. Armed by
/// <see cref="PackageLoadParameters.BackupBeforeUpgrade"/> and called from each upgrade write point right
/// before it overwrites a file. The timestamped backup folder is created lazily on the first real snapshot
/// (so nothing is written when an upgrade modifies no files), and each original is copied at most once
/// (a file may be touched by several upgraders). <see cref="Restore"/> puts the files back as they were.
/// </summary>
public sealed class UpgradeBackup
{
    // The folder of the backup that holds the files outside the root, by drive: C:\Shared\Lib.cs is outside\C\Shared\Lib.cs
    private const string OutsideFolderName = "outside";

    private readonly string rootDirectory;
    private readonly string backupFolderName;
    private readonly ILogger? log;
    private readonly object lockObject = new();
    // The files the upgrade changes, each with its copy in the backup, or null for a file the upgrade creates
    private readonly Dictionary<string, string?> files = new(StringComparer.OrdinalIgnoreCase);
    private string? backupDirectory;

    /// <param name="rootDirectory">
    /// The folder the backup mirrors — each file is stored under it preserving its path relative to this
    /// root. Usually the solution directory (the project directory for a standalone upgrade).
    /// </param>
    /// <param name="timestamp">The upgrade start time, used to name the backup folder.</param>
    /// <param name="log">Logger that receives a single notice when the backup folder is first created.</param>
    public UpgradeBackup(string rootDirectory, DateTime timestamp, ILogger? log = null)
    {
        this.rootDirectory = Path.GetFullPath(rootDirectory);
        backupFolderName = $".stride-backup-{timestamp:yyyyMMdd-HHmmss}";
        this.log = log;
    }

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
            if (files.ContainsKey(fullPath))
                return;

            if (!File.Exists(fullPath))
            {
                files[fullPath] = null;
                return;
            }

            if (backupDirectory is null)
            {
                // First file actually backed up: announce the folder once. Covers every upgrade write point
                // (source, project, assets) and entry point (compiler, Game Studio) since all funnel here.
                backupDirectory = Path.Combine(rootDirectory, backupFolderName);
                if (log is not null)
                    log.Info($"Upgrade: backing up the originals of modified files to [{backupDirectory}]. Review the changes before building.");
            }
            var target = Path.Combine(backupDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(fullPath, target, overwrite: true);
            files[fullPath] = target;
        }
    }

    /// <summary>
    /// Puts back as they were before the upgrade the files it changed for which <paramref name="include"/> is true: a file it
    /// changed gets its copy back, a file it created is deleted. They are no longer part of the backup.
    /// </summary>
    /// <param name="include">Whether to put back the file of this full path.</param>
    /// <returns>The full paths of the files that could not be put back.</returns>
    public IReadOnlyList<string> Restore(Func<string, bool> include)
    {
        ArgumentNullException.ThrowIfNull(include);
        var failed = new List<string>();
        lock (lockObject)
        {
            foreach (var (path, copy) in files.Where(x => include(x.Key)).ToList())
            {
                try
                {
                    if (copy is null)
                        File.Delete(path);
                    else
                        File.Copy(copy, path, overwrite: true);
                    files.Remove(path);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    log?.Error($"Could not put back [{path}] as it was before the upgrade", e);
                    failed.Add(path);
                }
            }
        }
        return failed;
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
    private string? GetBackupPath(string fullPath)
    {
        var relative = Path.GetRelativePath(rootDirectory, fullPath);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            // C:\Shared\Lib.cs -> outside\C\Shared\Lib.cs, \\server\share\Lib.cs -> outside\server\share\Lib.cs
            var root = Path.GetPathRoot(fullPath) ?? "";
            var drive = root.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.VolumeSeparatorChar.ToString(), "", StringComparison.Ordinal);
            relative = Path.Combine(OutsideFolderName, drive, fullPath[root.Length..]);
        }
        else if (relative.StartsWith(".stride-backup", StringComparison.Ordinal))
        {
            return null;
        }
        return ContainsSegment(relative, "bin") || ContainsSegment(relative, "obj") ? null : relative;
    }

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

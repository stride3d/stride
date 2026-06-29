// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets;

/// <summary>
/// Copy-on-write snapshot of the files an in-place upgrade is about to overwrite. Armed by
/// <see cref="PackageLoadParameters.BackupBeforeUpgrade"/> and called from each upgrade write point right
/// before it overwrites a file. The timestamped backup folder is created lazily on the first real snapshot
/// (so nothing is written when an upgrade modifies no files), and each original is copied at most once
/// (a file may be touched by several upgraders).
/// </summary>
public sealed class UpgradeBackup
{
    private readonly string rootDirectory;
    private readonly string backupFolderName;
    private readonly ILogger? log;
    private readonly object lockObject = new();
    private readonly HashSet<string> snapshotted = new(StringComparer.OrdinalIgnoreCase);
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
    /// Copies <paramref name="originalFullPath"/> into the backup folder (preserving its path relative to the
    /// backup root) the first time it is seen. No-op if the file doesn't exist, lies outside the root, or sits
    /// under <c>bin/</c>, <c>obj/</c>, or the backup folder. Safe to call repeatedly for the same file.
    /// </summary>
    public void Snapshot(string originalFullPath)
    {
        if (string.IsNullOrEmpty(originalFullPath))
            return;

        var fullPath = Path.GetFullPath(originalFullPath);

        var relative = Path.GetRelativePath(rootDirectory, fullPath);
        // Outside the root (relative escapes with "..") or excluded build/backup output: skip.
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            return;
        if (relative.StartsWith(".stride-backup", StringComparison.Ordinal)
            || ContainsSegment(relative, "bin")
            || ContainsSegment(relative, "obj"))
            return;

        lock (lockObject)
        {
            if (!snapshotted.Add(fullPath))
                return;

            if (!File.Exists(fullPath))
                return;

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
        }
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

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.IO;

/// <summary>
/// The last write time and the size of the files a listener of a <see cref="DirectoryWatcher"/> follows, to find which of them
/// changed while the watcher missed changes (<see cref="DirectoryWatcher.ChangesMissed"/>).
/// </summary>
/// <remarks>
/// A listener records each file it follows, then each file its events are about, and replays the events
/// <see cref="Reconcile"/> finds as if the watcher had sent them. A file that does not exist is recorded too, so that it is found
/// when it comes back. The methods can be called from several threads.
/// </remarks>
public sealed class FileWriteTimes
{
    private static readonly StringComparison PathComparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    private static readonly StringComparer PathComparer = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    private readonly Dictionary<string, FileState> files = new(PathComparer);

    /// <summary>
    /// Starts following a file: records its current state, unless it is followed already.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns><c>true</c> if the file was not followed yet.</returns>
    public bool Add(string path)
    {
        path = Path.GetFullPath(path);
        lock (files)
        {
            if (files.ContainsKey(path))
                return false;
            files.Add(path, FileState.Read(path));
            return true;
        }
    }

    /// <summary>
    /// Records the current state of a file, after an event of the watcher about it.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    public void Update(string path)
    {
        path = Path.GetFullPath(path);
        lock (files)
            files[path] = FileState.Read(path);
    }

    /// <summary>
    /// Whether a file is followed.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    public bool Contains(string path)
    {
        path = Path.GetFullPath(path);
        lock (files)
            return files.ContainsKey(path);
    }

    /// <summary>
    /// Finds the files of a directory that changed since they were recorded, and records their current state.
    /// </summary>
    /// <param name="directory">The directory whose changes were missed, with its subdirectories.</param>
    /// <param name="currentFiles">Files there that may not be followed yet, such as the files of the directory a listener lists: a
    /// file that is not followed yet is created.</param>
    /// <returns>The event of each file that was created, changed or deleted.</returns>
    public List<FileEvent> Reconcile(string directory, IEnumerable<string>? currentFiles = null)
    {
        // Listed before the lock: an update after an event does not wait for the disk
        var listedFiles = currentFiles?.Select(Path.GetFullPath).ToList() ?? [];
        var changes = new List<FileEvent>();
        lock (files)
        {
            var paths = files.Keys.Where(x => IsUnder(x, directory)).ToList();
            paths.AddRange(listedFiles.Where(x => !files.ContainsKey(x)).Distinct(PathComparer));

            foreach (var path in paths)
            {
                var state = FileState.Read(path);
                var known = files.TryGetValue(path, out var recorded);
                if (known && recorded == state)
                    continue;

                files[path] = state;
                var changeType = (known && recorded.Exists, state.Exists) switch
                {
                    (false, true) => FileEventChangeType.Created,
                    (true, false) => FileEventChangeType.Deleted,
                    (true, true) => FileEventChangeType.Changed,
                    // A file followed while it does not exist, and still missing
                    _ => (FileEventChangeType?)null,
                };
                if (changeType is { } type)
                    changes.Add(new FileEvent(type, Path.GetFileName(path), path));
            }
        }
        return changes;
    }

    /// <summary>
    /// Whether a path is a directory or is in it, with the case of the paths of the file system of the platform.
    /// </summary>
    /// <param name="path">The full path of a file or a directory.</param>
    /// <param name="directory">The full path of the directory.</param>
    public static bool IsUnder(string path, string directory)
    {
        directory = Path.TrimEndingDirectorySeparator(directory);
        // A root keeps its separator
        if (Path.EndsInDirectorySeparator(directory))
            return path.StartsWith(directory, PathComparison);
        return path.StartsWith(directory, PathComparison)
            && (path.Length == directory.Length || path[directory.Length] == Path.DirectorySeparatorChar || path[directory.Length] == Path.AltDirectorySeparatorChar);
    }

    private readonly record struct FileState(bool Exists, DateTime WriteTime, long Length)
    {
        public static FileState Read(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists ? new FileState(true, info.LastWriteTimeUtc, info.Length) : default;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return default;
            }
        }
    }
}

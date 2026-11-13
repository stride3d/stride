// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_DESKTOP
using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Core.IO;

public partial class DirectoryWatcher
{
    private readonly Dictionary<string, DirectoryWatcherItem> watchers = new(StringComparer.CurrentCultureIgnoreCase);

    private void InitializeInternal()
    {
        watcherCheckThread = new Thread(SafeAction.Wrap(RunCheckWatcher)) { IsBackground = true, Name = "RunCheckWatcher thread" };
        watcherCheckThread.Start();
    }

    private void DisposeInternal()
    {
        foreach (var watcher in watchers.Values)
        {
            if (watcher.Watcher != null)
            {
                DisposeNativeWatcher(watcher.Watcher);
            }
            watcher.Watcher = null;
        }
        watchers.Clear();
    }

    private List<string> GetTrackedDirectoriesInternal()
    {
        List<string> directories;
        lock (watchers)
        {
            directories = ListTrackedDirectories().Select(pair => pair.Key).ToList();
        }
        directories.Sort();
        return directories;
    }

    private void TrackInternal(string path)
    {
        var info = GetDirectoryInfoFromPath(path);
        if (info == null)
        {
            return;
        }

        lock (watchers)
        {
            Track(info, true);
        }
    }

    private void UnTrackInternal(string path)
    {
        var info = GetDirectoryInfoFromPath(path);
        if (info == null)
        {
            return;
        }

        lock (watchers)
        {
            if (!watchers.TryGetValue(info.FullName, out var watcher))
            {
                return;
            }

            UnTrack(watcher, true);
        }
    }

    private void RunCheckWatcher()
    {
        try
        {
            while (!exitThread.Wait(SleepBetweenWatcherCheck))
            {
                List<string>? restarted = null;
                lock (watchers)
                {
                    var list = ListTrackedDirectories().ToList();
                    foreach (var watcherKeyPath in list)
                    {
                        if (!watcherKeyPath.Value.IsPathExist())
                        {
                            UnTrack(watcherKeyPath.Value, true);
                            OnModified(this, new FileEvent(FileEventChangeType.Deleted, Path.GetFileName(watcherKeyPath.Value.Path), watcherKeyPath.Value.Path));
                        }
                    }

                    foreach (var (path, watcher) in ListTrackedDirectories().Where(pair => pair.Value.Failed).ToList())
                    {
                        if (TryRestart(watcher))
                            (restarted ??= []).Add(path);
                    }

                    // If no more directories are tracked, clear completely the watchers tree
                    if (!ListTrackedDirectories().Any())
                    {
                        watchers.Clear();
                    }
                }

                // Outside of the lock: a listener can track other files
                foreach (var path in restarted ?? [])
                {
                    try
                    {
                        OnChangesMissed(this, new ChangesMissedEventArgs(path));
                    }
                    catch (Exception ex)
                    {
                        // A listener must not end the checks
                        Trace.WriteLine($"Unexpected exception in a listener of ChangesMissed: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Unexpected end of thread {ex}");
            //Console.WriteLine("Unexpected exception {0}", ex);
        }
    }

    private IEnumerable<KeyValuePair<string, DirectoryWatcherItem>> ListTrackedDirectories()
    {
        return watchers.Where(pair => pair.Value.Watcher != null);
    }

    private DirectoryInfo? GetDirectoryInfoFromPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        // 1) Extract directory information from path
        DirectoryInfo info;
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path);
        }

        if (path != null && Directory.Exists(path))
        {
            // TODO : Need to investigate later whether ToLower method is safe to remove for Windows OS
            info = new DirectoryInfo(OperatingSystem.IsLinux() ? path :  path.ToLowerInvariant());
        }
        else
        {
            return null;
        }

        return info;
    }

    private IEnumerable<DirectoryWatcherItem> ListTracked(IEnumerable<DirectoryInfo> directories)
    {
        foreach (var directoryInfo in directories)
        {
            if (watchers.TryGetValue(directoryInfo.FullName, out var watcher))
            {
                yield return watcher;
            }
        }
    }

    private IEnumerable<DirectoryWatcherItem> ListTrackedChildren(DirectoryWatcherItem watcher)
    {
        return ListTracked(watcher.ListChildrenDirectories());
    }

    private int CountTracked(IEnumerable<DirectoryInfo> directories)
    {
        return ListTracked(directories).Count(watcher => watcher.Watcher != null);
    }

    private DirectoryWatcherItem Track(DirectoryInfo info, bool watcherNode)
    {
        if (watchers.TryGetValue(info.FullName, out var watcher))
        {
            if (watcher.Watcher == null && watcherNode)
            {
                watcher.Watcher = CreateFileSystemWatcher(watcher.Path);
            }
            watcher.TrackCount++;
            return watcher;
        }

        var parent = info.Parent != null ? Track(info.Parent, false) : null;

        if (parent != null && watcherNode)
        {
            if (parent.Watcher != null)
            {
                return parent;
            }

            var childrenDirectoryList = parent.ListChildrenDirectories().ToList();
            var countTracked = CountTracked(childrenDirectoryList);

            var newCount = countTracked + 1;
            if (newCount == childrenDirectoryList.Count && newCount > 1)
            {
                UnTrack(parent, false);
                parent.Watcher = CreateFileSystemWatcher(parent.Path);
                return parent;
            }
        }

        watcher = new DirectoryWatcherItem(info) { Parent = parent };
        if (watcherNode)
        {
            watcher.Watcher = CreateFileSystemWatcher(watcher.Path);
        }
        watchers.Add(watcher.Path, watcher);

        watcher.TrackCount++;
        return watcher;
    }

    private void UnTrack(DirectoryWatcherItem watcher, bool removeWatcherFromGlobals)
    {
        foreach (var child in ListTrackedChildren(watcher))
        {
            UnTrack(child, true);
        }

        watcher.TrackCount--;

        if (watcher.TrackCount == 0)
        {
            if (watcher.Watcher != null)
            {
                DisposeNativeWatcher(watcher.Watcher);
                watcher.Watcher = null;
            }

            watcher.Parent = null;

            if (removeWatcherFromGlobals)
            {
                watchers.Remove(watcher.Path);
            }
        }
    }

    private void DisposeNativeWatcher(FileSystemWatcher watcher)
    {
        //Console.WriteLine("Watcher disposing {0}", watcher.Path);
        watcher.EnableRaisingEvents = false;
        watcher.Changed -= OnModified;
        watcher.Created -= OnModified;
        watcher.Deleted -= OnModified;
        watcher.Renamed -= OnModified;
        watcher.Error -= WatcherOnError;
        watcher.Dispose();
        //Console.WriteLine("Watcher disposed {0}", watcher.Path);
    }

    protected FileSystemWatcher CreateFileSystemWatcher(string directory)
    {
        //Console.WriteLine("Watcher creating {0}", directory);
        var watcher = new FileSystemWatcher()
            {
                Path = directory,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = FileFilter,
                IncludeSubdirectories = true,
                // The default 8 KB overflows when a build writes to bin and obj; 64 KB is the most a network share allows
                InternalBufferSize = 64 * 1024,
            };

        watcher.Changed += OnModified;
        watcher.Created += OnModified;
        watcher.Deleted += OnModified;
        watcher.Renamed += OnModified;
        watcher.Error += WatcherOnError;

        try
        {
            watcher.EnableRaisingEvents = true;
        }
        catch
        {
            // On Linux, past the inotify limits
            DisposeNativeWatcher(watcher);
            throw;
        }

        //Console.WriteLine("Watcher created {0}", directory);
        return watcher;
    }

    private void WatcherOnError(object sender, ErrorEventArgs errorEventArgs)
    {
        try
        {
            lock (watchers)
            {
                // The buffer of the events overflowed, or the watch stopped: the check thread starts it again, then tells the
                // listeners that they may have missed changes
                var watcher = watchers.Values.FirstOrDefault(item => item.Watcher == sender);
                if (watcher != null)
                {
                    watcher.Failed = true;
                    Trace.WriteLine($"The watch of '{watcher.Path}' failed and starts again: {errorEventArgs.GetException()?.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Unexpected exception in WatcherOnError: {ex}");
        }
    }

    /// <summary>
    /// Replaces the native watcher of a watch that failed. The caller holds the lock of <see cref="watchers"/>.
    /// </summary>
    /// <returns><c>true</c> if the new watch runs; otherwise the next check tries again.</returns>
    private bool TryRestart(DirectoryWatcherItem watcher)
    {
        // The new watch runs before the old one stops, so that no change falls between them; at a limit of the system, such as the
        // number of inotify instances on Linux, the old one must stop first. A disposed watcher can be disposed again.
        var restarted = TryCreateFileSystemWatcher(watcher.Path);
        DisposeNativeWatcher(watcher.Watcher!);
        restarted ??= TryCreateFileSystemWatcher(watcher.Path);
        if (restarted == null)
            return false;

        watcher.Watcher = restarted;
        watcher.Failed = false;
        return true;
    }

    private FileSystemWatcher? TryCreateFileSystemWatcher(string directory)
    {
        try
        {
            return CreateFileSystemWatcher(directory);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Stops the watch of a tracked directory and raises its error, as the file system does when a watch fails.
    /// </summary>
    internal void RaiseWatchError(string path, Exception exception)
    {
        FileSystemWatcher? watcher = null;
        lock (watchers)
        {
            if (GetDirectoryInfoFromPath(path) is { } info && watchers.TryGetValue(info.FullName, out var item))
                watcher = item.Watcher;
        }
        if (watcher == null)
            throw new InvalidOperationException($"The directory '{path}' is not watched.");

        watcher.EnableRaisingEvents = false;
        WatcherOnError(watcher, new ErrorEventArgs(exception));
    }

    private void OnModified(object sender, FileSystemEventArgs e)
    {
        lock (watchers)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted && watchers.TryGetValue(e.FullPath, out var watcher))
            {
                UnTrack(watcher, true);
            }
        }

        var handler = Modified;
        if (handler != null)
        {
            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                var renamedEventArgs = e as RenamedEventArgs;
                OnModified(this, new FileRenameEvent(e.Name, e.FullPath, renamedEventArgs.OldFullPath));
            }
            else
            {
                OnModified(this, new FileEvent((FileEventChangeType)e.ChangeType, e.Name, e.FullPath));
            }
        }
    }

    [DebuggerDisplay("Active: {IsActive}, Path: {Path}")]
    private sealed class DirectoryWatcherItem
    {
        public DirectoryWatcherItem(DirectoryInfo path)
        {
            // TODO : Need to investigate later whether ToLower method is safe to remove for Windows OS
            Path = OperatingSystem.IsLinux() ? path.FullName : path.FullName.ToLowerInvariant();
        }

        public DirectoryWatcherItem? Parent;

        public string Path { get; }

        public bool IsPathExist()
        {
            return Directory.Exists(Path);
        }

        public int TrackCount { get; set; }

        public FileSystemWatcher? Watcher { get; set; }

        /// <summary>
        /// Whether <see cref="Watcher"/> raised an error: it may have stopped, and changes were missed.
        /// </summary>
        public bool Failed { get; set; }

        public IEnumerable<DirectoryInfo> ListChildrenDirectories()
        {
            var info = new DirectoryInfo(Path);
            try
            {
                if (info.Exists)
                {
                    return info.EnumerateDirectories();
                }
            }
            catch (Exception)
            {
                // An exception can occur if the file is being removed
            }
            return [];
        }

        private bool IsActive
        {
            get
            {
                return Watcher != null;
            }
        }
    }
}
#endif

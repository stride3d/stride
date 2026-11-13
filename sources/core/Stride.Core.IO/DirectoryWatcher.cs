// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.IO;

/// <summary>
/// Track file system events from several directories.
/// </summary>
public partial class DirectoryWatcher : IDisposable
{
    private const int SleepBetweenWatcherCheck = 200;
    private Thread? watcherCheckThread;
    // Set by Dispose: the check thread waits on it between its checks, so that it ends at once
    private readonly ManualResetEventSlim exitThread = new();

    /// <summary>
    /// Occurs when a file/directory change occurred.
    /// </summary>
    public event EventHandler<FileEvent>? Modified;

    /// <summary>
    /// Occurs when the watch of a directory failed and started again, for example after the buffer of its events overflowed: the
    /// changes in that directory in the meantime may never be reported by <see cref="Modified"/>.
    /// </summary>
    /// <remarks>
    /// It is raised once the new watch runs, so a listener that reads the files of the directory again then misses nothing; it can
    /// find what changed with a <see cref="FileWriteTimes"/>.
    /// </remarks>
    public event EventHandler<ChangesMissedEventArgs>? ChangesMissed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryWatcher"/> class.
    /// </summary>
    /// <param name="fileFilter">The file filter By default null default to *.*</param>
    public DirectoryWatcher(string? fileFilter = null)
    {
        FileFilter = fileFilter ?? "*.*";
        InitializeInternal();
    }

    /// <summary>
    /// Gets the file filter used by this instance.
    /// </summary>
    /// <value>The file filter.</value>
    public string FileFilter { get; private set; }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            exitThread.Set();
            if (watcherCheckThread != null)
            {
                watcherCheckThread.Join();
                watcherCheckThread = null;
            }
            DisposeInternal();
        }
    }

    /// <summary>
    /// Gets a list of current directories being tracked.
    /// </summary>
    /// <returns>A list of current directories being tracked</returns>
    public List<string> GetTrackedDirectories()
    {
        return GetTrackedDirectoriesInternal();
    }

    /// <summary>
    /// Tracks the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <remarks>
    /// If path is a file, this will used the parent directory. If the path is invalid, it will not fail but just skip it.
    /// </remarks>
    public void Track(string path)
    {
        TrackInternal(path);
    }

    /// <summary>
    /// UnTracks the specified path.
    /// </summary>
    /// <remarks>
    /// If path is a file, this will used the parent directory. If the path is invalid, it will not fail but just skip it.
    /// </remarks>
    public void UnTrack(string path)
    {
        UnTrackInternal(path);
    }

    /// <summary>
    /// Called when a file event occurred.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The file event.</param>
    protected virtual void OnModified(object sender, FileEvent e)
    {
        Modified?.Invoke(sender, e);
    }

    /// <summary>
    /// Called when the watch of a directory started again after an error.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The directory whose changes may have been missed.</param>
    protected virtual void OnChangesMissed(object sender, ChangesMissedEventArgs e)
    {
        ChangesMissed?.Invoke(sender, e);
    }

#if !STRIDE_PLATFORM_DESKTOP
    // Doesn't throw any exceptions on other platforms

    private void InitializeInternal()
    {
    }

    private void DisposeInternal()
    {
    }

    private void TrackInternal(string path)
    {
    }

    private void UnTrackInternal(string path)
    {
    }

    private List<string> GetTrackedDirectoriesInternal()
    {
        return new List<string>();
    }
#endif
}

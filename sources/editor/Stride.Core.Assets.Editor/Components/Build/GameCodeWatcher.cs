// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO.Enumeration;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Watches the code of the projects whose assembly the editor loads (the libraries of the game and the plugins of the session) and
/// reports its changes by bursts, as the project watcher of the WPF editor does for its reload of the game assemblies.
/// </summary>
/// <remarks>
/// <para>
/// A change is a <c>.cs</c> file created, changed, deleted or renamed in the folder of such a project, or a new content of its project
/// file. The build output of a project (its <c>bin</c> and <c>obj</c> folders) and its hidden folders do not count, so that the
/// builds of the editor do not report anything; neither do the projects the editor does not load (the executables of the game), even
/// in a folder of a loaded project.
/// </para>
/// <para>
/// An IDE saves several files at once: the changes are grouped until none came for <see cref="BurstDelay"/>, then reported together
/// by <see cref="Changed"/>, from a thread of the thread pool.
/// </para>
/// <para>
/// When the file system watcher missed changes (its buffer of events overflowed during a build), the files are read again and the
/// changes found are reported as its events.
/// </para>
/// </remarks>
public sealed class GameCodeWatcher : IDisposable
{
    /// <summary>
    /// The default time without a change that ends a burst, as in the WPF editor.
    /// </summary>
    public static readonly TimeSpan DefaultBurstDelay = TimeSpan.FromMilliseconds(500);

    private static readonly Logger Log = GlobalLogger.GetLogger(nameof(GameCodeWatcher));
    private static readonly StringComparison PathComparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    private static readonly StringComparer PathComparer = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    private readonly Lock gate = new();
    // Every project of the session, the deepest folder first: a file belongs to the first one whose folder holds it
    private readonly List<WatchedProject> projects;
    private readonly List<GameCodeChange> pendingChanges = [];
    // The files that count, to find the changes the watcher missed
    private readonly FileWriteTimes fileTimes = new();
    private readonly DirectoryWatcher? directoryWatcher;
    private readonly Timer burstTimer;
    private bool disposed;

    /// <summary>
    /// Watches the code of the given projects.
    /// </summary>
    /// <param name="codeProjects">The project files of the projects whose assembly the editor loads.</param>
    /// <param name="otherProjects">The project files of the other projects of the session: their files never count, even in a
    /// folder of a code project.</param>
    /// <param name="burstDelay">The time without a change that ends a burst; <see cref="DefaultBurstDelay"/> when <c>null</c>.</param>
    /// <param name="watchFileSystem">Whether to watch the file system; the tests report the changes with <see cref="OnFileChanged"/>.</param>
    public GameCodeWatcher(IEnumerable<string> codeProjects, IEnumerable<string>? otherProjects = null, TimeSpan? burstDelay = null, bool watchFileSystem = true)
        : this(codeProjects, otherProjects, burstDelay, watchFileSystem, static (watcher, project) => watcher.Track(project))
    {
    }

    /// <summary>
    /// Watches the code of the given projects, with a given way to start watching the folder of a code project (the tests make it fail).
    /// </summary>
    internal GameCodeWatcher(IEnumerable<string> codeProjects, IEnumerable<string>? otherProjects, TimeSpan? burstDelay, bool watchFileSystem, Action<DirectoryWatcher, string> trackProject)
    {
        ArgumentNullException.ThrowIfNull(codeProjects);
        BurstDelay = burstDelay ?? DefaultBurstDelay;
        CodeProjects = codeProjects.Select(Path.GetFullPath).Distinct(PathComparer).ToList();
        projects = CodeProjects.Select(x => new WatchedProject(x, true))
            .Concat((otherProjects ?? []).Select(Path.GetFullPath).Where(x => !CodeProjects.Contains(x, PathComparer)).Distinct(PathComparer).Select(x => new WatchedProject(x, false)))
            .OrderByDescending(x => x.Directory.Length)
            .ToList();
        burstTimer = new Timer(_ => EndBurst());

        if (watchFileSystem && CodeProjects.Count > 0)
        {
            var watcher = new DirectoryWatcher();
            watcher.Modified += (_, e) => OnFileChanged(e);
            watcher.ChangesMissed += (_, e) => OnChangesMissed(e.Directory);
            try
            {
                // The watcher follows the folder of a file, and its subfolders
                foreach (var project in CodeProjects)
                    trackProject(watcher, project);
                directoryWatcher = watcher;
            }
            catch (Exception e)
            {
                // On Linux, a file system watcher fails to start past the inotify limits (fs.inotify.max_user_instances): the
                // session still opens, without the offer to reload the game code
                watcher.Dispose();
                Log.Warning("The code of the game is not watched: the editor will not offer to reload it when it changes.", e);
            }

            if (directoryWatcher is not null)
            {
                foreach (var project in projects.Where(x => x.IsCode))
                {
                    fileTimes.Add(project.ProjectPath);
                    foreach (var file in EnumerateProjectFiles(project.Directory, IsCodeFile))
                        fileTimes.Add(file);
                }
            }
        }
    }

    /// <summary>
    /// Whether the file system is watched: it is not when the watcher could not start.
    /// </summary>
    public bool IsWatchingFileSystem => directoryWatcher is not null;

    /// <summary>
    /// The project files of the watched projects.
    /// </summary>
    public IReadOnlyList<string> CodeProjects { get; }

    /// <summary>
    /// The time without a change that ends a burst.
    /// </summary>
    public TimeSpan BurstDelay { get; }

    /// <summary>
    /// Whether the watcher was disposed: it reports nothing more.
    /// </summary>
    internal bool IsDisposed
    {
        get
        {
            lock (gate)
                return disposed;
        }
    }

    /// <summary>
    /// Raised at the end of a burst of changes, from a thread of the thread pool, with each changed file once.
    /// </summary>
    public event EventHandler<GameCodeChangedEventArgs>? Changed;

    /// <summary>
    /// The project files of the session whose assembly the editor loads: the projects of the session that have a project reference
    /// among the assemblies of their package. The executables of the game load no assembly in the editor.
    /// </summary>
    public static IReadOnlyList<string> GetCodeProjects(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var result = new List<string>();
        foreach (var package in session.LocalPackages.Where(x => x.IsLoaded))
        {
            foreach (var loadedAssembly in package.LoadedAssemblies)
            {
                if (loadedAssembly.ProjectReference?.Location is not { } location)
                    continue;
                if (location.IsRelative)
                    location = UPath.Combine(package.PackagePath.GetFullDirectory(), location);
                result.Add(location.ToOSPath());
            }
        }
        return result.Distinct(PathComparer).ToList();
    }

    /// <summary>
    /// Watches the code projects of a session (<see cref="GetCodeProjects"/>); its other projects (the executables) stay out.
    /// </summary>
    public static GameCodeWatcher Create(SessionViewModel session)
    {
        var otherProjects = session.LocalPackages.OfType<ProjectViewModel>().Select(x => x.ProjectPath.ToOSPath());
        return new GameCodeWatcher(GetCodeProjects(session), otherProjects);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
            pendingChanges.Clear();
        }
        directoryWatcher?.Dispose();
        burstTimer.Dispose();
    }

    /// <summary>
    /// Handles a change of the file system under the folder of a code project.
    /// </summary>
    internal void OnFileChanged(FileEvent e)
    {
        var changes = new List<GameCodeChange>(2);
        AddChange(changes, e.FullPath, e.ChangeType);
        // A renamed file leaves its old name, which may have counted
        if (e is FileRenameEvent rename && !string.IsNullOrEmpty(rename.OldFullPath))
            AddChange(changes, rename.OldFullPath, FileEventChangeType.Deleted);
        if (changes.Count == 0)
            return;

        lock (gate)
        {
            if (disposed)
                return;
            foreach (var change in changes)
            {
                if (!pendingChanges.Any(x => PathComparer.Equals(x.FilePath, change.FilePath)))
                    pendingChanges.Add(change);
            }
            // The burst goes on
            burstTimer.Change(BurstDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Finds the changes the file system watcher missed in a folder, from the files of the code projects there, and reports them as
    /// its events.
    /// </summary>
    /// <param name="directory">The folder whose watch started again.</param>
    internal void OnChangesMissed(string directory)
    {
        if (IsDisposed)
            return;

        var currentFiles = new List<string>();
        foreach (var project in projects.Where(x => x.IsCode))
        {
            var projectDirectory = project.Directory;
            if (FileWriteTimes.IsUnder(projectDirectory, directory))
                currentFiles.AddRange(EnumerateProjectFiles(projectDirectory, IsCodeFile));
            else if (FileWriteTimes.IsUnder(directory, projectDirectory))
                currentFiles.AddRange(EnumerateProjectFiles(directory, IsCodeFile));
        }

        foreach (var change in fileTimes.Reconcile(directory, currentFiles))
            OnFileChanged(change);
    }

    /// <summary>
    /// Lists the files of a folder of a project and of its subfolders, without the build output (<c>bin</c>, <c>obj</c>) and the
    /// hidden folders, as the default items of an SDK project.
    /// </summary>
    /// <param name="directory">The folder of the project, or a folder in it.</param>
    /// <param name="include">Which files to list, from their full path.</param>
    /// <returns>The full paths of the files, or none when the folder does not exist.</returns>
    public static IEnumerable<string> EnumerateProjectFiles(string directory, Func<string, bool> include)
    {
        if (!Directory.Exists(directory))
            return [];

        // Hidden files count, as for the watcher
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = 0 };
        try
        {
            return new FileSystemEnumerable<string>(directory, (ref FileSystemEntry entry) => entry.ToFullPath(), options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && include(entry.ToFullPath()),
                ShouldRecursePredicate = (ref FileSystemEntry entry) => !IsExcludedFolder(entry.FileName),
            }.ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The folder is being deleted, or cannot be read
            return [];
        }
    }

    private void AddChange(List<GameCodeChange> changes, string filePath, FileEventChangeType changeType)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var project = projects.FirstOrDefault(x => filePath.StartsWith(x.Directory, PathComparison));
        if (project is not { IsCode: true })
            return;

        if (PathComparer.Equals(filePath, project.ProjectPath))
        {
            fileTimes.Update(filePath);
            if (project.HasNewContent(changeType))
                changes.Add(new GameCodeChange(project.ProjectPath, filePath));
            return;
        }

        if (!IsCodeFile(filePath) || IsExcluded(filePath[project.Directory.Length..]))
            return;
        fileTimes.Update(filePath);
        changes.Add(new GameCodeChange(project.ProjectPath, filePath));
    }

    private static bool IsCodeFile(string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a file of a project is in its build output (<c>bin</c>, <c>obj</c>) or in a hidden folder, as the default items of an
    /// SDK project leave them out.
    /// </summary>
    /// <param name="relativePath">The path of the file in the folder of its project.</param>
    private static bool IsExcluded(string relativePath)
    {
        var folders = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        // The last part is the file itself
        for (var i = 0; i < folders.Length - 1; ++i)
        {
            if (IsExcludedFolder(folders[i]))
                return true;
        }
        return false;
    }

    private static bool IsExcludedFolder(ReadOnlySpan<char> folder)
    {
        return folder.StartsWith('.') || folder.Equals("bin", StringComparison.OrdinalIgnoreCase) || folder.Equals("obj", StringComparison.OrdinalIgnoreCase);
    }

    private void EndBurst()
    {
        List<GameCodeChange> changes;
        lock (gate)
        {
            if (disposed || pendingChanges.Count == 0)
                return;
            changes = [.. pendingChanges];
            pendingChanges.Clear();
        }
        Changed?.Invoke(this, new GameCodeChangedEventArgs(changes));
    }

    private sealed class WatchedProject
    {
        private readonly Lock contentGate = new();
        private string? content;

        public WatchedProject(string projectPath, bool isCode)
        {
            ProjectPath = projectPath;
            IsCode = isCode;
            Directory = Path.GetDirectoryName(projectPath) + Path.DirectorySeparatorChar;
            if (isCode)
                content = TryReadContent();
        }

        public string ProjectPath { get; }

        /// <summary>
        /// The folder of the project, with a trailing separator.
        /// </summary>
        public string Directory { get; }

        public bool IsCode { get; }

        /// <summary>
        /// Whether the project file has a new content since the last call: one save raises several events, a file being written is
        /// empty, and a project file saved as it was (the editor saves its projects with the session) has nothing new.
        /// </summary>
        public bool HasNewContent(FileEventChangeType changeType)
        {
            var newContent = changeType == FileEventChangeType.Deleted ? null : TryReadContent();
            lock (contentGate)
            {
                if (newContent == string.Empty || newContent == content)
                    return false;
                content = newContent;
                return true;
            }
        }

        private string? TryReadContent()
        {
            try
            {
                return File.ReadAllText(ProjectPath);
            }
            catch (IOException)
            {
                // Being written: a later event brings its content
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }
    }
}

/// <summary>
/// A burst of changes of <see cref="GameCodeWatcher"/>.
/// </summary>
public sealed class GameCodeChangedEventArgs : EventArgs
{
    public GameCodeChangedEventArgs(IReadOnlyList<GameCodeChange> changes)
    {
        Changes = changes;
    }

    /// <summary>
    /// The changed files, each once.
    /// </summary>
    public IReadOnlyList<GameCodeChange> Changes { get; }
}

/// <summary>
/// A changed file of the code of a project.
/// </summary>
/// <param name="ProjectPath">The project file of the project the file belongs to.</param>
/// <param name="FilePath">The full path of the changed file: a source file, or the project file itself.</param>
public sealed record GameCodeChange(string ProjectPath, string FilePath);

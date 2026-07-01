// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using NuGet.Common;
using Stride.Assets.Presentation.AssetEditors.ScriptEditor;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Project = Microsoft.CodeAnalysis.Project;

namespace Stride.Assets.Presentation.AssetEditors
{
    public enum AssemblyChangeType
    {
        /// <summary>The compiled assembly binary changed.</summary>
        Binary,
        /// <summary>The .csproj changed: reload the MSBuild project and Roslyn workspace.</summary>
        Project,
        /// <summary>A .cs file was added/removed/renamed: reconcile the asset tree, no MSBuild/workspace reload.</summary>
        ProjectAssets,
        /// <summary>A .cs file's content changed: reload its document text only.</summary>
        Source,
    }

    public class AssemblyChangedEvent
    {
        public AssemblyChangedEvent(PackageLoadedAssembly assembly, AssemblyChangeType changeType, string changedFile, Project project)
        {
            Assembly = assembly;
            ChangeType = changeType;
            ChangedFile = changedFile;
            Project = project;
        }

        /// <summary>The kind of change that occurred.</summary>
        public AssemblyChangeType ChangeType { get; set; }

        /// <summary>The tracked assembly the change applies to.</summary>
        public PackageLoadedAssembly Assembly { get; set; }

        /// <summary>The full path of the file that changed.</summary>
        public string ChangedFile { get; set; }

        /// <summary>The Roslyn project the change applies to.</summary>
        public Project Project { get; set; }

        /// <summary>For a .cs change, the underlying file change type.</summary>
        public FileEventChangeType SourceChangeType { get; set; }

        /// <summary>For a renamed .cs file, the previous full path.</summary>
        public string OldChangedFile { get; set; }
    }

    public class ProjectWatcher : IDisposable
    {
        private readonly LoggerResult logger;
        private readonly TrackingCollection<TrackedAssembly> trackedAssemblies;
        // Guards trackedAssemblies mutations/enumerations: mutated from package changes, enumerated on the dataflow thread.
        private readonly object trackedAssembliesLock = new object();
        private readonly BufferBlock<FileEvent> fileChanged = new BufferBlock<FileEvent>();
        private readonly IDisposable fileChangedLink1;
        private readonly IDisposable fileChangedLink2;
        private IDisposable assemblyChangedLink;
        private readonly DirectoryWatcher directoryWatcher;
        private readonly SessionViewModel session;
        private readonly bool trackBinaries;
        private TaskCompletionSource<bool> initializedTaskSource;
        private Project gameExecutable;

        private CancellationTokenSource batchChangesCancellationTokenSource = new CancellationTokenSource();
        // Source/project changes for ALL tracked projects (feeds the Roslyn workspace via CodeViewModel).
        public IAsyncEnumerable<List<AssemblyChangedEvent>> SourceChange;
        // Changes for editor-loaded projects only (non-null Assembly); feeds the assembly-reload path.
        public IAsyncEnumerable<List<AssemblyChangedEvent>> AssemblyChange;

        private MSBuildWorkspace msbuildWorkspace;
        private bool solutionOpened;
        private readonly SemaphoreSlim solutionLock = new SemaphoreSlim(1, 1);

        private Lazy<Task<RoslynHost>> roslynHost;

        private RoslynHost CreateRoslynHost()
        {
            try
            {
                return new RoslynHost();
            }
            catch (Exception e)
            {
                logger.Error($"Could not create {nameof(RoslynHost)}", e);
                throw;
            }
        }

        public ProjectWatcher(SessionViewModel session, LoggerResult logger, bool trackBinaries = true)
        {
            trackedAssemblies = new TrackingCollection<TrackedAssembly>();

            this.trackBinaries = trackBinaries;
            this.session = session;
            this.logger = logger;

            roslynHost = AsyncLazy.New(CreateRoslynHost);

            session.LocalPackages.CollectionChanged += LocalPackagesChanged;

            directoryWatcher = new DirectoryWatcher();
            directoryWatcher.Modified += directoryWatcher_Modified;

            var fileChangedTransform = new TransformBlock<FileEvent, AssemblyChangedEvent>(x => FileChangeTransformation(x));
            fileChangedLink1 = fileChanged.LinkTo(fileChangedTransform);
            fileChangedLink2 = fileChangedTransform.LinkTo(SourceChangedBroadcast);
            // Editor-loaded projects only reach the assembly-reload path; unloaded heads never do.
            assemblyChangedLink = SourceChangedBroadcast.LinkTo(AssemblyChangedBroadcast, e => e?.Assembly != null);

            SourceChange = BatchChanges(SourceChangedBroadcast, loadedOnly: false);
            AssemblyChange = BatchChanges(AssemblyChangedBroadcast, loadedOnly: true);
        }

        private async IAsyncEnumerable<List<AssemblyChangedEvent>> BatchChanges(ISourceBlock<AssemblyChangedEvent> source, bool loadedOnly)
        {
            var buffer = new BufferBlock<AssemblyChangedEvent>();
            using (source.LinkTo(buffer))
            {
                while (!batchChangesCancellationTokenSource.IsCancellationRequested)
                {
                    var hasChanged = false;
                    var assemblyChanges = new List<AssemblyChangedEvent>();
                    do
                    {
                        var assemblyChange = await buffer.ReceiveAsync(batchChangesCancellationTokenSource.Token);

                        if (assemblyChange == null)
                            continue;

                        assemblyChanges.Add(assemblyChange);
                        var project = assemblyChange.Project;
                        // Only binary/project structural changes fan out to dependent assemblies;
                        // source-content and asset-membership changes are local to their own project.
                        var referencedProjects = assemblyChange.ChangeType is AssemblyChangeType.Binary or AssemblyChangeType.Project
                            ? msbuildWorkspace.CurrentSolution.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(project.Id)
                            : Enumerable.Empty<ProjectId>();
                        var trackedSnapshot = SnapshotTrackedAssemblies();
                        foreach (var referenceProject in referencedProjects)
                        {
                            var foundProject = msbuildWorkspace.CurrentSolution.GetProject(referenceProject);
                            if (foundProject is null)
                                continue;
                            var assemblyName = foundProject.AssemblyName;
                            var target = trackedSnapshot.FirstOrDefault(x => x.Project.AssemblyName == assemblyName);
                            if (target != null)
                            {
                                // On the assembly-reload path, skip dependents the editor doesn't load (no assembly to reload).
                                if (loadedOnly && target.LoadedAssembly == null)
                                    continue;
                                string file = assemblyChange.ChangedFile;
                                if (assemblyChange.ChangeType == AssemblyChangeType.Binary)
                                {
                                    // The executable is tracked without a LoadedAssembly; it has no binary path to report.
                                    if (target.LoadedAssembly == null)
                                        continue;
                                    file = target.LoadedAssembly.Path;
                                }
                                else if (assemblyChange.ChangeType == AssemblyChangeType.Project)
                                {
                                    file = target.Project.FilePath;
                                }
                                assemblyChanges.Add(new AssemblyChangedEvent(target.LoadedAssembly, assemblyChange.ChangeType, file, target.Project));
                            }
                        }

                        if (!hasChanged)
                        {
                            // After the first change, wait for more changes
                            await Task.Delay(TimeSpan.FromMilliseconds(500), batchChangesCancellationTokenSource.Token);
                            hasChanged = true;
                        }

                    } while (!hasChanged || buffer.Count > 0);

                    // Merge files that were modified multiple time
                    assemblyChanges = assemblyChanges.GroupBy(x => x.ChangedFile).Select(x => x.Last()).ToList();

                    yield return assemblyChanges;
                }
            }
        }
        // All tracked-project changes (fed by the file-change transform).
        public BroadcastBlock<AssemblyChangedEvent> SourceChangedBroadcast { get; } = new BroadcastBlock<AssemblyChangedEvent>(null);

        // Editor-loaded projects only (non-null Assembly); a filtered view of SourceChangedBroadcast.
        public BroadcastBlock<AssemblyChangedEvent> AssemblyChangedBroadcast { get; } = new BroadcastBlock<AssemblyChangedEvent>(null);

        public Project CurrentGameLibrary
        {
            get
            {
                var project = session.CurrentProject as ProjectViewModel;
                if (project == null || project.Type != ProjectType.Library)
                    return null;

                return SnapshotTrackedAssemblies().FirstOrDefault(x => new UFile(x.Project.FilePath) == project.ProjectPath)?.Project;
            }
        }

        public Task<RoslynHost> RoslynHost => roslynHost.Value;

        public Project CurrentGameExecutable => gameExecutable;

        /// <summary>All projects currently loaded in the shared MSBuild solution (any csproj, not just tracked libraries).</summary>
        public IEnumerable<Project> GetLoadedProjects() => msbuildWorkspace?.CurrentSolution.Projects ?? Enumerable.Empty<Project>();

        public TrackingCollection<TrackedAssembly> TrackedAssemblies => trackedAssemblies;

        /// <summary>Thread-safe snapshot of the tracked assemblies for enumeration off the mutating thread.</summary>
        public List<TrackedAssembly> SnapshotTrackedAssemblies()
        {
            lock (trackedAssembliesLock)
                return trackedAssemblies.ToList();
        }

        public void Dispose()
        {
            batchChangesCancellationTokenSource.Cancel();

            session.LocalPackages.CollectionChanged -= LocalPackagesChanged;
            directoryWatcher.Dispose();
            fileChangedLink1.Dispose();
            fileChangedLink2.Dispose();
            assemblyChangedLink?.Dispose();
        }

        public async Task Initialize()
        {
            if (initializedTaskSource == null)
            {
                initializedTaskSource = new TaskCompletionSource<bool>();

                // Snapshot: TrackPackage awaits, and concurrent additions invalidate the enumerator.
                foreach (var package in session.LocalPackages.ToList())
                    await TrackPackage(package);

                // Locate the current package's game executable and track it, so external changes to its
                // scripts sync too (the executable isn't tracked as a library assembly).
                // TODO: Handle current package changes. Detect this as part of the package solution.
                var gameExecutableViewModel = session.CurrentProject as ProjectViewModel;
                if (gameExecutableViewModel?.Type == ProjectType.Executable && gameExecutableViewModel.IsLoaded)
                {
                    gameExecutable = await OpenProject(gameExecutableViewModel.ProjectPath);
                    if (gameExecutable != null)
                    {
                        directoryWatcher.Track(gameExecutableViewModel.ProjectPath);
                        TrackProjectDocuments(gameExecutable, gameExecutableViewModel.PackagePath.GetFullDirectory());
                        lock (trackedAssembliesLock)
                        {
                            if (!trackedAssemblies.Any(x => string.Equals(x.Project?.FilePath, gameExecutable.FilePath, StringComparison.OrdinalIgnoreCase)))
                                trackedAssemblies.Add(new TrackedAssembly { Package = gameExecutableViewModel, Project = gameExecutable });
                        }
                    }
                }

                initializedTaskSource.SetResult(true);
            }
            else
            {
                await initializedTaskSource.Task;
            }
        }

        public async Task ReceiveAndDiscardChanges(TimeSpan batchInterval, CancellationToken cancellationToken)
        {
            var hasChanged = false;
            var buffer = new BufferBlock<AssemblyChangedEvent>();
            using (SourceChangedBroadcast.LinkTo(buffer))
            {
                do
                {
                    var assemblyChange = await buffer.ReceiveAsync(cancellationToken);

                    if (assemblyChange != null && !hasChanged)
                    {
                        // After the first change, wait for more changes
                        await Task.Delay(batchInterval, cancellationToken);
                        hasChanged = true;
                    }

                } while (!hasChanged || fileChanged.Count > 0);
            }
        }

        private async Task<AssemblyChangedEvent> FileChangeTransformation(FileEvent e)
        {
            var renameEvent = e as FileRenameEvent;
            // For a rename, the old path is what identifies the existing document/tracked assembly.
            var changedFile = renameEvent?.OldFullPath ?? e.FullPath;

            foreach (var trackedAssembly in SnapshotTrackedAssemblies())
            {
                // Report change of the assembly binary (the executable is tracked without a LoadedAssembly).
                if (trackedAssembly.LoadedAssembly != null && string.Equals(trackedAssembly.LoadedAssembly.Path, changedFile, StringComparison.OrdinalIgnoreCase))
                    return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Binary, changedFile, trackedAssembly.Project);

                // The .csproj itself changed: reload the MSBuild project and Roslyn workspace.
                if (string.Equals(trackedAssembly.Project.FilePath, changedFile, StringComparison.OrdinalIgnoreCase))
                {
                    await UpdateProject(trackedAssembly, forceReload: true);
                    return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Project, trackedAssembly.Project.FilePath, trackedAssembly.Project);
                }

                var directoryName = Path.GetDirectoryName(trackedAssembly.Project.FilePath) + Path.DirectorySeparatorChar;
                var changedFileDirectoryName = Path.GetDirectoryName(changedFile) + Path.DirectorySeparatorChar;

                // Only handle .cs files under this project's directory (DefaultItems auto import *.cs).
                // TODO: Check actual unevaluated .csproj to get the auto includes/excludes?
                if (Path.GetExtension(changedFile)?.ToLowerInvariant() != ".cs"
                    || !changedFileDirectoryName.StartsWith(directoryName, StringComparison.OrdinalIgnoreCase))
                    continue;

                switch (e.ChangeType)
                {
                    // File membership changed: reconcile the asset tree (no MSBuild/workspace reload).
                    case FileEventChangeType.Created:
                    case FileEventChangeType.Deleted:
                    case FileEventChangeType.Renamed:
                        return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.ProjectAssets, e.FullPath, trackedAssembly.Project)
                        {
                            SourceChangeType = e.ChangeType,
                            OldChangedFile = renameEvent?.OldFullPath,
                        };

                    // Content changed: reload just this document's text.
                    case FileEventChangeType.Changed:
                        return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Source, changedFile, trackedAssembly.Project)
                        {
                            SourceChangeType = FileEventChangeType.Changed,
                        };
                }
            }

            return null;
        }

        private async void LocalPackagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // async void event handler: an unhandled throw would crash the process, so guard the body.
            try
            {
                if (e.OldItems != null)
                {
                    foreach (PackageViewModel oldItem in e.OldItems)
                        UntrackPackage(oldItem);
                }

                if (e.NewItems != null)
                {
                    foreach (PackageViewModel newItem in e.NewItems)
                        await TrackPackage(newItem);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to track local package changes", ex);
            }
        }

        private void UntrackPackage(PackageViewModel package)
        {
            // TODO: Properly untrack all files
            lock (trackedAssembliesLock)
                trackedAssemblies.RemoveWhere(trackedAssembly => trackedAssembly.Package == package);
        }

        private async Task TrackPackage(PackageViewModel package)
        {
            if (!package.IsLoaded)
                return;

            foreach (var loadedAssembly in package.LoadedAssemblies)
            {
                // Track the assembly binary
                if (trackBinaries)
                    directoryWatcher.Track(loadedAssembly.Path);

                // Track the project file
                if (loadedAssembly.ProjectReference == null)
                    continue;

                directoryWatcher.Track(loadedAssembly.ProjectReference.Location);

                var trackedAssembly = new TrackedAssembly { Package = package, LoadedAssembly = loadedAssembly };

                // Track project source code
                if (await UpdateProject(trackedAssembly))
                {
                    lock (trackedAssembliesLock)
                        trackedAssemblies.Add(trackedAssembly);
                }
            }

            // TODO: Detect changes to loaded assemblies?
        }

        // TODO: Properly untrack removed documents
        private async Task<bool> UpdateProject(TrackedAssembly trackedAssembly, bool forceReload = false)
        {
            UFile location;
            if (trackedAssembly.Project?.FilePath is { } existingPath)
            {
                // Reload from the known project path (also covers projects without a LoadedAssembly, e.g. the executable).
                location = new UFile(existingPath);
            }
            else
            {
                location = trackedAssembly.LoadedAssembly.ProjectReference.Location;
                if (location.IsRelative)
                {
                    location = UPath.Combine(trackedAssembly.Package.PackagePath.GetFullDirectory(), location);
                }
            }

            var project = await OpenProject(location, forceReload);
            if (project == null)
                return false;

            trackedAssembly.Project = project;
            TrackProjectDocuments(project, trackedAssembly.Package.PackagePath.GetFullDirectory());
            return true;
        }

        /// <summary>Tracks the source documents of a project (limited to the package/project folders) for change notifications.</summary>
        private void TrackProjectDocuments(Project project, UDirectory packageDirectory)
        {
            var projectDirectory = new UFile(project.FilePath).GetFullDirectory();
            foreach (var document in project.Documents)
            {
                // Limit ourselves to our package subfolders or project folders
                if (!packageDirectory.Contains(new UFile(document.FilePath))
                    && !projectDirectory.Contains(new UFile(document.FilePath)))
                    continue;

                directoryWatcher.Track(document.FilePath);
            }
        }

        private async Task<Project> OpenProject(UFile projectPath, bool forceReload = false)
        {
            var solution = await EnsureSolutionOpened(forceReload);
            var osPath = projectPath.ToOSPath();
            // Path match is case-insensitive; a multi-targeted project yields one entry per TFM (any works here).
            var project = solution.Projects.FirstOrDefault(x => string.Equals(x.FilePath, osPath, StringComparison.OrdinalIgnoreCase));
            if (project == null)
                logger.Warning($"[ScriptWorkspace] Could not load project '{osPath}' into the script workspace.");
            return project;
        }

        /// <summary>
        /// Opens the solution into the shared <see cref="msbuildWorkspace"/> once (or reloads it on <paramref name="forceReload"/>).
        /// </summary>
        private async Task<Solution> EnsureSolutionOpened(bool forceReload)
        {
            // Serialize workspace init/reopen: concurrent callers (package changes, file changes, init)
            // must not race on the non-thread-safe MSBuildWorkspace.
            await solutionLock.WaitAsync();
            try
            {
                if (msbuildWorkspace == null)
                {
                    var host = await RoslynHost;
                    msbuildWorkspace = MSBuildWorkspace.Create(ImmutableDictionary<string, string>.Empty, host.HostServices);
                }

                if (!solutionOpened || forceReload)
                {
                    await msbuildWorkspace.OpenSolutionAsync(session.SolutionPath.ToOSPath());
                    solutionOpened = true;

                    // Surface design-time build issues instead of swallowing them (empty-document symptoms).
                    foreach (var diagnostic in msbuildWorkspace.Diagnostics)
                    {
                        if (diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                            logger.Warning($"[ScriptWorkspace] {diagnostic.Message}");
                        else
                            logger.Verbose($"[ScriptWorkspace] {diagnostic.Message}");
                    }

                    // Reopening mints new project ids, so rebind existing snapshots to the fresh solution.
                    foreach (var tracked in SnapshotTrackedAssemblies())
                    {
                        var refreshed = msbuildWorkspace.CurrentSolution.Projects.FirstOrDefault(x => string.Equals(x.FilePath, tracked.Project?.FilePath, StringComparison.OrdinalIgnoreCase));
                        if (refreshed != null)
                            tracked.Project = refreshed;
                    }
                }

                return msbuildWorkspace.CurrentSolution;
            }
            finally
            {
                solutionLock.Release();
            }
        }

        private void directoryWatcher_Modified(object sender, FileEvent e)
        {
            fileChanged.Post(e);
        }

        public class TrackedAssembly
        {
            public PackageViewModel Package { get; set; }

            public PackageLoadedAssembly LoadedAssembly { get; set; }

            public Project Project { get; set; }
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.IO;

namespace Stride.GameStudio.Avalonia.Debugging;

internal sealed partial class ProjectWatcher : IDisposable
{
    private readonly CancellationTokenSource batchChangesCancellationTokenSource = new();
    private readonly Task batchChangesTask;
    private readonly DirectoryWatcher directoryWatcher;
    private readonly BufferBlock<FileEvent> fileChanged = new();
    private readonly IDisposable fileChangedLink1;
    private readonly IDisposable fileChangedLink2;
    private Project? gameExecutable;
    private TaskCompletionSource<bool>? initializedTaskSource;
    private MSBuildWorkspace? msbuildWorkspace;
    private readonly ISessionViewModel session;
    private readonly bool trackBinaries;

    public ProjectWatcher(ISessionViewModel session, bool trackBinaries)
    {
        this.session = session;
        this.session.LocalPackages.CollectionChanged += LocalPackagesChanged;
        this.trackBinaries = trackBinaries;

        directoryWatcher = new DirectoryWatcher();
        directoryWatcher.Modified += DirectoryWatcherModified;

        var fileChangedTransform = new TransformBlock<FileEvent, AssemblyChangedEvent?>(x => FileChangeTransformation(x));
        fileChangedLink1 = fileChanged.LinkTo(fileChangedTransform);
        fileChangedLink2 = fileChangedTransform.LinkTo(AssemblyChangedBroadcast);

        batchChangesTask = BatchChanges();
    }

    public BroadcastBlock<AssemblyChangedEvent?> AssemblyChangedBroadcast { get; } = new(null);
    public BroadcastBlock<List<AssemblyChangedEvent>> AssembliesChangedBroadcast { get; } = new(null);

    public Project? CurrentGameLibrary
    {
        get
        {
            if (session.CurrentProject is not ProjectViewModel project || project.Type != ProjectType.Library)
                return null;

            return TrackedAssemblies.FirstOrDefault(x => new UFile(x.Project?.FilePath) == project.ProjectPath)?.Project;
        }
    }

    public Project? CurrentGameExecutable => gameExecutable;

    internal TrackingCollection<TrackedAssembly> TrackedAssemblies { get; } = new();

    internal async Task Initialize()
    {
        if (initializedTaskSource == null)
        {
            initializedTaskSource = new TaskCompletionSource<bool>();

            // Track all packages
            foreach (var package in session.LocalPackages)
                await TrackPackage(package);

            // Locate current package's game executable
            // TODO: Handle current package changes. Detect this as part of the package solution.
            var gameExecutableViewModel = session.CurrentProject?.Type == ProjectType.Executable ? session.CurrentProject : null;
            if (gameExecutableViewModel is not null && gameExecutableViewModel.IsLoaded)
                gameExecutable = await OpenProject(gameExecutableViewModel.ProjectPath);

            initializedTaskSource.SetResult(true);
        }
        else
        {
            await initializedTaskSource.Task;
        }
    }

    internal async Task ReceiveAndDiscardChanges(TimeSpan batchInterval, CancellationToken token)
    {
        var hasChanged = false;
        var buffer = new BufferBlock<AssemblyChangedEvent?>();
        using (AssemblyChangedBroadcast.LinkTo(buffer))
        {
            do
            {
                var assemblyChange = await buffer.ReceiveAsync(token);
                if (assemblyChange is not null && !hasChanged)
                {
                    // After the first change, wait for more changes
                    await Task.Delay(batchInterval, token);
                    hasChanged = true;
                }

            } while (!hasChanged || fileChanged.Count > 0);
        }
    }

    private async Task BatchChanges()
    {
        var buffer = new BufferBlock<AssemblyChangedEvent?>();
        using (AssemblyChangedBroadcast.LinkTo(buffer))
        {
            while (true)
            {
                var hasChanged = false;
                var assemblyChanges = new List<AssemblyChangedEvent>();
                do
                {
                    var assemblyChange = await buffer.ReceiveAsync(batchChangesCancellationTokenSource.Token);
                    if (assemblyChange != null)
                        assemblyChanges.Add(assemblyChange);

                    if (assemblyChange != null && !hasChanged)
                    {
                        // After the first change, wait for more changes
                        await Task.Delay(TimeSpan.FromMilliseconds(500), batchChangesCancellationTokenSource.Token);
                        hasChanged = true;
                    }

                } while (!hasChanged || buffer.Count > 0);

                // Merge files that were modified multiple time
                assemblyChanges = assemblyChanges.GroupBy(x => x.ChangedFile).Select(x => x.Last()).ToList();

                AssembliesChangedBroadcast.Post(assemblyChanges);
            }
        }
    }

    private void DirectoryWatcherModified(object? sender, FileEvent e)
    {
        fileChanged.Post(e);
    }

    private async Task<AssemblyChangedEvent?> FileChangeTransformation(FileEvent e)
    {
        string changedFile;
        var renameEvent = e as FileRenameEvent;
        changedFile = renameEvent?.OldFullPath ?? e.FullPath;

        foreach (var trackedAssembly in TrackedAssemblies)
        {
            // Report change of the assembly binary
            if (string.Equals(trackedAssembly.LoadedAssembly.Path, changedFile, StringComparison.OrdinalIgnoreCase))
                return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Binary, changedFile, trackedAssembly.Project);

            var needProjectReload = string.Equals(trackedAssembly.Project.FilePath, changedFile, StringComparison.OrdinalIgnoreCase);

            // Also check for .cs file changes (DefaultItems auto import *.cs, with some excludes such as obj subfolder)
            // TODO: Check actual unevaluated .csproj to get the auto includes/excludes?
            if (needProjectReload == false
                && ((e.ChangeType == FileEventChangeType.Deleted || e.ChangeType == FileEventChangeType.Renamed || e.ChangeType == FileEventChangeType.Created)
                && Path.GetExtension(changedFile)?.ToLowerInvariant() == ".cs"
                && changedFile.StartsWith(Path.GetDirectoryName(trackedAssembly.Project.FilePath), StringComparison.OrdinalIgnoreCase)))
            {
                needProjectReload = true;
            }

            // Reparse the project file and report source changes
            if (needProjectReload)
            {
                // Reparse the project file and report source changes
                await UpdateProject(trackedAssembly);
                return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Project, trackedAssembly.Project.FilePath, trackedAssembly.Project);
            }

            // Only deal with file changes
            if (e.ChangeType != FileEventChangeType.Changed)
                continue;

            // Check if we have a matching document
            var document = trackedAssembly.Project.Documents.FirstOrDefault(x => string.Equals(x.FilePath, changedFile, StringComparison.OrdinalIgnoreCase));
            if (document == null)
                continue;

            string source = null;
            // Try multiple times
            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    using (var streamReader = new StreamReader(File.Open(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8, true))
                    {
                        source = streamReader.ReadToEnd();
                    }
                    break;
                }
                catch (IOException)
                {
                }
                await Task.Delay(1);
            }

            if (source == null)
            {
                // Something went wrong reading the file
                return null;
            }

            // Remove and readd new source
            trackedAssembly.Project = trackedAssembly.Project.RemoveDocument(document.Id);
            var documentId = DocumentId.CreateNewId(trackedAssembly.Project.Id);
            trackedAssembly.Project = trackedAssembly.Project.Solution.AddDocument(documentId, document.Name, SourceText.From(source, Encoding.UTF8), null, document.FilePath).GetDocument(documentId).Project;

            return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Source, changedFile, trackedAssembly.Project);
        }

        return null;
    }

    private async void LocalPackagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

    private async Task<Project> OpenProject(UFile projectPath)
    {
        if (msbuildWorkspace == null)
        {
            var host = await roslynHost.Value;
            msbuildWorkspace = MSBuildWorkspace.Create(ImmutableDictionary<string, string>.Empty, host.HostServices);
        }

        msbuildWorkspace.CloseSolution();

        // Try up to 10 times (1 second)
        const int retryCount = 10;
        for (var i = retryCount - 1; i >= 0; --i)
        {
            try
            {
                var project = await msbuildWorkspace.OpenProjectAsync(projectPath.ToWindowsPath());

                if (msbuildWorkspace.Diagnostics.Count > 0)
                {
                    // There was an issue compiling the project
                    // at the moment there's no mechanism to surface those errors to the UI, so leaving this in here:
                    //if (Debugger.IsAttached) Debugger.Break();
                    foreach (var diagnostic in msbuildWorkspace.Diagnostics)
                        Debug.WriteLine(diagnostic.Message, category: nameof(ProjectWatcher));
                }
                return project;
            }
            catch (IOException)
            {
                // FIle might still be locked, let's wait little bit more
                await Task.Delay(100);

                if (i == 0)
                    throw;
            }
        }

        // Unreachable
        throw new InvalidOperationException();
    }

    private async Task TrackPackage(PackageViewModel package)
    {
        if (!package.IsLoaded)
            return;

        foreach (var loadedAssembly in package.Package.LoadedAssemblies)
        {
            // Track the assembly binary
            if (trackBinaries)
                directoryWatcher.Track(loadedAssembly.Path);

            // Track the project file
            if (loadedAssembly.ProjectReference is null)
                continue;

            directoryWatcher.Track(loadedAssembly.ProjectReference.Location);

            var trackedAssembly = new TrackedAssembly { Package = package, LoadedAssembly = loadedAssembly };

            // Track project source code
            if (await UpdateProject(trackedAssembly))
                TrackedAssemblies.Add(trackedAssembly);
        }

        // TODO: Detect changes to loaded assemblies?
    }

    private void UntrackPackage(PackageViewModel package)
    {
        // TODO: Properly untrack all files
        TrackedAssemblies.RemoveWhere(trackedAssembly => trackedAssembly.Package == package);
    }

    // TODO: Properly untrack removed documents
    private async Task<bool> UpdateProject(TrackedAssembly trackedAssembly)
    {
        var location = trackedAssembly.LoadedAssembly!.ProjectReference.Location;
        if (location.IsRelative)
        {
            location = UPath.Combine(trackedAssembly.Package!.PackagePath.GetFullDirectory(), location);
        }
        var project = await OpenProject(location);
        if (project is null)
            return false;

        trackedAssembly.Project = project;

        var packageDirectory = trackedAssembly.Package!.PackagePath.GetFullDirectory();
        var projectDirectory = location.GetFullDirectory();

        foreach (var document in project.Documents)
        {
            // Limit ourselves to our package subfolders or project folders
            if (!packageDirectory.Contains(new UFile(document.FilePath)) &&
                !projectDirectory.Contains(new UFile(document.FilePath)))
                continue;

            directoryWatcher.Track(document.FilePath);
        }

        return true;
    }

    void IDisposable.Dispose()
    {
        batchChangesCancellationTokenSource.Cancel();
        batchChangesTask.Wait();

        directoryWatcher.Dispose();
        fileChangedLink1.Dispose();
        fileChangedLink2.Dispose();

        msbuildWorkspace?.Dispose();
    }

    internal enum AssemblyChangeType
    {
        Binary,
        Project,
        Source,
    }

    internal sealed class AssemblyChangedEvent
    {
        public AssemblyChangedEvent(PackageLoadedAssembly assembly, AssemblyChangeType changeType, string changedFile, Project project)
        {
            Assembly = assembly;
            ChangeType = changeType;
            ChangedFile = changedFile;
            Project = project;
        }

        public AssemblyChangeType ChangeType { get; }

        public PackageLoadedAssembly Assembly { get; }

        public string ChangedFile { get; }

        public Project Project { get; }
    }

    internal sealed class TrackedAssembly
    {
        public PackageViewModel? Package { get; set; }

        public PackageLoadedAssembly? LoadedAssembly { get; set; }

        public Project? Project { get; set; }
    }
}

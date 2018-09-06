// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.IO;
using Microsoft.CodeAnalysis.MSBuild;
using Xenko.Core.Collections;
using Xenko.Core.Extensions;
using Xenko.Assets.Presentation.AssetEditors.ScriptEditor;
using Project = Microsoft.CodeAnalysis.Project;

namespace Xenko.Assets.Presentation.AssetEditors
{
    public enum AssemblyChangeType
    {
        Binary,
        Project,
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

        public AssemblyChangeType ChangeType { get; set; }

        public PackageLoadedAssembly Assembly { get; set; }

        public string ChangedFile { get; set; }

        public Project Project { get; set; }
    }
    
    public class ProjectWatcher : IDisposable
    {
        private readonly TrackingCollection<TrackedAssembly> trackedAssemblies;
        private readonly BufferBlock<FileEvent> fileChanged = new BufferBlock<FileEvent>();
        private readonly IDisposable fileChangedLink1;
        private readonly IDisposable fileChangedLink2;
        private readonly DirectoryWatcher directoryWatcher;
        private readonly SessionViewModel session;
        private readonly bool trackBinaries;
        private TaskCompletionSource<bool> initializedTaskSource;
        private Project gameExecutable;

        private CancellationTokenSource batchChangesCancellationTokenSource = new CancellationTokenSource();
        private Task batchChangesTask;

        private MSBuildWorkspace msbuildWorkspace;

        private Lazy<Task<RoslynHost>> roslynHost = new Lazy<Task<RoslynHost>>(() => Task.Factory.StartNew(() => new RoslynHost()));

        public ProjectWatcher(SessionViewModel session, bool trackBinaries = true)
        {
            trackedAssemblies = new TrackingCollection<TrackedAssembly>();

            this.trackBinaries = trackBinaries;
            this.session = session;
            session.LocalPackages.CollectionChanged += LocalPackagesChanged;

            directoryWatcher = new DirectoryWatcher();
            directoryWatcher.Modified += directoryWatcher_Modified;

            var fileChangedTransform = new TransformBlock<FileEvent, AssemblyChangedEvent>(x => FileChangeTransformation(x));
            fileChangedLink1 = fileChanged.LinkTo(fileChangedTransform);
            fileChangedLink2 = fileChangedTransform.LinkTo(AssemblyChangedBroadcast);

            batchChangesTask = BatchChanges();
        }

        private async Task BatchChanges()
        {
            var buffer = new BufferBlock<AssemblyChangedEvent>();
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

        public BroadcastBlock<AssemblyChangedEvent> AssemblyChangedBroadcast { get; } = new BroadcastBlock<AssemblyChangedEvent>(null);
        public BroadcastBlock<List<AssemblyChangedEvent>> AssembliesChangedBroadcast { get; } = new BroadcastBlock<List<AssemblyChangedEvent>>(null);

        public Project CurrentGameLibrary
        {
            get
            {
                var project = session.CurrentProject as ProjectViewModel;
                if (project == null || project.Type != ProjectType.Library)
                    return null;

                return TrackedAssemblies.FirstOrDefault(x => new UFile(x.Project.FilePath) == project.ProjectPath)?.Project;
            }
        }

        public Task<RoslynHost> RoslynHost => roslynHost.Value;

        public Project CurrentGameExecutable => gameExecutable;

        public TrackingCollection<TrackedAssembly> TrackedAssemblies => trackedAssemblies;

        public void Dispose()
        {
            batchChangesCancellationTokenSource.Cancel();
            batchChangesTask.Wait();

            directoryWatcher.Dispose();
            fileChangedLink1.Dispose();
            fileChangedLink2.Dispose();
        }

        public async Task Initialize()
        {
            if (initializedTaskSource == null)
            {
                initializedTaskSource = new TaskCompletionSource<bool>();

                // Track all packages
                foreach (var package in session.LocalPackages)
                    await TrackPackage(package);

                // Locate current package's game executable
                // TODO: Handle current package changes. Detect this as part of the package solution.
                var gameExecutableViewModel = (session.CurrentProject as ProjectViewModel)?.Type == ProjectType.Executable ? session.CurrentProject : null;
                if (gameExecutableViewModel != null && gameExecutableViewModel.IsLoaded)
                    gameExecutable = await OpenProject(gameExecutableViewModel.ProjectPath);

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
            using (AssemblyChangedBroadcast.LinkTo(buffer))
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

        private  async Task<AssemblyChangedEvent> FileChangeTransformation(FileEvent e)
        {
            string changedFile;
            var renameEvent = e as FileRenameEvent;
            changedFile = renameEvent?.OldFullPath ?? e.FullPath;

            foreach (var trackedAssembly in trackedAssemblies)
            {
                // Report change of the assembly binary
                if (string.Equals(trackedAssembly.LoadedAssembly.Path, changedFile, StringComparison.OrdinalIgnoreCase))
                    return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Binary, changedFile, trackedAssembly.Project);

                var needProjectReload = string.Equals(trackedAssembly.Project.FilePath, changedFile, StringComparison.OrdinalIgnoreCase);

                // Also check for .cs file changes (DefaultItems auto import *.cs, with some excludes such as obj subfolder)
                // TODO: Check actual unevaluated .csproj to get the auto includes/excludes?
                if (needProjectReload == false
                    && (e.ChangeType == FileEventChangeType.Deleted || e.ChangeType == FileEventChangeType.Renamed || e.ChangeType == FileEventChangeType.Created)
                    && Path.GetExtension(changedFile)?.ToLowerInvariant() == ".cs"
                    && !UPath.Combine(new UFile(trackedAssembly.Project.FilePath).GetFullDirectory(), new UDirectory("obj")).Contains(new UFile(changedFile)))
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

        private async void LocalPackagesChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        private void UntrackPackage(PackageViewModel package)
        {
            // TODO: Properly untrack all files
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
                directoryWatcher.Track(loadedAssembly.ProjectReference.Location);

                var trackedAssembly = new TrackedAssembly { Package = package, LoadedAssembly = loadedAssembly };
                
                // Track project source code
                if (await UpdateProject(trackedAssembly))
                    trackedAssemblies.Add(trackedAssembly);
            }

            // TODO: Detect changes to loaded assemblies?
        }

        // TODO: Properly untrack removed documents
        private async Task<bool> UpdateProject(TrackedAssembly trackedAssembly)
        {
            var location = trackedAssembly.LoadedAssembly.ProjectReference.Location;
            if (location.IsRelative)
            {
                location = UPath.Combine(trackedAssembly.Package.PackagePath.GetFullDirectory(), location);
            }
            var project = await OpenProject(location);
            if (project == null)
                return false;

            trackedAssembly.Project = project;

            var packageDirectory = trackedAssembly.Package.PackagePath.GetFullDirectory();
            var projectDirectory = location.GetFullDirectory();

            foreach (var document in project.Documents)
            {
                // Limit ourselves to our package subfolders or project folders
                if (!packageDirectory.Contains(new UFile(document.FilePath))
                    && !projectDirectory.Contains(new UFile(document.FilePath)))
                    continue;

                directoryWatcher.Track(document.FilePath);
            }

            return true;
        }

        private async Task<Project> OpenProject(UFile projectPath)
        {
            if (msbuildWorkspace == null)
            {
                // Only load workspace for C# assemblies (default includes VB but not added as a NuGet package)
                //var csharpWorkspaceAssemblies = new[] { Assembly.Load("Microsoft.CodeAnalysis.Workspaces"), Assembly.Load("Microsoft.CodeAnalysis.CSharp.Workspaces"), Assembly.Load("Microsoft.CodeAnalysis.Workspaces.Desktop") };
                var host = await RoslynHost;
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

                    // Change the default CSharp language version to match the supported version for a specific visual studio version or MSBuild version
                    //  this is because roslyn  will always resolve Default to Latest which might not match the 
                    //  latest version supported by the build tools installed on the machine
                    var csharpParseOptions = project.ParseOptions as CSharpParseOptions;
                    if (csharpParseOptions != null)
                    {
                        if (csharpParseOptions.SpecifiedLanguageVersion == LanguageVersion.Default || csharpParseOptions.SpecifiedLanguageVersion == LanguageVersion.Latest)
                        {
                            LanguageVersion targetLanguageVersion = csharpParseOptions.SpecifiedLanguageVersion;

                            // Check the visual studio version inside the solution first, which is what visual studio uses to decide which version to open
                            //  this should not be confused with the toolsVersion below, since this is the MSBuild version (they might be different)
                            Version visualStudioVersion = session.CurrentProject?.Package.Session.VisualStudioVersion;
                            if (visualStudioVersion != null)
                            {
                                if (visualStudioVersion.Major <= 14)
                                {
                                    targetLanguageVersion = LanguageVersion.CSharp6;
                                }

                            }
                            else 
                            {
                                // Fallback to checking the tools version on the csproj 
                                //  this happens when you open an xkpkg instead of a sln file as a project
                                ProjectRootElement xml = ProjectRootElement.Open(projectPath);
                                Version toolsVersion;
                                if (Version.TryParse(xml.ToolsVersion, out toolsVersion))
                                {
                                    if (toolsVersion.Major <= 14)
                                    {
                                        targetLanguageVersion = LanguageVersion.CSharp6;
                                    }
                                }
                            }
                            project = project.WithParseOptions(csharpParseOptions.WithLanguageVersion(targetLanguageVersion));
                        }
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

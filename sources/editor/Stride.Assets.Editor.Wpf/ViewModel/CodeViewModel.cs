// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Stride.Assets.Presentation.AssetEditors;
using Stride.Assets.Effect;
using Stride.Assets.Presentation.AssetEditors.ScriptEditor;
using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;
using Stride.Core.Translation;
using Stride.Editor.Build;
using RoslynWorkspace = Stride.Assets.Presentation.AssetEditors.ScriptEditor.RoslynWorkspace;
using Stride.Assets.Editor.Build;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;

namespace Stride.Assets.Presentation.ViewModel
{
    /// <summary>
    /// Manages source code project and files, including change tracking, Roslyn workspace updates, etc...
    /// </summary>
    public class CodeViewModel : DispatcherViewModel, IDisposable
    {
        public LoggerResult Logger { get; } = new("Project");

        /// <summary>
        /// The editor minimum font size.
        /// </summary>
        public const int MinimumEditorFontSize = 8;

        /// <summary>
        /// The editor maximum font size.
        /// </summary>
        public const int MaximumEditorFontSize = 72;

        private readonly TaskCompletionSource<ProjectWatcher> projectWatcherCompletion = new();
        private readonly TaskCompletionSource<RoslynWorkspace> workspaceCompletion = new();
        private ProjectWatcher projectWatcher;
        private RoslynWorkspace roslynWorkspace;
        private int editorFontSize = ScriptEditorSettings.FontSize.GetValue(); // default size
        private bool userShaderBuildRequested;
        // The source and shader reconciles change the same assets, and each awaits in the middle
        private readonly SemaphoreSlim reconcileLock = new(1, 1);
        private readonly Dictionary<string, ObjectId> builtShaderHashes = new(StringComparer.OrdinalIgnoreCase);

        private readonly Brush keywordBrush;
        private readonly Brush typeBrush;

        public CodeViewModel(StrideAssetsViewModel strideAssetsViewModel)
            : base(strideAssetsViewModel.SafeArgument(nameof(strideAssetsViewModel)).ServiceProvider)
        {
            _ = InitializeAsync(strideAssetsViewModel);

            // Apply syntax highlighting for tooltips
            keywordBrush = new SolidColorBrush(ClassificationHighlightColorsDark.KeywordColor);
            typeBrush = new SolidColorBrush(ClassificationHighlightColorsDark.TypeColor);
            keywordBrush.Freeze();
            typeBrush.Freeze();

            // TODO: Update with latest RoslynPad
            //SymbolDisplayPartExtensions.StyleRunFromSymbolDisplayPartKind = StyleRunFromSymbolDisplayPartKind;
            //SymbolDisplayPartExtensions.StyleRunFromTextTag = StyleRunFromTextTag;
        }

        private async Task InitializeAsync(StrideAssetsViewModel strideAssetsViewModel)
        {
            try
            {
                var watcher = new ProjectWatcher(strideAssetsViewModel.Session, Logger);
                projectWatcher = watcher;
                await watcher.Initialize();
                var workspace = (await watcher.RoslynHost).Workspace;
                roslynWorkspace = workspace;

                // Add every project loaded in the solution (library packages, executable heads, any csproj
                // package) so scripts in any of them get a workspace project — required for diagnostics and
                // to avoid opening empty / freezing on delete when the project isn't in the workspace.
                foreach (var project in watcher.GetLoadedProjects())
                    workspace.AddOrUpdateProject(project);

                watcher.TrackedAssemblies.CollectionChanged += TrackedAssembliesCollectionChanged;

                _ = UpdateProjects(watcher, workspace, strideAssetsViewModel);
                _ = UpdateShaderFiles(watcher, strideAssetsViewModel.Session);

                projectWatcherCompletion.SetResult(watcher);
                workspaceCompletion.SetResult(workspace);
            }
            catch (Exception e)
            {
                // Surface the failure to consumers instead of leaving them awaiting forever.
                Logger.Error("Failed to initialize the script editor workspace", e);
                projectWatcherCompletion.TrySetException(e);
                workspaceCompletion.TrySetException(e);
            }
        }

        private void TrackedAssembliesCollectionChanged(object sender, Core.Collections.TrackingCollectionChangedEventArgs e)
        {
            if (roslynWorkspace == null)
                return;

            if (((ProjectWatcher.TrackedAssembly)e.Item).Project is { } project)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        roslynWorkspace.AddOrUpdateProject(project);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        roslynWorkspace.RemoveProject(project.Id);
                        break;
                }
            }
        }

        private async Task UpdateProjects(ProjectWatcher projectWatcher, RoslynWorkspace workspace, StrideAssetsViewModel strideAssetsViewModel)
        {
            var session = strideAssetsViewModel.Session;
            try
            {
                await foreach (var events in projectWatcher.SourceChange)
                {
                    // InvokeTask (not InvokeAsync) so the batch is actually awaited: no reentrancy, exceptions observed.
                    await InvokeReconcile(async () =>
                    {
                        // Structural changes first: .csproj reloads and .cs add/remove/rename asset reconciliation.
                        foreach (var e in events.Where(x => x.ChangeType is AssemblyChangeType.Project or AssemblyChangeType.ProjectAssets))
                        {
                            if (e.Project == null)
                                continue;
                            try
                            {
                                await ReloadProject(session, e.Project, reloadWorkspace: e.ChangeType == AssemblyChangeType.Project);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Failed to reload project for '{e.ChangedFile}'", ex);
                            }
                        }

                        // Then document text reloads.
                        foreach (var e in events.Where(x => x.ChangeType == AssemblyChangeType.Source))
                        {
                            try
                            {
                                var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(e.ChangedFile).FirstOrDefault();
                                if (documentId != null)
                                    await workspace.HostDocumentTextLoaderChanged(documentId, new FileTextLoader(e.ChangedFile, Encoding.UTF8));
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Failed to reload document '{e.ChangedFile}'", ex);
                            }
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // The project watcher was disposed; stop listening for changes.
            }
        }

        private async Task InvokeReconcile(Func<Task> reconcile)
        {
            await reconcileLock.WaitAsync();
            try
            {
                await Dispatcher.InvokeTask(reconcile);
            }
            finally
            {
                reconcileLock.Release();
            }
        }

        private async Task UpdateShaderFiles(ProjectWatcher projectWatcher, SessionViewModel session)
        {
            try
            {
                await foreach (var events in projectWatcher.ShaderChange)
                {
                    try
                    {
                        var touchedPaths = events.Select(x => x.ChangedFile).ToList();
                        await InvokeReconcile(async () =>
                        {
                            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
                            await Task.WhenAll(undoRedoService.TransactionCompletion, undoRedoService.UndoRedoCompletion, session.SaveCompletion);
                            await ReconcileShaderFiles(session, projectWatcher, touchedPaths);
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to update the shader assets", ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The project watcher was disposed; stop listening for changes.
            }
        }

        /// <summary>
        /// Brings the shader assets in line with the files on the disk, after file events on the given paths.
        /// </summary>
        private async Task ReconcileShaderFiles(SessionViewModel session, ProjectWatcher projectWatcher, List<string> touchedPaths)
        {
            var needsBuild = false;
            var trackedProjects = projectWatcher.SnapshotTrackedAssemblies()
                .Where(x => x.Package is ProjectViewModel)
                .DistinctBy(x => x.Project.FilePath, StringComparer.OrdinalIgnoreCase);
            foreach (var tracked in trackedProjects)
            {
                var projectDirectory = new UFile(tracked.Project.FilePath).GetFullDirectory();
                var projectPaths = touchedPaths.Where(x => ProjectShaderChanges.IsUnder(x, projectDirectory)).ToList();
                if (projectPaths.Count > 0)
                    needsBuild |= await ReconcileProjectShaderFiles((ProjectViewModel)tracked.Package, tracked.Project, projectDirectory, projectPaths);
            }

            if (needsBuild)
                RequestUserShaderBuild(session);
        }

        /// <returns>True if the user shaders need a build.</returns>
        private async Task<bool> ReconcileProjectShaderFiles(ProjectViewModel projectViewModel, Project project, UDirectory projectDirectory, List<string> touchedPaths)
        {
            var projectFiles = new List<(UFile FilePath, UFile Link)>();
            if (ProjectShaderChanges.NeedsProjectItems(touchedPaths, GetShaderAssets(projectViewModel).Select(x => x.AssetItem.FullPath.FullPath), File.Exists, Directory.Exists))
                projectFiles = await Task.Run(() => Package.FindAssetsInProject(project.FilePath, out _));

            List<ProjectShaderChange> Plan()
            {
                var deletedInSession = projectFiles
                    .Where(x => FindDeletedAsset(projectViewModel, x, projectDirectory) is { IsDeletedOutside: false })
                    .Select(x => x.FilePath.FullPath);
                return ProjectShaderChanges.Plan(touchedPaths, GetShaderAssets(projectViewModel).Select(x => x.AssetItem.FullPath.FullPath), projectFiles.Select(x => x.FilePath.FullPath), deletedInSession, File.Exists);
            }

            // A tool that saves through temporary files makes a file disappear for a moment
            if (Plan().Any(x => x.Action == ProjectShaderAction.Delete))
                await Task.Delay(TimeSpan.FromMilliseconds(500));

            await WaitUntilWritten(Plan().Where(x => x.Action != ProjectShaderAction.Delete && ProjectShaderChanges.IsBuiltFile(x.FilePath)).Select(x => x.FilePath));

            var changes = Plan();
            var assets = GetShaderAssets(projectViewModel);
            var needsBuild = false;

            var filesToAdd = new List<(UFile FilePath, UFile Link)>();
            foreach (var change in changes.Where(x => x.Action == ProjectShaderAction.Add))
            {
                var file = projectFiles.First(x => x.FilePath.FullPath == change.FilePath);
                if (FindDeletedAsset(projectViewModel, file, projectDirectory) is { } deletedAsset)
                {
                    RestoreDeletedOutside(projectViewModel, deletedAsset);
                    needsBuild |= ReloadText(deletedAsset, change.FilePath);
                }
                else
                {
                    filesToAdd.Add(file);
                    needsBuild |= ProjectShaderChanges.IsBuiltFile(change.FilePath);
                }
            }
            AddNewProjectAssets(projectViewModel, assets, project, filesToAdd);

            foreach (var change in changes.Where(x => x.Action == ProjectShaderAction.ContentChanged))
                needsBuild |= ReloadText(assets.First(x => x.AssetItem.FullPath.FullPath == change.FilePath), change.FilePath);

            var assetsToDelete = changes.Where(x => x.Action == ProjectShaderAction.Delete)
                .Select(x => assets.First(y => y.AssetItem.FullPath.FullPath == x.FilePath))
                .ToList();
            if (await DeleteProjectAssets(projectViewModel, assetsToDelete, project))
                needsBuild |= assetsToDelete.Any(x => ProjectShaderChanges.IsBuiltFile(x.AssetItem.FullPath.FullPath));

            return needsBuild;
        }

        private static List<AssetViewModel> GetShaderAssets(ProjectViewModel projectViewModel)
        {
            var assets = new List<AssetViewModel>();
            GetAssets(projectViewModel.Code, assets);
            assets.RemoveAll(x => !ProjectShaderChanges.IsShaderFile(x.AssetItem.FullPath.FullPath));
            return assets;
        }

        private static AssetViewModel FindDeletedAsset(ProjectViewModel projectViewModel, (UFile FilePath, UFile Link) file, UDirectory projectDirectory)
        {
            var location = projectViewModel.Package.GetProjectAssetLocation(file.FilePath, file.Link, projectDirectory);
            return projectViewModel.DeletedAssetsSinceLastSave.FirstOrDefault(x => x.AssetItem.Location == location);
        }

        /// <summary>
        /// Brings back the asset of a file that was deleted outside Game Studio and came back before a save. The asset
        /// follows the disk, so this is not a step of the undo stack.
        /// </summary>
        private static void RestoreDeletedOutside(ProjectViewModel projectViewModel, AssetViewModel asset)
        {
            asset.RestoreProjectAsset(projectViewModel.GetOrCreateProjectDirectory(projectViewModel.GetDisplayDirectory(asset.AssetItem.Location), false));
        }

        /// <summary>
        /// Reads the text of a source code asset from its file again.
        /// </summary>
        /// <returns>True if the file is built with the user shaders and differs from what the last build took, or can't be read.</returns>
        private bool ReloadText(AssetViewModel asset, string filePath)
        {
            var content = TryReadFile(filePath);
            if (content == null)
            {
                Logger.Warning($"Could not read the file '{filePath}'");
                return ProjectShaderChanges.IsBuiltFile(filePath);
            }

            string text;
            using (var reader = new StreamReader(new MemoryStream(content)))
                text = reader.ReadToEnd();
            var sourceCode = (SourceCodeAsset)asset.Asset;
            if (sourceCode.Text != text)
                sourceCode.Text = text;

            return ProjectShaderChanges.IsBuiltFile(filePath)
                && !(builtShaderHashes.TryGetValue(filePath, out var builtHash) && builtHash == ObjectId.FromBytes(content));
        }

        private static byte[] TryReadFile(string filePath)
        {
            try
            {
                using var stream = new FileStream(new UFile(filePath).ToOSPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var content = new MemoryStream();
                stream.CopyTo(content);
                return content.ToArray();
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gives a tool that still writes the files a moment to end. A later write raises a later event.
        /// </summary>
        private static async Task WaitUntilWritten(IEnumerable<string> filePaths)
        {
            var remaining = filePaths.ToList();
            for (var attempt = 0; attempt < 5 && remaining.Count > 0; ++attempt)
            {
                remaining.RemoveAll(x => !IsLocked(x));
                if (remaining.Count > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        private static bool IsLocked(string filePath)
        {
            try
            {
                using var stream = new FileStream(new UFile(filePath).ToOSPath(), FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the project watcher which tracks source code changes on the disk; it is created asychronously.
        /// </summary>
        public Task<ProjectWatcher> ProjectWatcher => projectWatcherCompletion.Task;

        /// <summary>
        /// Gets the roslyn workspace; it is created asynchronously.
        /// </summary>
        public Task<RoslynWorkspace> Workspace => workspaceCompletion.Task;

        /// <summary>
        /// The editor current font size. It will be saved in the settings.
        /// </summary>
        public int EditorFontSize
        {
            get { return editorFontSize; }
            set
            {
                if (value < MinimumEditorFontSize || value > MaximumEditorFontSize) return;

                if (SetValue(ref editorFontSize, value))
                {
                    ScriptEditorSettings.FontSize.SetValue(editorFontSize);
                    ScriptEditorSettings.Save();
                }
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(CodeViewModel));
            Cleanup();
            base.Destroy();
        }

        /// <summary>
        /// Requests a build of the user shaders. All the requests of one dispatcher step share one build.
        /// </summary>
        public void RequestUserShaderBuild(SessionViewModel session)
        {
            if (userShaderBuildRequested)
                return;

            userShaderBuildRequested = true;
            Dispatcher.InvokeAsync(() =>
            {
                userShaderBuildRequested = false;
                try
                {
                    BuildUserShaders(session);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to start the build of the user shaders", e);
                }
            }).Forget();
        }

        private void BuildUserShaders(SessionViewModel session)
        {
            // The build service is registered during plugin initialization, so it is not available yet when a save
            // runs while opening the session (e.g. persisting an upgrade); it compiles the shaders of all packages when it starts.
            var builder = session.ServiceProvider.TryGet<AssetBuilderService>();
            if (builder == null)
                return;

            // What this build takes: a file event with the same content has nothing new to build
            builtShaderHashes.Clear();
            foreach (var shader in session.AllPackages.Where(x => !x.Package.IsReadOnly).SelectMany(x => x.Assets).Where(x => x.Asset is EffectShaderAsset))
            {
                var filePath = shader.AssetItem.FullPath.FullPath;
                if (TryReadFile(filePath) is { } content)
                    builtShaderHashes[filePath] = ObjectId.FromBytes(content);
            }

            var shaderBuildSteps = new StrideShaderImporter().CreateUserShaderBuildSteps(session);
            if (shaderBuildSteps != null)
                builder.PushBuildUnit(new PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier.Default, shaderBuildSteps, true));
        }

        /// <summary>
        /// Reloads a project when a .csproj files changes on the hard drive.
        /// </summary>
        /// <remarks>
        /// In case of destructive changes (i.e. dirty files that disappeared), user confirmation will be asked to proceed.
        /// </remarks>
        private async Task ReloadProject(SessionViewModel session, Project project, bool reloadWorkspace = true)
        {
            var workspace = await Workspace;

            // Get assets and namespace from csproj
            // TODO: Use roslyn list of files? not sure we could have non .cs files easily though
            // However, if we load from file, it might not be in sync with Roslyn state
            string projectNamespace;
            var projectFiles = Package.FindAssetsInProject(project.FilePath, out projectNamespace);

            // Find associated ProjectViewModel
            var projectViewModel = session.LocalPackages.FirstOrDefault(y => y.Name == project.Name) as ProjectViewModel;
            if (projectViewModel == null)
                return;

            // List current assets
            var projectAssets = new List<AssetViewModel>();
            var isProjectDirty = GetAssets(projectViewModel.Code, projectAssets);

            // Project is dirty, ask user if he really wants to auto-reload
            if (isProjectDirty)
            {
                var dialogResult = projectViewModel.ServiceProvider.Get<IDialogService2>().BlockingMessageBox(
                    string.Format(
                        Tr._p("Message", "Game Studio can't auto-reload the project file {0} because you have local changes such as new or deleted scripts.\r\n\r\nClick OK to keep reloading or Cancel to keep the current version."),
                        Path.GetFileName(project.FilePath)), MessageBoxButton.OKCancel);
                if (dialogResult == MessageBoxResult.Cancel)
                    return;
            }

            // Remove deleted assets (and ask user if he really wants to proceed in case some of them were dirty?)
            bool continueReload = await DeleteRemovedProjectAssets(projectViewModel, projectAssets, project, projectFiles);
            if (!continueReload)
                return;

            // Update Roslyn project (only when the .csproj itself changed; for .cs add/remove the
            // asset view models add/remove their own documents, so no full workspace rebuild is needed).
            if (reloadWorkspace)
                workspace.AddOrUpdateProject(project);

            // Add new assets
            AddNewProjectAssets(projectViewModel, projectAssets, project, projectFiles);

            // Mark project as non dirty
            // TODO: Does that work properly with Undo/Redo?
            UpdateDirtiness(projectViewModel.Code, false);
        }

        /// <summary>
        /// // Handle Script asset deletion (from Visual Studio/HDD external changes to Game Studio).
        /// </summary>
        /// <returns>False if user refused to continue (in case deleted assets were dirty).</returns>
        private static async Task<bool> DeleteRemovedProjectAssets(ProjectViewModel projectViewModel, List<AssetViewModel> projectAssets, Project project, List<(UFile FilePath, UFile Link)> projectFiles)
        {
            // List IProjectAsset
            var currentProjectAssets = projectAssets.Where(x => x.AssetItem.Asset is IProjectAsset);

            var assetsToDelete = new List<AssetViewModel>();
            foreach (var asset in currentProjectAssets)
            {
                // Note: if file doesn't exist on HDD anymore (i.e. automatic csproj tracking for *.cs), no need to delete it anymore
                bool isDeleted = !projectFiles.Any(x => x.FilePath == asset.AssetItem.FullPath);
                if (isDeleted)
                {
                    assetsToDelete.Add(asset);
                }
            }

            return await DeleteProjectAssets(projectViewModel, assetsToDelete, project);
        }

        /// <summary>
        /// Deletes project assets whose file is gone.
        /// </summary>
        /// <returns>False if user refused to continue (in case deleted assets were dirty).</returns>
        private static async Task<bool> DeleteProjectAssets(ProjectViewModel projectViewModel, List<AssetViewModel> assetsToDelete, Project project)
        {
            var dirtyAssetsToDelete = assetsToDelete.Where(x => x.AssetItem.IsDirty).ToList();
            if (dirtyAssetsToDelete.Count > 0)
            {
                // Check if user is OK with deleting those dirty assets?
                var dialogResult = projectViewModel.ServiceProvider.Get<IDialogService2>().BlockingMessageBox(
                    string.Format(
                        Tr._p("Message", "The following source files in the {0} project have been deleted externally, but have unsaved changes in Game Studio. Do you want to delete these files?\r\n\r\n{1}"),
                       Path.GetFileName(project.FilePath), string.Join("\r\n", dirtyAssetsToDelete.Select(x => x.AssetItem.FullPath.ToOSPath()))),
                    MessageBoxButton.OKCancel);
                if (dialogResult == MessageBoxResult.Cancel)
                    return false;
            }

            // delete this asset
            // TODO: this action (it should occur only during assembly releoad) will be undoable (undoing reload restores deleted script)
            return assetsToDelete.Count == 0 || await projectViewModel.Session.ActiveAssetView.DeleteContent(assetsToDelete, true, deletedOutside: true);
        }

        /// <summary>
        /// Handles project asset addition (from Visual Studio/HDD external changes to Game Studio).
        /// </summary>
        private void AddNewProjectAssets(ProjectViewModel projectViewModel, List<AssetViewModel> projectAssets, Project project, List<(UFile FilePath, UFile Link)> projectFiles)
        {
            // Nothing to add?
            if (projectFiles.Count == 0)
                return;

            var scriptAssets = projectAssets.Where(x => x.AssetItem.Asset is IProjectAsset).Select(x => x.AssetItem);

            var documentsToIgnore = (from scriptAsset in scriptAssets
                                     from document in projectFiles
                                     let ufileDoc = document.FilePath
                                     where ufileDoc == scriptAsset.FullPath
                                     select document).ToList();

            //remove what we have already
            var documentsCopy = new List<(UFile FilePath, UFile Link)>(projectFiles);
            foreach (var document in documentsToIgnore)
            {
                documentsCopy.Remove(document);
            }

            //add what we are missing
            if (documentsCopy.Count > 0)
            {
                var newScriptAssets = new List<AssetViewModel>();
                foreach (var document in documentsCopy)
                {
                    var docFile = new UFile(document.FilePath);

                    if (projectViewModel.DeletedAssetsSinceLastSave.FirstOrDefault(x => x.IsDeletedOutside && x.AssetItem.FullPath == docFile) is { } deletedOutside)
                    {
                        RestoreDeletedOutside(projectViewModel, deletedOutside);
                        ReloadText(deletedOutside, docFile.FullPath);
                        continue;
                    }

                    AssetItem assetItem;
                    if (string.Equals(docFile.GetFileExtension(), ScriptSourceFileAsset.Extension, StringComparison.OrdinalIgnoreCase))
                    {
                        var assetName = docFile.MakeRelative(projectViewModel.Package.RootDirectory).GetDirectoryAndFileNameWithoutExtension();

                        var asset = new ScriptSourceFileAsset();
                        assetItem = new AssetItem(assetName, asset)
                        {
                            IsDirty = true, //todo review / this is actually very important in the case of renaming, to propagate the change from VS to Game Studio, if we set it false here, during renaming the renamed asset won't be removed
                            SourceFolder = projectViewModel.Package.RootDirectory,
                        };
                    }
                    else
                    {
                        // A shader or a visual script: the same asset as when the project opens, and its file already holds its content
                        assetItem = projectViewModel.Package.LoadProjectAsset(Logger, docFile, document.Link, new UFile(project.FilePath).GetFullDirectory());
                        if (assetItem == null)
                            continue;
                    }

                    var directory = projectViewModel.GetOrCreateProjectDirectory(projectViewModel.GetDisplayDirectory(assetItem.Location), false);
                    var newScriptAsset = projectViewModel.CreateAsset(directory, assetItem, false, null);
                    newScriptAssets.Add(newScriptAsset);
                }

                // We're out of any transaction in this context so we have to manually notify that new assets were created.
                projectViewModel.Session.NotifyAssetPropertiesChanged(newScriptAssets);
            }
        }

        /// <summary>
        /// Enumerate assets.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="list"></param>
        /// <returns>Returns true if any of the (sub)directories is dirty.</returns>
        private static bool GetAssets(DirectoryBaseViewModel dir, List<AssetViewModel> list)
        {
            bool dirDirty = dir.IsDirty;

            foreach (var directory in dir.SubDirectories)
            {
                dirDirty |= GetAssets(directory, list);
            }

            //is dirty check is necessary to avoid unsaved scripts to be deleted
            list.AddRange(dir.Assets.Where(x => x.Asset is IProjectAsset));

            return dirDirty;
        }

        private void UpdateDirtiness(DirectoryBaseViewModel dir, bool isDirty)
        {
            if (dir.IsDirty)
                ((IDirtiable)dir).UpdateDirtiness(isDirty);

            foreach (var subdir in dir.SubDirectories)
            {
                UpdateDirtiness(subdir, isDirty);
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (projectWatcher != null)
            {
                projectWatcher.TrackedAssemblies.CollectionChanged -= TrackedAssembliesCollectionChanged;
                // Cancels BatchChanges (ends the UpdateProjects loop) and disposes the directory watcher / dataflow links.
                projectWatcher.Dispose();
                projectWatcher = null;
            }
        }

        private void StyleRunFromSymbolDisplayPartKind(SymbolDisplayPartKind partKind, Run run)
        {
            switch (partKind)
            {
                case SymbolDisplayPartKind.Keyword:
                    run.Foreground = keywordBrush;
                    return;
                case SymbolDisplayPartKind.StructName:
                case SymbolDisplayPartKind.EnumName:
                case SymbolDisplayPartKind.TypeParameterName:
                case SymbolDisplayPartKind.ClassName:
                case SymbolDisplayPartKind.DelegateName:
                case SymbolDisplayPartKind.InterfaceName:
                    run.Foreground = typeBrush;
                    return;
            }
        }

        private void StyleRunFromTextTag(string textTag, Run run)
        {
            switch (textTag)
            {
                case TextTags.Keyword:
                    run.Foreground = keywordBrush;
                    break;
                case TextTags.Struct:
                case TextTags.Enum:
                case TextTags.TypeParameter:
                case TextTags.Class:
                case TextTags.Delegate:
                case TextTags.Interface:
                    run.Foreground = typeBrush;
                    break;
            }
        }
    }
}

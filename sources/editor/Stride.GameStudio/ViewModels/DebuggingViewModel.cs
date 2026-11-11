// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.GameStudio.Logs;
using Stride.GameStudio.Debugging;
using Stride.GameStudio.Deployment;
using Stride.GameStudio.Helpers;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssemblyReloading;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Core.Translation;
using Stride.Assets.Presentation.AssetEditors;
using Stride.Core.Presentation.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.GameStudio.ViewModels
{
    /// <summary>
    /// The build and run of Game Studio: the shared <see cref="BuildAndRunViewModel"/> (Build project, Start project, Cancel build, the
    /// build log), with the deploy steps of Game Studio, plus live scripting, the reload of the game assemblies in place, and the
    /// logs of the Output pane.
    /// </summary>
    public class DebuggingViewModel : DispatcherViewModel, IDisposable
    {
        private readonly IDebugService debugService;
        private readonly GameStudioViewModel editor;
        private readonly Dictionary<PackageLoadedAssembly, ModifiedAssembly> modifiedAssemblies;
        // Projects whose editor loadability (StrideContainsAssetTypes) flipped; null LoadedAssembly = to load, otherwise to unload.
        private readonly Dictionary<PackageViewModel, ModifiedAssembly> modifiedLoadabilities = new Dictionary<PackageViewModel, ModifiedAssembly>();
        private ProjectWatcher projectWatcher;
        private readonly EntityComponentSourceCodeResolver entityComponentsSorter;
        private readonly CancellationTokenSource assemblyTrackingCancellation;
        private readonly LoggerResult assemblyReloadLogger = new LoggerResult();
        private bool assemblyChangesPending;
        // A failed build keeps its changes pending: the next change asks again.
        private bool askOnNextChange;
        private bool trackAssemblyChanges;
        private string outputTitle;
        private readonly string outputTitleBase = Tr._p("Title", "Output");
        // The builds of the reload of the game assemblies
        private ICancellableAsyncBuild currentBuild;
        private bool reloadBuildInProgress;
        private bool cleanedUp;

        public DebuggingViewModel(GameStudioViewModel editor, IDebugService debugService)
            : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
        {
            this.editor = editor;
            this.debugService = debugService;

            outputTitle = outputTitleBase;

            Build = new BuildAndRunViewModel(ServiceProvider, editor.Status) { Session = editor.Session };
            GameDeployers.Register(Build, ServiceProvider);
            Build.PropertyChanged += BuildPropertyChanged;
            // Any new message of the build log marks the title of the Output pane, until it is activated
            Build.BuildLog.Messages.CollectionChanged += BuildLogMessagesChanged;
            ProjectLog = new LoggerViewModel(ServiceProvider);
            var logger = StrideAssetsViewModel.Instance.Code.Logger;
            ProjectLog.AddLoggerWithPast(logger);
            LiveScriptingLog = new LoggerViewModel(ServiceProvider);
            LiveScriptingLog.AddLogger(assemblyReloadLogger);
            EditorLog = new LoggerViewModel(ServiceProvider);
            var editorLogger = new LoggerResult("Editor");
            EditorLog.AddLogger(editorLogger);
            GlobalLogBuffer.Instance.ForwardTo(editorLogger);
            LivePlayProjectCommand = new AnonymousTaskCommand(ServiceProvider, LivePlayProject);
            ReloadAssembliesCommand = new AnonymousTaskCommand(ServiceProvider, ReloadAssemblies) { IsEnabled = false };
            ResetOutputTitleCommand = new AnonymousCommand(ServiceProvider, () => OutputTitle = outputTitleBase);
            modifiedAssemblies = new Dictionary<PackageLoadedAssembly, ModifiedAssembly>();
            trackAssemblyChanges = true;

            assemblyTrackingCancellation = new CancellationTokenSource();

            // Create script resolver
            entityComponentsSorter = new EntityComponentSourceCodeResolver();
            ServiceProvider.RegisterService(entityComponentsSorter);

            assemblyReloadLogger.MessageLogged += (sender, e) => Dispatcher.InvokeAsync(() => OutputTitle = outputTitleBase + '*');
            editor.Session.PropertyChanged += SessionPropertyChanged;
            UpdateCommands();

            Task.Run(async () =>
            {
                var watcher = await editor.StrideAssets.Code.ProjectWatcher;
                await entityComponentsSorter.Initialize(editor.Session, watcher, assemblyTrackingCancellation.Token);
                PullAssemblyChanges(watcher);
            });
        }

        /// <summary>
        /// Gets the current session.
        /// </summary>
        public SessionViewModel Session => editor.Session;

        /// <summary>
        /// Gets the build and run of the current project: its commands and the build log.
        /// </summary>
        [NotNull]
        public BuildAndRunViewModel Build { get; }

        /// <summary>
        /// Gets the project log.
        /// </summary>
        [NotNull]
        public LoggerViewModel ProjectLog { get; }


        /// <summary>
        /// Gets the live-scripting log.
        /// </summary>
        [NotNull]
        public LoggerViewModel LiveScriptingLog { get; }

        /// <summary>
        /// Gets the editor log: the warnings and errors of the global log (preview, plugins, editors...).
        /// </summary>
        [NotNull]
        public LoggerViewModel EditorLog { get; }

        /// <summary>
        /// Gets the title of the output pane, including an asterisk if new logs have arrived.
        /// </summary>
        // We need to manage dirtiness of the output title directly in the string because the rad pane requires a single string as title
        public string OutputTitle { get => outputTitle; private set => SetValue(ref outputTitle, value); }

        /// <summary>
        /// Gets whether there is a build currently in progress: a build of the build and run, or of the reload of the game assemblies.
        /// </summary>
        public bool BuildInProgress => Build.BuildInProgress || reloadBuildInProgress;

        [NotNull]
        public ICommandBase LivePlayProjectCommand { get; }

        [NotNull]
        public ICommandBase ReloadAssembliesCommand { get; }

        [NotNull]
        public ICommandBase ResetOutputTitleCommand { get; }

        /// <summary>
        /// Builds the current project and starts its game, as Start project does.
        /// </summary>
        /// <returns>Whether the game was built and started, and the identifier of its process when it started on this machine.</returns>
        internal async Task<(bool Success, int? ProcessId)> RunProjectAsync()
        {
            var result = await Build.RunAsync(true);
            return (result.Outcome == GameBuildOutcome.Succeeded, result.Game?.Id);
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(DebuggingViewModel));
            Cleanup();
            base.Destroy();
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (cleanedUp)
                return;
            cleanedUp = true;

            editor.Session.PropertyChanged -= SessionPropertyChanged;
            Build.PropertyChanged -= BuildPropertyChanged;
            Build.BuildLog.Messages.CollectionChanged -= BuildLogMessagesChanged;
            Build.Destroy();
            EditorLog.Destroy();
            debugService?.Dispose();

            trackAssemblyChanges = false;
            assemblyTrackingCancellation.Cancel();
        }

        private async void PullAssemblyChanges([NotNull] ProjectWatcher projectWatcher)
        {
            this.projectWatcher = projectWatcher;
            await foreach (var events in projectWatcher.AssemblyChange)
            {
                foreach (var assemblyChange in events)
                {
                    // A loadability flip means the project's assembly must be loaded (null Assembly) or unloaded.
                    if (assemblyChange is { ChangeType: AssemblyChangeType.Loadability, Package: not null })
                    {
                        modifiedLoadabilities[assemblyChange.Package] = new ModifiedAssembly
                        {
                            LoadedAssembly = assemblyChange.Assembly,
                            ChangeType = assemblyChange.ChangeType,
                            Project = assemblyChange.Project,
                            Package = assemblyChange.Package
                        };
                        continue;
                    }
                    // Skip changes from projects the editor doesn't load (null Assembly, e.g. executable heads):
                    // those have no loaded assembly to reload, and their source is handled by the Roslyn/CodeViewModel
                    // path instead. Skip the binary of a project too: its source change already asks for a build.
                    // A dev build of a dependency package has no project: its binary is the change.
                    if (assemblyChange?.Assembly == null || (assemblyChange.ChangeType == AssemblyChangeType.Binary && assemblyChange.Project != null))
                        continue;
                    modifiedAssemblies[assemblyChange.Assembly] = new ModifiedAssembly
                    {
                        LoadedAssembly = assemblyChange.Assembly,
                        ChangeType = assemblyChange.ChangeType,
                        Project = assemblyChange.Project
                    };
                }

                var shouldNotify = !assemblyChangesPending || askOnNextChange;
                if ((modifiedAssemblies.Count > 0 || modifiedLoadabilities.Count > 0) && shouldNotify)
                {
                    askOnNextChange = false;
                    RunNotifier(true);
                }
            }
        }

        private void RunNotifier(bool shouldNotify)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCommands();

                if (shouldNotify)
                {
                    var message = Tr._p("Message", "Some game code files have been modified. Do you want to reload the assemblies?");
                    ServiceProvider.Get<IEditorDialogService>().AddDelayedNotification(EditorSettings.AskBeforeReloadingAssemblies, message,
                        Tr._p("Button", "Reload"), Tr._p("Button", "Don't reload"),
                        yesAction: async () =>
                        {
                            var undoRedoService = ServiceProvider.Get<IUndoRedoService>();
                            // Wait for current transactions, undo/redo or save to complete before continuing.
                            await Task.WhenAll(undoRedoService.TransactionCompletion, undoRedoService.UndoRedoCompletion, Session.SaveCompletion);
                            // Reload assembly, if possible
                            if (ReloadAssembliesCommand.IsEnabled)
                                ReloadAssembliesCommand.Execute();
                        },
                        yesNoSettingsKey: EditorSettings.AutoReloadAssemblies);
                }
            });

        }

        private void UpdateCommands()
        {
            assemblyChangesPending = modifiedAssemblies.Count > 0 || modifiedLoadabilities.Count > 0;
            var hasCurrentProject = editor.Session.CurrentProject != null;

            LivePlayProjectCommand.IsEnabled = hasCurrentProject && !BuildInProgress;
            ReloadAssembliesCommand.IsEnabled = assemblyChangesPending && !BuildInProgress;
        }

        private struct ModifiedAssembly
        {
            public PackageLoadedAssembly LoadedAssembly;

            public string LoadedAssemblyPath;

            public AssemblyChangeType ChangeType;

            public Project Project;

            public PackageViewModel Package;
        }

        /// <summary>
        /// The changed assemblies plus the loaded assemblies that depend on them, in dependency order.
        /// </summary>
        /// <summary>
        /// The reload's log, shown in the Output panel: the build log, whose loggers each build replaces (the Live
        /// Scripting log it also goes to has no visible tab).
        /// </summary>
        private LoggerResult ReloadLog
        {
            get
            {
                Build.BuildLog.EnsureLogger(assemblyReloadLogger);
                return assemblyReloadLogger;
            }
        }

        /// <summary>
        /// The dependencies of the changed projects whose restored version differs from what the session loaded. A
        /// project whose csproj changed restores first, so that its build uses the dependencies it now asks for.
        /// </summary>
        private async Task<List<DependencyVersionChange>> FindDependencyVersionChanges(IEnumerable<ModifiedAssembly> modifiedAssemblies)
        {
            var changes = new List<DependencyVersionChange>();
            var projects = new HashSet<SolutionProject>();
            foreach (var modifiedAssembly in modifiedAssemblies)
            {
                var package = Session.AllPackages.FirstOrDefault(x => x.LoadedAssemblies.Contains(modifiedAssembly.LoadedAssembly))?.Package;
                if (package?.Container is not SolutionProject project || project.FullPath is null || !projects.Add(project))
                    continue;

                try
                {
                    if (modifiedAssembly.ChangeType == AssemblyChangeType.Project)
                        await VSProjectHelper.RestoreNugetPackages(ReloadLog, project.FullPath.ToOSPath());
                    changes.AddRange(package.Session.FindDependencyVersionChanges(project));
                }
                catch (Exception e)
                {
                    ReloadLog.Error($"[{project.FullPath.GetFileNameWithoutExtension()}] could not read its restored dependencies", e);
                }
            }

            // A dependency of several projects changes once
            return changes.GroupBy(x => x.Dependency.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
        }

        /// <param name="changedLibraries">Library assemblies moving to another version: the assemblies that reference them reload too.</param>
        private List<ModifiedAssembly> ExpandWithDependents(Dictionary<PackageLoadedAssembly, ModifiedAssembly> changedAssemblies, IReadOnlyCollection<System.Reflection.Assembly> changedLibraries)
        {
            if (projectWatcher == null)
                return changedAssemblies.Values.ToList();

            var result = new List<ModifiedAssembly>();
            var expanded = new HashSet<PackageLoadedAssembly>();
            foreach (var trackedAssembly in projectWatcher.GetAssembliesToReload(changedAssemblies.Keys))
            {
                expanded.Add(trackedAssembly.LoadedAssembly);
                result.Add(changedAssemblies.TryGetValue(trackedAssembly.LoadedAssembly, out var changedAssembly)
                    ? changedAssembly
                    : new ModifiedAssembly
                    {
                        LoadedAssembly = trackedAssembly.LoadedAssembly,
                        ChangeType = AssemblyChangeType.Dependency,
                        Project = trackedAssembly.Project,
                    });
            }

            // An assembly the watcher doesn't track (a dev build of a dependency package) is reloaded too, before the others.
            foreach (var changedAssembly in changedAssemblies)
            {
                if (!expanded.Contains(changedAssembly.Key))
                    result.Insert(0, changedAssembly.Value);
            }

            // Any loaded assembly that references a reloaded one, whatever its package: its reference stays bound to the
            // copy it resolved first. Ordered dependencies first, as the references go.
            var loadedAssemblies = Session.AllPackages.SelectMany(x => x.LoadedAssemblies).Where(x => x.Assembly != null)
                .GroupBy(x => x.Assembly).ToDictionary(x => x.Key, x => x.First());
            var closure = Session.AssemblyContainer.GetDependentsClosure(result.Select(x => x.LoadedAssembly.Assembly).NotNull().Concat(changedLibraries));
            var trackedAssemblies = projectWatcher.SnapshotTrackedAssemblies();
            foreach (var assembly in closure)
            {
                if (!loadedAssemblies.TryGetValue(assembly, out var loadedAssembly) || result.Any(x => x.LoadedAssembly == loadedAssembly))
                    continue;

                // A project builds against the new copy; a package's binary loads again as it is
                result.Add(new ModifiedAssembly
                {
                    LoadedAssembly = loadedAssembly,
                    ChangeType = loadedAssembly.ProjectReference != null ? AssemblyChangeType.Dependency : AssemblyChangeType.Binary,
                    Project = trackedAssemblies.FirstOrDefault(x => x.LoadedAssembly == loadedAssembly)?.Project,
                });
            }
            var order = closure.Select((assembly, index) => (assembly, index)).ToDictionary(x => x.assembly, x => x.index);
            return result.OrderBy(x => x.LoadedAssembly.Assembly != null && order.TryGetValue(x.LoadedAssembly.Assembly, out var index) ? index : -1).ToList();
        }

        private async Task ReloadAssemblies()
        {
            trackAssemblyChanges = false;

            var modifiedAssembliesCopy = new Dictionary<PackageLoadedAssembly, ModifiedAssembly>(modifiedAssemblies);
            var assembliesToReload = new List<ModifiedAssembly>();
            modifiedAssemblies.Clear();

            // Apply loadability flips: load projects that became asset assemblies, unload ones that no longer are.
            var modifiedLoadabilitiesCopy = new Dictionary<PackageViewModel, ModifiedAssembly>(modifiedLoadabilities);
            modifiedLoadabilities.Clear();
            var loadabilityChanged = false;
            // A build clears the build log: what the flips logged is shown again after each one
            var loadabilityLog = new LoggerResult();
            Build.BuildLog.AddLogger(loadabilityLog);
            foreach (var modifiedLoadability in modifiedLoadabilitiesCopy)
            {
                var package = modifiedLoadability.Key;
                var change = modifiedLoadability.Value;
                if (change.LoadedAssembly == null)
                {
                    // The project should now be loaded: build it, then load and register it through the
                    // standard package path (gated on the updated ShouldLoadAssemblyInEditor).
                    if (package.Package.Container is not SolutionProject solutionProject)
                        continue;
                    var result = await BuildProject(solutionProject.FullPath);
                    Build.BuildLog.EnsureLoggerWithPast(loadabilityLog);
                    if (!result.IsSuccessful)
                    {
                        modifiedLoadabilities[package] = change;
                        askOnNextChange = true;
                        continue;
                    }
                    using (var transaction = Session.UndoRedoService.CreateTransaction())
                    {
                        package.Package.UpdateAssemblyReferences(loadabilityLog);
                        // Reload with no assembly swap: re-deserializes IUnloadable instances so the newly loaded types resurrect.
                        GameStudioAssemblyReloader.Reload(Session, loadabilityLog, async () =>
                        {
                            if (change.Project != null)
                                await entityComponentsSorter.AnalyzeProject(Session, change.Project, assemblyTrackingCancellation.Token);
                            UpdateCommands();
                        }, () =>
                        {
                            modifiedLoadabilities[package] = change;
                        }, new Dictionary<PackageLoadedAssembly, string>(), package.Package.LoadedAssemblies.Select(x => x.Assembly).NotNull().ToHashSet());
                        Session.AllAssets.ForEach(x => x.PropertyGraph?.RefreshBase());
                        Session.AllAssets.ForEach(x => x.PropertyGraph?.ReconcileWithBase());
                        Session.UndoRedoService.SetName(transaction, "Load game assemblies");
                    }
                }
                else
                {
                    // The project should no longer be loaded: live instances of its types become IUnloadable
                    // (preserving their Yaml), then the assembly is unloaded and unregistered.
                    using (var transaction = Session.UndoRedoService.CreateTransaction())
                    {
                        GameStudioAssemblyReloader.Unload(Session, loadabilityLog, UpdateCommands, () =>
                        {
                            modifiedLoadabilities[package] = change;
                        }, package.Package, change.LoadedAssembly);
                        Session.AllAssets.ForEach(x => x.PropertyGraph?.RefreshBase());
                        Session.AllAssets.ForEach(x => x.PropertyGraph?.ReconcileWithBase());
                        Session.UndoRedoService.SetName(transaction, "Unload game assemblies");
                    }
                    // Don't also reload what was just unloaded.
                    modifiedAssembliesCopy.Remove(change.LoadedAssembly);
                }
                loadabilityChanged = true;
                projectWatcher.RefreshLoadedAssembly(package);
            }

            // A dependency of a rebuilt project moving to another version (a PackageReference edit) loads its new version
            // with the rest: the project builds against it
            var versionChanges = await FindDependencyVersionChanges(modifiedAssembliesCopy.Values);
            var blockedChanges = versionChanges.Where(x => x.Blocker != null).ToList();
            if (blockedChanges.Count > 0)
            {
                foreach (var blockedChange in blockedChanges)
                    ReloadLog.Error($"[{blockedChange}] can't replace the loaded version in this session ({blockedChange.Blocker}): reload the session to use it. No assembly was reloaded.");
                foreach (var pending in modifiedAssembliesCopy)
                    modifiedAssemblies[pending.Key] = pending.Value;
                askOnNextChange = true;
                UpdateCommands();
                trackAssemblyChanges = true;
                return;
            }
            foreach (var versionChange in versionChanges)
            {
                foreach (var (packageAssembly, newPath) in versionChange.PackageAssemblies)
                {
                    modifiedAssembliesCopy[packageAssembly] = new ModifiedAssembly
                    {
                        LoadedAssembly = packageAssembly,
                        LoadedAssemblyPath = newPath,
                        ChangeType = AssemblyChangeType.Binary,
                    };
                }
            }
            var changedLibraries = versionChanges.SelectMany(x => x.Libraries).Select(x => x.LoadedAssembly.Assembly).ToList();

            // The whole set builds before anything reloads: a runtime reloaded without its companions (or the other way
            // around) would bind them to different copies of its types. One failed build reloads nothing, and the
            // changes stay pending.
            foreach (var modifiedAssembly in ExpandWithDependents(modifiedAssembliesCopy, changedLibraries))
            {
                var loadedAssembly = modifiedAssembly.LoadedAssembly;
                // If the assembly binary has changed, just reload (from the new version's file for a version change)
                if (modifiedAssembly.ChangeType == AssemblyChangeType.Binary)
                {
                    var assemblyToReload = modifiedAssembly;
                    assemblyToReload.LoadedAssemblyPath ??= loadedAssembly.Path;
                    assembliesToReload.Add(assemblyToReload);
                }
                else if (loadedAssembly.ProjectReference != null)
                {
                    // If source code has changed, rebuild
                    var result = await BuildProject(loadedAssembly.ProjectReference.Location);
                    if (!result.IsSuccessful)
                    {
                        ReloadLog.Error($"[{loadedAssembly.ProjectReference.Location.GetFileNameWithoutExtension()}] failed to build: no assembly was reloaded.");
                        foreach (var pending in modifiedAssembliesCopy)
                            modifiedAssemblies[pending.Key] = pending.Value;
                        askOnNextChange = true;
                        assembliesToReload.Clear();
                        versionChanges.Clear();
                        break;
                    }

                    var assemblyToReload = modifiedAssembly;
                    assemblyToReload.LoadedAssemblyPath = result.AssemblyPath;
                    assembliesToReload.Add(assemblyToReload);
                }
            }
            Build.BuildLog.EnsureLoggerWithPast(loadabilityLog);

            // The host's own copy, or a copy the host's code is bound to, stays in use: a new copy next to it would mix
            // the types of both. Only a new session uses the new build.
            var inUseByHost = assembliesToReload
                .Select(x => x.LoadedAssembly.Assembly)
                .Where(x => x != null && (System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(x) == System.Runtime.Loader.AssemblyLoadContext.Default
                                          || Core.Reflection.AssemblyContainer.IsBoundByHost(x)))
                .Select(x => x.GetName().Name)
                .ToList();
            if (inUseByHost.Count > 0)
            {
                ReloadLog.Error($"[{string.Join(", ", inUseByHost)}] in use by Game Studio itself: reload the session to use the new build. No assembly was reloaded.");
                assembliesToReload.Clear();
                versionChanges.Clear();
            }

            var userDocumentationService = Session.ServiceProvider.Get<UserDocumentationService>();
            foreach (var assemblyToReload in assembliesToReload)
            {
                if (assemblyToReload.ChangeType != AssemblyChangeType.Binary && assemblyToReload.LoadedAssembly.Assembly != null)
                    userDocumentationService.ClearCachedAssemblyDocumentation(assemblyToReload.LoadedAssembly.Assembly);
            }

            UpdateCommands();
            trackAssemblyChanges = true;

            // If any assemblies are built successfully, reload them
            if (assembliesToReload.Count > 0 || versionChanges.Count > 0)
            {
                using (var transaction = Session.UndoRedoService.CreateTransaction())
                {
                    // Before the reload, so that the reloaded assemblies resolve the new versions; undone after it
                    if (versionChanges.Count > 0)
                    {
                        foreach (var versionChange in versionChanges)
                            ReloadLog.Info($"[{versionChange}] loads the new version.");
                        void Apply() => versionChanges.ForEach(x => x.Apply());
                        void Revert() => Enumerable.Reverse(versionChanges).ForEach(x => x.Revert());
                        Apply();
                        Session.UndoRedoService.PushOperation(new Core.Presentation.Dirtiables.AnonymousDirtyingOperation([], Revert, Apply));
                    }

                    var assemblyToAnalyze = assembliesToReload.Where(x => x.LoadedAssembly?.Assembly != null && x.Project != null).ToDictionary(x => x.Project, x => x.LoadedAssembly.Assembly.FullName);
                    var logResult = new LoggerResult();
                    Build.BuildLog.AddLogger(logResult);
                    GameStudioAssemblyReloader.Reload(Session, logResult, async () =>
                    {
                        foreach (var assemblyToReload in assemblyToAnalyze)
                        {
                            await entityComponentsSorter.AnalyzeProject(Session, assemblyToReload.Key, assemblyTrackingCancellation.Token);
                        }
                        UpdateCommands();
                    }, () =>
                    {
                        foreach (var assemblyToReload in assembliesToReload)
                        {
                            if (!modifiedAssemblies.ContainsKey(assemblyToReload.LoadedAssembly))
                            {
                                var modifiedAssembly = assemblyToReload;
                                modifiedAssembly.ChangeType = AssemblyChangeType.Binary;
                                modifiedAssemblies.Add(assemblyToReload.LoadedAssembly, modifiedAssembly);
                            }
                        }
                    }, assembliesToReload.ToDictionary(x => x.LoadedAssembly, x => x.LoadedAssemblyPath));
                    Session.AllAssets.ForEach(x => x.PropertyGraph?.RefreshBase());
                    Session.AllAssets.ForEach(x => x.PropertyGraph?.ReconcileWithBase());

                    Session.UndoRedoService.SetName(transaction, "Reload game assemblies");
                }
                // Make sure we refresh the property grid so we don't reference any old type
                Session.AssetViewProperties.RefreshSelectedPropertiesAsync().Forget();
            }
            else if (loadabilityChanged)
            {
                // Make sure we refresh the property grid so we don't reference any old type
                Session.AssetViewProperties.RefreshSelectedPropertiesAsync().Forget();
            }
        }

        private async Task<bool> LivePlayProject()
        {
            LiveScriptingLog.ClearMessages();

            // Make sure it is Windows platform (only supported for now)
            if (Session.CurrentProject is { } currentProject && currentProject.Platform != PlatformType.Windows)
            {
                await ServiceProvider.Get<IDialogService>()
                    .MessageBoxAsync(
                        string.Format(Tr._p("Message", "Platform {0} isn't supported for execution."), currentProject.Platform),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // Build projects+assets (note: assets only would be enough), then start live debugging instead of the game
            var result = await Build.RunAsync(true, (project, _, _) => debugService.StartDebug(editor, project, assemblyReloadLogger));
            return result.Outcome == GameBuildOutcome.Succeeded;
        }

        private struct BuildProjectResult
        {
            public BuildProjectResult(bool isSuccessful, string assemblyPath)
            {
                IsSuccessful = isSuccessful;
                AssemblyPath = assemblyPath;
            }
            public bool IsSuccessful { get; }

            public string AssemblyPath { get; }
        }

        private async Task<BuildProjectResult> BuildProject(UFile projectPath)
        {
            Build.BuildLog.ClearMessages();
            Build.BuildLog.ClearLoggers();
            var logger = new LoggerResult();
            Build.BuildLog.AddLogger(logger);

            reloadBuildInProgress = true;
            UpdateCommands();
            var jobToken = editor.Status.NotifyBackgroundJobStarted("Build in progress…", JobPriority.Compile);

            try
            {
                var platformName = "AnyCPU";
                var configuration = "Debug";
                var target = "Build";

                var extraProperties = new Dictionary<string, string>
                {
                    ["SolutionPlatform"] = "Any CPU",
                    ["StrideBuildEngineLogVerbose"] = "true",
                };

                currentBuild = VSProjectHelper.CompileProjectAssemblyAsync(projectPath, logger, target, configuration, platformName, extraProperties, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
                if (currentBuild == null)
                {
                    logger.Error(string.Format(Tr._p("Message", "Unable to load and compile project {0}"), projectPath));
                }
                else
                {
                    await currentBuild.BuildTask;
                }
            }
            catch (Exception e)
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "An exception occurred while compiling the project: {0}"), e.FormatSummary(true)), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                editor.Status.NotifyBackgroundJobFinished(jobToken);
                editor.Status.PushDiscardableStatus(!logger.HasErrors ? "Build succeeded" : "Build failed");
                reloadBuildInProgress = false;
                UpdateCommands();
            }

            var result = new BuildProjectResult(!logger.HasErrors, currentBuild?.AssemblyPath);
            return result;
        }

        private void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SessionViewModel.CurrentProject))
            {
                UpdateCommands();
            }
        }

        private void BuildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BuildAndRunViewModel.BuildInProgress))
            {
                UpdateCommands();
            }
        }

        private void BuildLogMessagesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                OutputTitle = outputTitleBase + '*';
        }
    }
}

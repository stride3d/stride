// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Components.Status;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.GameStudio.Logs;
using Xenko.GameStudio.Debugging;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Assets.Presentation.AssemblyReloading;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Annotations;
using Xenko.Core.Translation;
using Xenko.Assets.Presentation.AssetEditors;
using Xenko.GameStudio.Services;

namespace Xenko.GameStudio
{
    public class DebuggingViewModel : DispatcherViewModel, IDisposable
    {

        private readonly IDebugService debugService;
        private readonly GameStudioViewModel editor;
        private readonly Dictionary<PackageLoadedAssembly, ModifiedAssembly> modifiedAssemblies;
        private readonly ScriptSourceCodeResolver scriptsSorter;
        private readonly CancellationTokenSource assemblyTrackingCancellation;
        private readonly LoggerResult assemblyReloadLogger = new LoggerResult();
        private bool assemblyChangesPending;
        private bool trackAssemblyChanges;
        private string outputTitle;
        private readonly string outputTitleBase = Tr._p("Title", "Output");
        private bool buildInProgress;
        private ICancellableAsyncBuild currentBuild;

        public DebuggingViewModel(GameStudioViewModel editor, IDebugService debugService)
            : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
        {
            this.editor = editor;
            this.debugService = debugService;

            outputTitle = outputTitleBase;

            BuildLog = new BuildLogViewModel(ServiceProvider);
            LiveScriptingLog = new LoggerViewModel(ServiceProvider);
            LiveScriptingLog.AddLogger(assemblyReloadLogger);
            BuildProjectCommand = new AnonymousTaskCommand(ServiceProvider, () => BuildProject(false));
            StartProjectCommand = new AnonymousTaskCommand(ServiceProvider, () => BuildProject(true));
            CancelBuildCommand = new AnonymousCommand(ServiceProvider, () => { currentBuild?.Cancel(); });
            LivePlayProjectCommand = new AnonymousTaskCommand(ServiceProvider, LivePlayProject);
            ReloadAssembliesCommand = new AnonymousTaskCommand(ServiceProvider, ReloadAssemblies) { IsEnabled = false };
            ResetOutputTitleCommand = new AnonymousCommand(ServiceProvider, () => OutputTitle = outputTitleBase);
            modifiedAssemblies = new Dictionary<PackageLoadedAssembly, ModifiedAssembly>();
            trackAssemblyChanges = true;

            assemblyTrackingCancellation = new CancellationTokenSource();

            // Create script resolver
            scriptsSorter = new ScriptSourceCodeResolver();
            ServiceProvider.RegisterService(scriptsSorter);

            assemblyReloadLogger.MessageLogged += (sender, e) => Dispatcher.InvokeAsync(() => OutputTitle = outputTitleBase + '*');
            editor.Session.PropertyChanged += SessionPropertyChanged;
            UpdateCommands();

            Task.Run(async () =>
            {
                var watcher = await editor.XenkoAssets.Code.ProjectWatcher;
                await scriptsSorter.Initialize(editor.Session, watcher, assemblyTrackingCancellation.Token);
                PullAssemblyChanges(watcher);
            });
        }

        /// <summary>
        /// Gets the current session.
        /// </summary>
        public SessionViewModel Session => editor.Session;

        /// <summary>
        /// Gets the build log.
        /// </summary>
        [NotNull]
        public BuildLogViewModel BuildLog { get; }

        /// <summary>
        /// Gets the live-scripting log.
        /// </summary>
        [NotNull]
        public LoggerViewModel LiveScriptingLog { get; }

        /// <summary>
        /// Gets the title of the output pane, including an asterisk if new logs have arrived.
        /// </summary>
        // We need to manage dirtiness of the output title directly in the string because the rad pane requires a single string as title
        public string OutputTitle { get => outputTitle; private set => SetValue(ref outputTitle, value); }

        /// <summary>
        /// Gets whether there is a build currently in progress.
        /// </summary>
        public bool BuildInProgress { get => buildInProgress; private set => SetValue(ref buildInProgress, value, UpdateCommands); }

        [NotNull]
        public ICommandBase BuildProjectCommand { get; }

        [NotNull]
        public ICommandBase StartProjectCommand { get; }

        [NotNull]
        public ICommandBase CancelBuildCommand { get; }

        [NotNull]
        public ICommandBase LivePlayProjectCommand { get; }

        [NotNull]
        public ICommandBase ReloadAssembliesCommand { get; }

        [NotNull]
        public ICommandBase ResetOutputTitleCommand { get; }

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
            BuildLog.Destroy();
            debugService?.Dispose();

            trackAssemblyChanges = false;
            assemblyTrackingCancellation.Cancel();
        }

        private async void PullAssemblyChanges([NotNull] ProjectWatcher projectWatcher)
        {
            var changesBuffer = new BufferBlock<AssemblyChangedEvent>();
            using (projectWatcher.AssemblyChangedBroadcast.LinkTo(changesBuffer))
            {
                while (!assemblyTrackingCancellation.IsCancellationRequested)
                {
                    var assemblyChange = await changesBuffer.ReceiveAsync(assemblyTrackingCancellation.Token);

                    if (!trackAssemblyChanges || assemblyChange == null)
                        continue;

                    // Ignore Binary changes
                    if (assemblyChange.ChangeType == AssemblyChangeType.Binary)
                        continue;

                    var shouldNotify = !assemblyChangesPending;
                    modifiedAssemblies[assemblyChange.Assembly] = new ModifiedAssembly
                    {
                        LoadedAssembly = assemblyChange.Assembly,
                        ChangeType = assemblyChange.ChangeType,
                        Project = assemblyChange.Project
                    };

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
            }
        }

        private void UpdateCommands()
        {
            assemblyChangesPending = modifiedAssemblies.Count > 0;
            var hasCurrentProject = editor.Session.CurrentProject != null;

            BuildProjectCommand.IsEnabled = !BuildInProgress;
            StartProjectCommand.IsEnabled = hasCurrentProject && !BuildInProgress;
            CancelBuildCommand.IsEnabled = BuildInProgress;
            LivePlayProjectCommand.IsEnabled = hasCurrentProject && !BuildInProgress;
            ReloadAssembliesCommand.IsEnabled = assemblyChangesPending && !BuildInProgress;
        }

        private struct ModifiedAssembly
        {
            public PackageLoadedAssembly LoadedAssembly;

            public string LoadedAssemblyPath;

            public AssemblyChangeType ChangeType;

            public Project Project;
        }

        private async Task ReloadAssemblies()
        {
            trackAssemblyChanges = false;

            var modifiedAssembliesCopy = new Dictionary<PackageLoadedAssembly, ModifiedAssembly>(modifiedAssemblies);
            var assembliesToReload = new List<ModifiedAssembly>();
            modifiedAssemblies.Clear();

            foreach (var modifiedAssembly in modifiedAssembliesCopy)
            {
                // If the assembly binary has changed, just reload
                if (modifiedAssembly.Value.ChangeType == AssemblyChangeType.Binary)
                {
                    assembliesToReload.Add(modifiedAssembly.Value);
                }
                else
                {
                    // If source code has changed, rebuild. If the build is successfull, reload the assembly.
                    // Otherwise add the assembly back to the list of modified ones.
                    var result = await BuildProject(modifiedAssembly.Key.ProjectReference.Location);

                    if (result.IsSuccessful)
                    {
                        var assemblyToReload = modifiedAssembly.Value;
                        assemblyToReload.LoadedAssemblyPath = result.AssemblyPath;
                        assembliesToReload.Add(assemblyToReload);
                    }
                    else
                    {
                        modifiedAssemblies[modifiedAssembly.Key] = modifiedAssembly.Value;
                    }
                }
            }

            UpdateCommands();
            trackAssemblyChanges = true;

            // If any assemblies are built successfully, reload them
            if (assembliesToReload.Count > 0)
            {
                using (var transaction = Session.UndoRedoService.CreateTransaction())
                {
                    var assemblyToAnalyze = assembliesToReload.Where(x => x.LoadedAssembly?.Assembly != null && x.Project != null).ToDictionary(x => x.Project, x => x.LoadedAssembly.Assembly.FullName);
                    var logResult = new LoggerResult();
                    BuildLog.AddLogger(logResult);
                    GameStudioAssemblyReloader.Reload(Session, logResult, async () =>
                    {
                        foreach (var assemblyToReload in assemblyToAnalyze)
                        {
                            await scriptsSorter.AnalyzeProject(Session, assemblyToReload.Key, assemblyTrackingCancellation.Token);
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
        }

        private async Task<bool> LivePlayProject()
        {
            LiveScriptingLog.ClearMessages();

            if (!await PrepareBuild())
                return false;

            // Make sure it is Windows platform (only supported for now)
            if (Session.CurrentProject.Platform != PlatformType.Windows)
            {
                await ServiceProvider.Get<IDialogService>()
                    .MessageBox(
                        string.Format(Tr._p("Message", "Platform {0} isn't supported for execution."), Session.CurrentProject.Platform),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            BuildInProgress = true;
            var jobToken = editor.Status.NotifyBackgroundJobStarted("Build in progress…", JobPriority.Compile);

            var result = false;
            try
            {
                // Build projects+assets (note: assets only would be enough)
                if (!await BuildProjectCore(false))
                {
                    return false;
                }

                // Start live debugging
                return result = await debugService.StartDebug(editor, Session.CurrentProject, assemblyReloadLogger);
            }
            finally
            {
                editor.Status.NotifyBackgroundJobFinished(jobToken);
                editor.Status.PushDiscardableStatus(result ? "Build successful" : "Build failed");
                BuildInProgress = false;
            }
        }

        private async Task<bool> BuildProject(bool startProject)
        {
            if (BuildInProgress)
                return false;

            try
            {
                BuildInProgress = true;
                if (!await PrepareBuild())
                    return false;

                var jobToken = editor.Status.NotifyBackgroundJobStarted("Building...", JobPriority.Compile);
                var result = false;
                try
                {
                    return result = await BuildProjectCore(startProject);
                }
                finally
                {
                    editor.Status.NotifyBackgroundJobFinished(jobToken);
                    editor.Status.PushDiscardableStatus(result ? "Build successful" : "Build failed");
                }
            }
            finally
            {
                BuildInProgress = false;
            }
        }

        private void RegisterBuildLogger(LoggerResult logger)
        {
            BuildLog.AddLogger(logger);
            logger.MessageLogged += (sender, e) => Dispatcher.InvokeAsync(() => OutputTitle = outputTitleBase + '*');
        }

        private async Task<bool> BuildProjectCore(bool startProject)
        {
            var logger = new LoggerResult();
            RegisterBuildLogger(logger);

            try
            {
                // Build configuration and parameters depending on platform
                var platformName = "AnyCPU";
                var configuration = "Debug";
                var target = "Build";
                var cpu = string.Empty; // Used only for Windows Phone so far, default to ARM (need to provide a selector or detection)
                var extraProperties = new Dictionary<string, string>
                {
                    ["XenkoBuildEngineLogPipeUrl"] = BuildLog.PipeName,
                    ["XenkoBuildEngineLogVerbose"] = "true",
                };

                var projectViewModel = Session.CurrentProject.Type == ProjectType.Executable ? Session.CurrentProject : null;

                if (Session.CurrentProject.Platform != PlatformType.Shared)
                {
                    if (!string.IsNullOrEmpty(Session.SolutionPath))
                    {
                        var solutionPath = UPath.Combine(Environment.CurrentDirectory, Session.SolutionPath);
                        extraProperties.Add("SolutionPath", solutionPath.ToWindowsPath());
                        extraProperties.Add("SolutionDir", solutionPath.GetParent().ToWindowsPath() + "\\");
                    }
                    else
                    {
                        logger.Verbose("Building from a .csproj file. The 'SolutionDir' and 'SolutionPath' variables will not be set for the build session.");
                    }

                    switch (Session.CurrentProject.Platform)
                    {
                        case PlatformType.Windows:
                            extraProperties.Add("SolutionPlatform", "Any CPU");
                            break;
                        case PlatformType.Android:
                            var androidDevices = AndroidDeviceEnumerator.ListAndroidDevices();
                            if (androidDevices.Length == 0)
                            {
                                logger.Error(Tr._p("Message", "No Android device found for execution."));
                                return false;
                            }

                            // On Android, directly install on device
                            platformName = "Android";
                            target = "GetAndroidPackage;Install";

                            // For now, use first android device
                            // TODO: Android device selector (together with platform selector)
                            extraProperties.Add("AdbTarget", "-s " + androidDevices[0].Serial);

                            extraProperties.Add("SolutionPlatform", "Android");
                            break;
                        case PlatformType.iOS:
                            platformName = "iPhone";
                            extraProperties.Add("SolutionPlatform", "iPhone");
                            break;

                        case PlatformType.Linux:
                            platformName = "Linux";
                            extraProperties.Add("SolutionPlatform", "Linux");
                            if (XenkoEditorSettings.UseCoreCLR.GetValue())
                            {
                                configuration = "CoreCLR_" + configuration;
                            }
                            break;

                        case PlatformType.macOS:
                            platformName = "macOS";
                            extraProperties.Add("SolutionPlatform", "macOS");
                            if (XenkoEditorSettings.UseCoreCLR.GetValue())
                            {
                                configuration = "CoreCLR_" + configuration;
                            }
                            break;

                        default:
                            logger.Error(string.Format(Tr._p("Message", "Platform {0} isn't supported for execution."), Session.CurrentProject.Platform));
                            return false;
                    }
                }

                if (projectViewModel == null)
                {
                    logger.Error(string.Format(Tr._p("Message", "Platform {0} isn't supported for execution."), Session.CurrentProject.Platform != PlatformType.Shared ? Session.CurrentProject.Platform : PlatformType.Windows));
                    return false;
                }

                // Build project
                currentBuild = VSProjectHelper.CompileProjectAssemblyAsync(Session?.SolutionPath, projectViewModel.ProjectPath, logger, target, configuration, platformName, extraProperties, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
                if (currentBuild == null)
                {
                    logger.Error(string.Format(Tr._p("Message", "Unable to load and compile project {0}"), projectViewModel.ProjectPath));
                    return false;
                }

                var assemblyPath = currentBuild.AssemblyPath;
                var buildTask = await currentBuild.BuildTask;

                // Execute
                if (startProject && !currentBuild.IsCanceled && !logger.HasErrors && projectViewModel.Platform != PlatformType.Shared)
                {
                    switch (projectViewModel.Platform)
                    {
                        case PlatformType.Windows:
                            if (string.Equals(Path.GetExtension(assemblyPath), ".exe", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!File.Exists(assemblyPath))
                                {
                                    logger.Error(string.Format(Tr._p("Message", "Unable to reach to output executable: {0}"), assemblyPath));
                                    return false;
                                }
                                var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo(assemblyPath)
                                    {
                                        WorkingDirectory = Path.GetDirectoryName(assemblyPath) ?? ""
                                    }
                                };
                                process.Start();
                            }
                            break;
                        case PlatformType.Android:
                            // Extract GetAndroidPackage result
                            if (!buildTask.ResultsByTarget.TryGetValue("GetAndroidPackage", out TargetResult targetResult))
                            {
                                logger.Error(string.Format(Tr._p("Message", "Couldn't find Android package name for {0}."), Session.CurrentProject.Name));
                                return false;
                            }

                            var packageName = targetResult.Items[0].ItemSpec;

                            // Locate ADB
                            var adbPath = await Task.Run(() => AndroidDeviceEnumerator.GetAdbPath());
                            if (adbPath == null)
                            {
                                logger.Error(Tr._p("Message", @"Android tool ""adb"" couldn't found (no running process, in registry or on the PATH). Please add it to your PATH."));
                                return false;
                            }
                            // Run
                            var adbResult = await Task.Run(() => ShellHelper.RunProcessAndGetOutput(adbPath, $@"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1"));
                            if (adbResult.ExitCode != 0)
                            {
                                logger.Error(string.Format(Tr._p("Message", "Can't run Android app with adb: {0}"), string.Join(Environment.NewLine, adbResult.OutputErrors)));
                                return false;
                            }

                            break;
                        case PlatformType.Linux:
                        case PlatformType.macOS:
                            {
                                // Sanity check to verify executable was compiled properly
                                if (string.Equals(Path.GetExtension(assemblyPath), ".exe", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (!File.Exists(assemblyPath))
                                    {
                                        logger.Error(Tr._p("Message", "Unable to reach to output executable: {0}"));
                                        return false;
                                    }
                                }

                                // Ask for credentials if requested, otherwise we use what we have stored.
                                if (XenkoEditorSettings.AskForCredentials.GetValue())
                                {
                                    var prompt = ServiceProvider.Get<IXenkoDialogService>().CreateCredentialsDialog();
                                    await prompt.ShowModal();
                                    if (!prompt.AreCredentialsValid)
                                    {
                                        logger.Error(string.Format(Tr._p("Message", "No credentials provided. To allow deployment, add your credentials.")));
                                        return false;
                                    }
                                }

                                // Launch game on remote host
                                var launchApp = await Task.Run(() => RemoteFacilities.Launch(logger, new UFile(assemblyPath), XenkoEditorSettings.UseCoreCLR.GetValue()));
                                if (!launchApp)
                                {
                                    logger.Error(string.Format(Tr._p("Message", "Unable to launch project {0}"), new UFile(assemblyPath).GetFileName()));
                                    return false;
                                }

                                break;
                            }
                    }

                    logger.Info(string.Format(Tr._p("Message", "Deployment of {0} successful."), projectViewModel.Name));
                }
            }
            catch (Exception e)
            {
                logger.Error("An exception occurred during compilation.", e);
                await ServiceProvider.Get<IDialogService>().MessageBox(string.Format(Tr._p("Message", "An exception occurred while compiling the project: {0}"), e.FormatSummary(true)), MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return !currentBuild.IsCanceled && !logger.HasErrors;
        }

        private async Task<bool> PrepareBuild()
        {
            if (Session.CurrentProject == null)
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "To process the build, set an executable project as the current project in the session explorer."), MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var saved = await Session.SaveSession();

            if (!saved)
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "To build, save the project first."), MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            BuildLog.ClearMessages();
            BuildLog.ClearLoggers();
            return true;
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
            BuildLog.ClearMessages();
            BuildLog.ClearLoggers();
            var logger = new LoggerResult();
            RegisterBuildLogger(logger);

            BuildInProgress = true;
            var jobToken = editor.Status.NotifyBackgroundJobStarted("Build in progress…", JobPriority.Compile);

            try
            {
                var platformName = "AnyCPU";
                var configuration = "Debug";
                var target = "Build";

                var extraProperties = new Dictionary<string, string>
                {
                    ["SolutionPlatform"] = "Any CPU",
                    ["XenkoBuildEngineLogPipeUrl"] = BuildLog.PipeName,
                    ["XenkoBuildEngineLogVerbose"] = "true",
                };

                currentBuild = VSProjectHelper.CompileProjectAssemblyAsync(Session?.SolutionPath, projectPath, logger, target, configuration, platformName, extraProperties, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
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
                await ServiceProvider.Get<IDialogService>().MessageBox(string.Format(Tr._p("Message", "An exception occurred while compiling the project: {0}"), e.FormatSummary(true)), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                editor.Status.NotifyBackgroundJobFinished(jobToken);
                editor.Status.PushDiscardableStatus(!logger.HasErrors ? "Build succeeded" : "Build failed");
                BuildInProgress = false;
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
    }
}

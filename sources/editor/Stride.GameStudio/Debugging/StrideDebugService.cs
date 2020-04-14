// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Assets.Presentation.View;
using Xenko.Debugger.Target;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Translation;
using Xenko.Core.VisualStudio;
using Xenko.Assets.Presentation.AssetEditors;

namespace Xenko.GameStudio.Debugging
{
    public class XenkoDebugService : IDebugService
    {
        public IDispatcherService Dispatcher { get; }

        public TimeSpan RecompilationDelay { get; set; }

        public XenkoDebugService(IViewModelServiceProvider serviceProvider)
        {
            Dispatcher = serviceProvider.Get<IDispatcherService>();
            RecompilationDelay = TimeSpan.FromSeconds(0.5);
        }

        public async Task<bool> StartDebug(EditorViewModel editor, ProjectViewModel currentProject, LoggerResult logger)
        {
            if (currentProject == null)
            {
                await editor.Session.Dialogs.MessageBox(Tr._p("Message", "An executable project must be set as current project in the session explorer in order to process build."),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            try
            {
                var projectWatcher = new ProjectWatcher(currentProject.Session, false);
                await projectWatcher.Initialize();

                var executableOutputPath = Path.GetDirectoryName(projectWatcher.CurrentGameExecutable.OutputFilePath);

                var debuggerProcess = await GetDebuggerProcess(editor);

                var projectCouldFirstCompile = new TaskCompletionSource<bool>();
                Task.Run(() => StartDebugHost(executableOutputPath, projectWatcher, projectCouldFirstCompile, RecompilationDelay, debuggerProcess, logger)).Forget();

                return await projectCouldFirstCompile.Task;
            }
            catch
            {
                return false;
            }
        }

        private static async Task StartDebugHost(string executableOutputPath, ProjectWatcher projectWatcher, TaskCompletionSource<bool> projectCouldFirstCompile, TimeSpan recompilationDelay, Process debuggerProcess, LoggerResult logger)
        {
            // Clear logger, so we don't fail because of a previous debug session
            logger.Clear();
            logger.HasErrors = false;

            var assemblyRecompiler = new AssemblyRecompiler();
            AssemblyRecompiler.UpdateResult updateResult;

            // TODO: When should we do the NuGet restore? Should we do it only once, or every change?

            try
            {
                updateResult = await assemblyRecompiler.Recompile(projectWatcher.CurrentGameLibrary, logger);

                if (updateResult.HasErrors)
                {
                    // Failure during initial compilation
                    updateResult.Error("Initial LiveScripting compilation failed, can't start live scripting");
                    projectCouldFirstCompile.TrySetResult(false);
                    return;
                }
            }
            catch (Exception e)
            {
                projectCouldFirstCompile.TrySetException(e);
                throw;
            }

            // Notify project could compile succesfully
            projectCouldFirstCompile.TrySetResult(true);

            using (var debugHost = new DebugHost())
            {
                // Start the debug host and wait for it to be available
                debugHost.Start(executableOutputPath, debuggerProcess, logger);
                var debugTarget = await debugHost.GameHost.Target;

                bool firstLoad = true;

                // List of currently loaded assemblies
                var loadedAssemblies = new Dictionary<AssemblyRecompiler.SourceGroup, DebugAssembly>(AssemblyRecompiler.SourceGroupComparer.Default);

                // Listen for game exited event
                var gameExited = new CancellationTokenSource();
                debugHost.GameHost.GameExited += gameExited.Cancel;

                while (!gameExited.IsCancellationRequested)
                {
                    if (!updateResult.HasErrors)
                    {
                        // Assemblies to unload, based on currently loaded ones
                        var assembliesToUnload = updateResult.UnloadedProjects.Select(x => loadedAssemblies[x]).ToList();

                        // Load new assemblies
                        foreach (var loadedProject in updateResult.LoadedProjects)
                        {
                            var assembly = debugTarget.AssemblyLoadRaw(loadedProject.PE, loadedProject.PDB);
                            loadedAssemblies[loadedProject] = assembly;
                        }

                        // Assemblies to load, based on the updated SourceGroup mapping
                        var assembliesToLoad = updateResult.LoadedProjects.Select(x => loadedAssemblies[x]).ToList();

                        // Update runtime game to use new assemblies
                        debugTarget.AssemblyUpdate(assembliesToUnload, assembliesToLoad);

                        // Start game on first load
                        if (firstLoad)
                        {
                            // Arbitrarily launch first game (should be only one anyway?)
                            // TODO: Maybe game is not even necessary anymore and we should just instantiate a "DefaultSceneGame"?
                            var games = debugTarget.GameEnumerateTypeNames();
                            debugTarget.GameLaunch(games.First());

                            firstLoad = false;
                        }
                    }

                    // Wait for any file change that would trigger a recompilation (or a game exit event)
                    await projectWatcher.ReceiveAndDiscardChanges(recompilationDelay, gameExited.Token);

                    // Check if live session exited to avoid recompiling for nothing
                    if (gameExited.IsCancellationRequested)
                        break;

                    // Update result for next loop
                    updateResult = await assemblyRecompiler.Recompile(projectWatcher.CurrentGameLibrary, logger);
                }
            }
        }

        private static async Task<Process> GetDebuggerProcess(EditorViewModel editor)
        {
            // Check if the current solution is opened in some IDE instance
            var process = await VisualStudioService.GetVisualStudio(editor.Session, false);

            if (process == null)
            {
                // If not, let the user pick an instance
                var picker = new DebuggerPickerWindow(VisualStudioDTE.GetActiveInstances());

                var result = await picker.ShowModal();

                if (result == DialogResult.Ok)
                {
                    process = await picker.SelectedDebugger.Launch(editor.Session);
                }
            }

            return process;
        }

        public void Dispose()
        {
        }
    }
}

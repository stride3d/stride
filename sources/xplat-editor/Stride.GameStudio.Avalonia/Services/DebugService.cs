// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Debugger.Target;
using Stride.GameStudio.Avalonia.Debugging;

namespace Stride.GameStudio.Avalonia.Services;

internal sealed partial class DebugService : IDebugService
{
    private readonly TimeSpan recompilationDelay = TimeSpan.FromSeconds(0.5);

    public async Task<bool> StartDebugAsync(ISessionViewModel session, ProjectViewModel project, LoggerResult logger)
    {
        try
        {
            using var projectWatcher = new ProjectWatcher(session, false);
            await projectWatcher.Initialize();

            var executableOutputPath = (UFile)projectWatcher.CurrentGameExecutable!.OutputFilePath;
            var projectCouldCompile = new TaskCompletionSource<bool>();
            Task.Run(() => StartDebugHostAsync(executableOutputPath.GetFullDirectory(), projectWatcher, projectCouldCompile, recompilationDelay, logger)).Forget();
            return await projectCouldCompile.Task;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task StartDebugHostAsync(UDirectory outputPath, ProjectWatcher watcher, TaskCompletionSource<bool> projectCouldComplile, TimeSpan recompilationDelay, LoggerResult logger)
    {
        // Clear logger, so we don't fail because of a previous debug session
        logger.Clear();
        logger.HasErrors = false;

        var assemblyRecompiler = new AssemblyRecompiler();
        AssemblyRecompiler.UpdateResult updateResult;
        // TODO: When should we do the NuGet restore? Should we do it only once, or every change?

        try
        {
            updateResult = await assemblyRecompiler.Recompile(watcher.CurrentGameLibrary!, logger);
            if (updateResult.HasErrors)
            {
                // Failure during initial compilation
                updateResult.Error("Initial LiveScripting compilation failed, can't start live scripting");
                projectCouldComplile.TrySetResult(false);
                return;
            }
        }
        catch (Exception e)
        {
            projectCouldComplile.TrySetException(e);
            throw;
        }

        // Notify project could compile successfully
        projectCouldComplile.TrySetResult(true);

        using var debugHost = new DebugHost();

        // Start the debug host and wait for it to be available
        debugHost.Start(outputPath, logger);
        var debugTarget = await debugHost.GameHost!.Target;

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
            await watcher.ReceiveAndDiscardChanges(recompilationDelay, gameExited.Token);

            // Check if live session exited to avoid recompiling for nothing
            if (gameExited.IsCancellationRequested)
                break;

            // Update result for next loop
            updateResult = await assemblyRecompiler.Recompile(watcher.CurrentGameLibrary, logger);
        }
    }
}

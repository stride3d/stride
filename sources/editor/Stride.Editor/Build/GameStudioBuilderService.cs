// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stride.Assets.Effect;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Editor.Build
{
    public sealed class GameStudioBuilderService : AssetBuilderService
    {
        public static string? GlobalEffectLogPath;
        private readonly EffectPriorityScheduler taskScheduler;
        private readonly EffectCompilerBase effectCompiler;
        private readonly IEditorDebugService? debugService;
        private readonly IDebugPage? assetBuilderServiceDebugPage;
        private readonly IDebugPage? effectCompilerServiceDebugPage;
        private readonly bool createDebugTools;
        private int currentJobToken = -1;

        // The shader files the last build of each build unit read, watched so that what they built can be built again
        private readonly object shaderDependentsLock = new();
        private readonly Dictionary<(Guid Context, AssetId Asset), HashSet<string>> shaderFilesByUnit = new();
        private readonly HashSet<string> watchedShaderFiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> changedShaderFiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly DirectoryWatcher shaderFileWatcher = new("*.sdsl");
        private bool shaderChangePending;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameStudioBuilderService"/> class.
        /// </summary>
        /// <param name="session">The session whose assets to build.</param>
        /// <param name="settingsProvider">The provider of the game settings of the session.</param>
        /// <param name="buildDirectory">The directory of the build database.</param>
        /// <param name="createDebugTools">Whether to create the debug pages, when the host registers an <see cref="IEditorDebugService"/>.</param>
        public GameStudioBuilderService(SessionViewModel session, GameSettingsProviderService settingsProvider, string buildDirectory, bool createDebugTools = true)
            : base(buildDirectory)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            this.createDebugTools = createDebugTools;
            if (createDebugTools)
            {
                // The debug pages are a feature of the host (the WPF studio always has them): without the service there are none
                debugService = session.ServiceProvider.TryGet<IEditorDebugService>();
                assetBuilderServiceDebugPage = debugService?.CreateLogDebugPage(Logger, "AssetBuilderService");
                effectCompilerServiceDebugPage = debugService?.CreateLogDebugPage(GlobalLogger.GetLogger("EffectCompilerCache"), "EffectCompilerCache");
            }

            // The effects compile from the shader files of the session where they are, not from an import into the database
            ShaderSources = ShaderSourceLocator.Create(session.AllPackages.Select(x => x.Package));

            Database = new GameStudioDatabase(this, settingsProvider);

            const string shaderBundleUrl = "/binary/editor/EditorShadersD3D11.bundle";
            if (VirtualFileSystem.FileExists(shaderBundleUrl))
            {
                Builder.ObjectDatabase.BundleBackend.LoadBundleFromUrl("EditorShadersD3D11", Builder.ObjectDatabase.ContentIndexMap, shaderBundleUrl, true).Wait();
            }

            // Use a shared database for our shader system
            // TODO: Shaders compiled on main thread won't actually be visible to MicroThread build engine (contentIndexMap are separate).
            // It will still work and cache because EffectCompilerCache caches not only at the index map level, but also at the database level.
            // Later, we probably want to have a GetSharedDatabase() allowing us to mutate it (or merging our results back with IndexFileCommand.AddToSharedGroup()),
            // so that database created with MountDatabase also have all the newest shaders.
            taskScheduler = new EffectPriorityScheduler(ThreadPriority.BelowNormal, Math.Max(1, Environment.ProcessorCount / 2));
            TaskSchedulerSelector taskSchedulerSelector = (mixinTree, compilerParameters) => taskScheduler.GetOrCreatePriorityGroup(compilerParameters?.TaskPriority ?? 0);
            var sharedDatabase = MicrothreadLocalDatabases.GetSharedDatabase();
            effectCompiler = (EffectCompilerBase)EffectCompilerFactory.CreateEffectCompiler(new ShaderSourceFileProvider(ShaderSources, sharedDatabase), taskSchedulerSelector: taskSchedulerSelector, database: sharedDatabase);
            shaderFileWatcher.Modified += ShaderFileModified;

            StartPushNotificationsTask();
        }

        /// <summary>
        /// Raised on a thread of the thread pool when shader files that builds read changed, a short time after the last change,
        /// so that what they built can be built again: the effects of the editor games follow the files by themselves, but not what
        /// a build made with shaders, such as the lighting of a skybox or a thumbnail.
        /// </summary>
        public event EventHandler<ShaderSourcesChangedEventArgs>? ShaderSourcesChanged;

        /// <summary>
        /// Gets or sets how long <see cref="ShaderSourcesChanged"/> waits after a change of a shader file.
        /// </summary>
        /// <remarks>
        /// An editor that saves a file raises several events, and the effect systems of the editor games, which watch the same files,
        /// must forget the effects compiled from the old content before anything builds again.
        /// </remarks>
        internal TimeSpan ShaderChangeDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets the session attached to this building service.
        /// </summary>
        public SessionViewModel Session { get; }

        public GameStudioDatabase Database { get; }

        /// <summary>
        /// Gets the effect compiler.
        /// </summary>
        public IEffectCompiler EffectCompiler => effectCompiler;

        public string? EffectLogPath => GlobalEffectLogPath;

        /// <summary>
        /// Gets the locator of the shader files of the session, which <see cref="EffectCompiler"/> reads.
        /// </summary>
        public ShaderSourceLocator ShaderSources { get; }

        /// <summary>
        /// Gets whether this instance of <see cref="GameStudioBuilderService"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Stops the loop of <see cref="StartPushNotificationsTask"/>.</summary>
        private readonly CancellationTokenSource notificationsCancellation = new();

        /// <summary>
        ///   The number of shader-compile tasks waiting in the queue. Test harnesses use this to
        ///   detect a quiescent state; production code should treat it as informational only.
        /// </summary>
        public int PendingShaderCompilationCount => taskScheduler.QueuedTaskCount;

        public override void Dispose()
        {
            base.Dispose();
            if (createDebugTools)
            {
                debugService?.UnregisterDebugPage(assetBuilderServiceDebugPage);
                debugService?.UnregisterDebugPage(effectCompilerServiceDebugPage);
            }
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
            notificationsCancellation.Cancel();
            shaderFileWatcher.Modified -= ShaderFileModified;
            shaderFileWatcher.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnBuildUnitProcessed(AssetBuildUnit unit, ListBuildStep buildStep)
        {
            // The commands that ran shaders (skyboxes, thumbnails) registered the shader files they read as inputs
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var step in buildStep.EnumerateRecursively())
            {
                if (step is not CommandBuildStep { Result: { } result })
                    continue;

                foreach (var input in result.InputDependencyVersions.Keys)
                {
                    if (input.Type == UrlType.File && input.Path.EndsWith(".sdsl", StringComparison.OrdinalIgnoreCase))
                        files.Add(Path.GetFullPath(input.Path));
                }
            }

            var unitKey = (unit.Identifier.ContextIdentifier, unit.Identifier.AssetIdentifier);
            lock (shaderDependentsLock)
            {
                if (IsDisposed)
                    return;

                // A failed build may stop before it reads them all: the files of the last build still count, so that a fix builds it again
                if (buildStep.Failed && shaderFilesByUnit.TryGetValue(unitKey, out var previousFiles))
                    files.UnionWith(previousFiles);

                if (files.Count == 0)
                {
                    shaderFilesByUnit.Remove(unitKey);
                    return;
                }

                shaderFilesByUnit[unitKey] = files;
                foreach (var file in files)
                {
                    if (watchedShaderFiles.Add(file))
                        shaderFileWatcher.Track(file);
                }
            }
        }

        private void ShaderFileModified(object? sender, FileEvent e)
        {
            // A text editor can save by writing another file and renaming it
            if (e.ChangeType is not (FileEventChangeType.Changed or FileEventChangeType.Created or FileEventChangeType.Renamed))
                return;

            var file = Path.GetFullPath(e.FullPath);
            lock (shaderDependentsLock)
            {
                if (!watchedShaderFiles.Contains(file))
                    return;

                changedShaderFiles.Add(file);
                if (shaderChangePending)
                    return;
                shaderChangePending = true;
            }

            _ = NotifyShaderSourcesChangedAsync();
        }

        private async Task NotifyShaderSourcesChangedAsync()
        {
            await Task.Delay(ShaderChangeDelay);

            string[] files;
            List<AssetBuildUnitIdentifier> dependents;
            lock (shaderDependentsLock)
            {
                files = changedShaderFiles.ToArray();
                changedShaderFiles.Clear();
                shaderChangePending = false;
                dependents = shaderFilesByUnit.Where(x => x.Value.Overlaps(files)).Select(x => new AssetBuildUnitIdentifier(x.Key.Context, x.Key.Asset)).ToList();
            }

            if (dependents.Count == 0 || IsDisposed)
                return;

            try
            {
                ShaderSourcesChanged?.Invoke(this, new ShaderSourcesChangedEventArgs(files, dependents));
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while building again what used the changed shader files.", ex);
            }
        }

        /// <summary>
        /// Reads the shader files of the session again, after shader assets were added, removed or moved.
        /// </summary>
        /// <remarks>
        /// Call it from the thread that changes the session. A shader whose file changed is compiled again from its new file;
        /// a change of the content of a file is found by the hot reload of the effects instead.
        /// </remarks>
        public void UpdateShaderSources()
        {
            var changed = ShaderSources.Update(Session.AllPackages.Select(x => x.Package));
            if (changed.Count > 0)
                effectCompiler.ResetCache(changed.Select(x => System.IO.Path.GetFileNameWithoutExtension(x)).ToHashSet());
        }

        private void StartPushNotificationsTask()
        {
            // Shows the shader compilations in the status bar until the service is disposed, as a background work of the session
            var token = notificationsCancellation.Token;
            Session.TrackBackgroundWork(Task.Run(async () =>
            {
                while (!IsDisposed)
                {
                    try
                    {
                        await Task.Delay(500, token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    if (currentJobToken >= 0)
                    {
                        if (taskScheduler.QueuedTaskCount > 0)
                        {
                            Session.Main.Status.NotifyBackgroundJobProgress(currentJobToken, taskScheduler.QueuedTaskCount, true);
                        }
                        else
                        {
                            Session.Main.Status.NotifyBackgroundJobFinished(currentJobToken);
                            currentJobToken = -1;
                        }
                    }
                    else if (taskScheduler.QueuedTaskCount > 0)
                    {
                        currentJobToken = Session.Main.Status.NotifyBackgroundJobStarted("Building effects ({0} in queue)", JobPriority.Editor);
                    }
                }
            }));
        }
    }
}

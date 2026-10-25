// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Editor.Build
{
    public sealed class GameStudioBuilderService : AssetBuilderService
    {
        public static string? GlobalEffectLogPath;
        private readonly ManualResetEvent shaderLoadedEvent = new ManualResetEvent(false);
        private readonly TaskCompletionSource shadersLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly EffectPriorityScheduler taskScheduler;
        private readonly EffectCompilerBase effectCompiler;
        private readonly IEditorDebugService? debugService;
        private readonly IDebugPage? assetBuilderServiceDebugPage;
        private readonly IDebugPage? effectCompilerServiceDebugPage;
        private readonly bool createDebugTools;
        private int currentJobToken = -1;

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

            var shaderImporter = new StrideShaderImporter();
            var shaderBuildSteps = shaderImporter.CreateSystemShaderBuildSteps(session);
            if (shaderBuildSteps != null)
            {
                shaderBuildSteps.StepProcessed += ShaderBuildStepsStepProcessed;
                ShaderImportProgress = new ShaderImportProgressStatus(shaderBuildSteps);
                PushBuildUnit(new PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier.Default, shaderBuildSteps, true));
            }
            else
            {
                // No package of the session has system shaders (a session without the Stride package, the tests): nothing to wait for
                shaderLoadedEvent.Set();
                shadersLoaded.TrySetResult();
            }

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
            effectCompiler = (EffectCompilerBase)EffectCompilerFactory.CreateEffectCompiler(MicrothreadLocalDatabases.GetSharedDatabase(), taskSchedulerSelector: taskSchedulerSelector);

            StartPushNotificationsTask();
        }

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
        /// Gets the progress of the import of the system shaders, the build the session runs when it opens (thumbnails and previews
        /// wait for it); <c>null</c> when there was nothing to import.
        /// </summary>
        public IProgressStatus? ShaderImportProgress { get; }

        /// <summary>
        /// Gets whether the system shaders are imported (or there were none to import).
        /// </summary>
        public bool AreShadersLoaded => shadersLoaded.Task.IsCompleted;

        /// <summary>
        /// Gets whether this instance of <see cref="GameStudioBuilderService"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

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
        }

        private void ShaderBuildStepsStepProcessed(object? sender, BuildStepEventArgs e)
        {
            shaderLoadedEvent.Set();
            shadersLoaded.TrySetResult();
        }

        public void WaitForShaders()
        {
            shaderLoadedEvent.WaitOne();
        }

        /// <summary>
        /// Waits for the system shaders to be imported, without blocking the caller as <see cref="WaitForShaders"/> does.
        /// </summary>
        public Task WaitForShadersAsync() => shadersLoaded.Task;

        private void StartPushNotificationsTask()
        {
            Task.Run(async () =>
            {
                while (!IsDisposed)
                {
                    await Task.Delay(500);
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
            });
        }

        /// <summary>
        /// Reports the progress of the system shader import: one step per build step of the import (a shader asset, then the
        /// update of the import cache).
        /// </summary>
        private sealed class ShaderImportProgressStatus : IProgressStatus
        {
            private readonly int stepCount;
            private int processedSteps;

            public ShaderImportProgressStatus(ListBuildStep buildSteps)
            {
                var steps = buildSteps.Steps.ToList();
                stepCount = steps.Count;
                foreach (var step in steps)
                    step.StepProcessed += StepProcessed;
            }

            public event EventHandler<ProgressStatusEventArgs>? ProgressChanged;

            public void OnProgressChanged(ProgressStatusEventArgs e)
            {
                ProgressChanged?.Invoke(this, e);
            }

            private void StepProcessed(object? sender, BuildStepEventArgs e)
            {
                e.Step.StepProcessed -= StepProcessed;
                var current = Interlocked.Increment(ref processedSteps);
                OnProgressChanged(new ProgressStatusEventArgs($"Importing the system shaders ({current}/{stepCount})", current, stepCount));
            }
        }
    }
}

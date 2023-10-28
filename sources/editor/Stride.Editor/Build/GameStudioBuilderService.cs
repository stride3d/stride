// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Shaders.Compiler;

namespace Stride.Editor.Build;

public class GameStudioBuilderService : AssetBuilderService
{
    public static string GlobalEffectLogPath;
    private readonly ManualResetEvent shaderLoadedEvent = new(false);
    private readonly EffectPriorityScheduler taskScheduler;
    private readonly EffectCompilerBase effectCompiler;
    // FIXME xplat-editor
    //private readonly IDebugPage assetBuilderServiceDebugPage;
    //private readonly IDebugPage effectCompilerServiceDebugPage;
    private readonly bool createDebugTools;
    private int currentJobToken = -1;

    public GameStudioBuilderService(ISessionViewModel sessionViewModel, GameSettingsProviderService settingsProvider, string buildDirectory, bool createDebugTools = true)
        : base(buildDirectory)
    {
        this.createDebugTools = createDebugTools;
        if (createDebugTools)
        {
            // FIXME xplat-editor
            //assetBuilderServiceDebugPage = EditorDebugTools.CreateLogDebugPage(GlobalLogger.GetLogger("AssetBuilderService"), "AssetBuilderService");
            //effectCompilerServiceDebugPage = EditorDebugTools.CreateLogDebugPage(GlobalLogger.GetLogger("EffectCompilerCache"), "EffectCompilerCache");
        }

        Session = sessionViewModel ?? throw new ArgumentNullException(nameof(sessionViewModel));

        var shaderImporter = new StrideShaderImporter();
        var shaderBuildSteps = shaderImporter.CreateSystemShaderBuildSteps(sessionViewModel);
        shaderBuildSteps.StepProcessed += ShaderBuildStepsStepProcessed;
        PushBuildUnit(new PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier.Default, shaderBuildSteps, true));

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
    /// Gets the session view model attached to this building service.
    /// </summary>
    /// <value>The session view model.</value>
    public ISessionViewModel Session { get; private set; }

    public GameStudioDatabase Database { get; }

    /// <summary>
    /// Gets the effect compiler.
    /// </summary>
    public IEffectCompiler EffectCompiler => effectCompiler;

    public string EffectLogPath => GlobalEffectLogPath;

    /// <summary>
    /// Gets whether this instance of <see cref="GameStudioBuilderService"/> has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    public override void Dispose()
    {
        base.Dispose();
        if (createDebugTools)
        {
            // FIXME xplat-editor
            //EditorDebugTools.UnregisterDebugPage(assetBuilderServiceDebugPage);
            //EditorDebugTools.UnregisterDebugPage(effectCompilerServiceDebugPage);
        }
        if (!IsDisposed)
        {
            IsDisposed = true;
        }
    }

    private void ShaderBuildStepsStepProcessed(object? sender, BuildStepEventArgs e)
    {
        shaderLoadedEvent.Set();
    }

    public void WaitForShaders()
    {
        // TODO: turn into task
        shaderLoadedEvent.WaitOne();
    }

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
                        // FIXME xplat-editor
                        //EditorViewModel.Instance.Status.NotifyBackgroundJobProgress(currentJobToken, taskScheduler.QueuedTaskCount, true);
                    }
                    else
                    {
                        // FIXME xplat-editor
                        //EditorViewModel.Instance.Status.NotifyBackgroundJobFinished(currentJobToken);
                        currentJobToken = -1;
                    }
                }
                else if (taskScheduler.QueuedTaskCount > 0)
                {
                    // FIXME xplat-editor
                    //currentJobToken = EditorViewModel.Instance.Status.NotifyBackgroundJobStarted("Building effects ({0} in queue)", JobPriority.Editor);
                }
            }
        });
    }
}

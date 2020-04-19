// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Assets;
using Stride.Assets.SpriteFont;
using Stride.Assets.SpriteFont.Compiler;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Shaders.Compiler;

namespace Stride.Editor.Preview
{
    /// <summary>
    /// A <see cref="Game"/> instance specialized to render previews and create thumbnails
    /// </summary>
    public class PreviewGame : EditorGame.Game.EditorServiceGame
    {
        private readonly IEffectCompiler effectCompiler;

        /// <summary>
        /// The pending preview request to process.
        /// </summary>
        private PreviewRequest previewRequest;

        private readonly object requestLock = new object();

        /// <summary>
        /// A default font that can be used when rendering previews or thumbnails
        /// </summary>
        public SpriteFont DefaultFont;

        /// <summary>
        /// A callback that can be used to update the scene.
        /// </summary>
        public Func<RenderingMode> UpdateSceneCallback;

        private Scene previewScene;

        public PreviewGame(IEffectCompiler effectCompiler)
        {
            this.effectCompiler = effectCompiler;
        }

        /// <inheritdoc />
        public override Vector3 GetPositionInScene(Vector2 mousePosition)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void TriggerActiveRenderStageReevaluation()
        {
            var visgroups = SceneSystem.SceneInstance.VisibilityGroups;
            if (visgroups != null)
            {
                foreach (var sceneInstanceVisibilityGroup in visgroups)
                {
                    sceneInstanceVisibilityGroup.NeedActiveRenderStageReevaluation = true;
                }
            }
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            // Use a shared database for our shader system
            // TODO: Shaders compiled on main thread won't actually be visible to MicroThread build engine (contentIndexMap are separate).
            // It will still work and cache because EffectCompilerCache caches not only at the index map level, but also at the database level.
            // Later, we probably want to have a GetSharedDatabase() allowing us to mutate it (or merging our results back with IndexFileCommand.AddToSharedGroup()),
            // so that database created with MountDatabase also have all the newest shaders.
            ((IReferencable)effectCompiler).AddReference();
            EffectSystem.Compiler = effectCompiler;
        }

        /// <inheritdoc />
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // create the default fonts
            var fontItem = OfflineRasterizedSpriteFontFactory.Create();
            fontItem.FontType.Size = 22;
            DefaultFont = OfflineRasterizedFontCompiler.Compile(Font, fontItem, GraphicsDevice.ColorSpace == ColorSpace.Linear);

            previewScene = new Scene();

            // create and set the main scene instance
            SceneSystem.SceneInstance = new SceneInstance(Services, previewScene, ExecutionMode.Preview);

            // add thumbnail builder and preview script to scheduler
            Script.AddTask(ProcessPreviewRequestsTask);
        }

        /// <inheritdoc />
        protected override bool OnFault(Exception ex)
        {
            base.OnFault(ex);
            // Always handle the exception
            return true;
        }

        private async Task ProcessPreviewRequestsTask()
        {
            while (IsRunning)
            {
                await Script.NextFrame();

                PreviewRequest request;
                lock (requestLock)
                {
                    request = previewRequest;
                    previewRequest = null;
                }

                if (request != null)
                {
                    try
                    {
                        MicrothreadLocalDatabases.MountCommonDatabase();

                        Faulted = false;

                        previewScene.Children.Clear();

                        if (SceneSystem.GraphicsCompositor != request.GraphicsCompositor)
                        {
                            SceneSystem.GraphicsCompositor?.Dispose();
                            SceneSystem.GraphicsCompositor = request.GraphicsCompositor;
                        }

                        if (request.Scene != null)
                            previewScene.Children.Add(request.Scene);

                        request.RequestCompletion.SetResult(ResultStatus.Successful);
                    }
                    catch (Exception e)
                    {
                        // end the thumbnail build task
                        request.Logger.Error("An exception occurred while loading the preview scene.", e);
                        request.RequestCompletion.SetResult(ResultStatus.Failed);
                    }
                }

                if (previewScene.Children.Count != 0)
                {
                    var handler = UpdateSceneCallback;
                    if (handler != null)
                    {
                        var renderingMode = handler();
                    }
                }
            }
        }

        /// <summary>
        /// Load a preview scene into the preview game.
        /// </summary>
        /// <param name="previewScene">The scene to load as preview</param>
        /// <param name="logger">The logger to use in case of errors.</param>
        /// <returns>The result of the scene load</returns>
        public async Task<ResultStatus> LoadPreviewScene(Scene previewScene, GraphicsCompositor graphicsCompositor, ILogger logger)
        {
            lock (requestLock)
            {
                previewRequest = new PreviewRequest(previewScene, graphicsCompositor, logger);
            }

            return await previewRequest.RequestCompletion.Task;
        }

        /// <summary>
        /// Unload the current preview scene.
        /// </summary>
        /// <param name="logger">The logger to use in case of errors.</param>
        public async Task UnloadPreviewScene(ILogger logger)
        {
            await LoadPreviewScene(null, null, null);
        }

        private class PreviewRequest
        {
            /// <summary>
            /// The signal triggered when the preview request is completed.
            /// </summary>
            public readonly TaskCompletionSource<ResultStatus> RequestCompletion = new TaskCompletionSource<ResultStatus>();

            /// <summary>
            /// The log to use in case of error
            /// </summary>
            public readonly ILogger Logger;

            /// <summary>
            /// The preview scene to display
            /// </summary>
            public readonly Scene Scene;

            public readonly GraphicsCompositor GraphicsCompositor;

            public PreviewRequest(Scene scene, GraphicsCompositor graphicsCompositor, ILogger logger)
            {
                Logger = logger;
                Scene = scene;
                GraphicsCompositor = graphicsCompositor;
            }
        }
    }
}

// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Assets;
using Xenko.Assets.SpriteFont;
using Xenko.Assets.SpriteFont.Compiler;
using Xenko.Editor.Preview;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Graphics.Font;
using Xenko.Physics;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;
using Xenko.Rendering.Fonts;
using Xenko.Shaders.Compiler;
using Xenko.UI;

namespace Xenko.Editor.Thumbnails
{
    public class ThumbnailGenerator : IDisposable
    {
        /// <summary>
        /// A constant string that can be used to identify an attribute of type <see cref="PreviewGame"/> in collections.
        /// </summary>
        public static readonly PropertyKey<ThumbnailGenerator> Key = new PropertyKey<ThumbnailGenerator>("ThumbnailGeneratorKey", typeof(ThumbnailGenerator));

        /// <summary>
        /// The list of services used for the thumbnails
        /// </summary>
        public readonly IServiceRegistry Services;

        /// <summary>
        /// The preview game system collection.
        /// </summary>
        private readonly GameSystemCollection gameSystems;

        /// <summary>
        /// The asset manager to use when building thumbnails.
        /// </summary>
        public readonly ContentManager ContentManager;

        /// <summary>
        /// Gets the instance of graphics device dedicated to thumbnail generation.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; }

        public CommandList GraphicsCommandList { get; }

        public GraphicsContext GraphicsContext { get; set; }

        private readonly SceneSystem sceneSystem;

        public EffectSystem EffectSystem { get; }

        public IGraphicsDeviceService GraphicsDeviceService { get; private set; }

        /// <summary>
        /// An instance of sprite batch that can be used to draw text or images.
        /// </summary>
        public readonly SpriteBatch SpriteBatch;

        /// <summary>
        /// An instance of ui batch that can be used to draw text or images, including sdf fonts.
        /// </summary>
        public readonly UIBatch UIBatch;

        /// <summary>
        /// A default font that can be used when rendering previews or thumbnails
        /// </summary>
        public readonly SpriteFont DefaultFont;

        private readonly GameTime nullGameTime;

        private readonly GameFontSystem fontSystem;

        private readonly object lockObject = new object();

        private readonly HashSet<GraphicsCompositor> thumbnailGraphicsCompositors = new HashSet<GraphicsCompositor>();

        /// <summary>
        /// The main scene used to render thumbnails.
        /// </summary>
        private Scene thumbnailScene;

        public ThumbnailGenerator(EffectCompilerBase effectCompiler)
        {
            // create base services
            Services = new ServiceRegistry();
            Services.AddService(MicrothreadLocalDatabases.ProviderService);
            ContentManager = new ContentManager(Services);
            Services.AddService<IContentManager>(ContentManager);
            Services.AddService(ContentManager);

            GraphicsDevice = GraphicsDevice.New();
            GraphicsContext = new GraphicsContext(GraphicsDevice);
            GraphicsCommandList = GraphicsContext.CommandList;
            Services.AddService(GraphicsContext);
            sceneSystem = new SceneSystem(Services);
            Services.AddService(sceneSystem);
            fontSystem = new GameFontSystem(Services);
            Services.AddService(fontSystem.FontSystem);
            Services.AddService<IFontFactory>(fontSystem.FontSystem);

            GraphicsDeviceService = new GraphicsDeviceServiceLocal(Services, GraphicsDevice);
            Services.AddService(GraphicsDeviceService);

            var uiSystem = new UISystem(Services);
            Services.AddService(uiSystem);

            var physicsSystem = new Bullet2PhysicsSystem(Services);
            Services.AddService<IPhysicsSystem>(physicsSystem);

            gameSystems = new GameSystemCollection(Services) { fontSystem, uiSystem, physicsSystem };
            Services.AddService<IGameSystemCollection>(gameSystems);
            Simulation.DisableSimulation = true; //make sure we do not simulate physics within the editor

            // initialize base services
            gameSystems.Initialize();

            // create remaining services
            EffectSystem = new EffectSystem(Services);
            Services.AddService(EffectSystem);

            gameSystems.Add(EffectSystem);
            gameSystems.Add(sceneSystem);
            EffectSystem.Initialize();

            // Mount the same database for the cache
            EffectSystem.Compiler = EffectCompilerFactory.CreateEffectCompiler(effectCompiler.FileProvider, EffectSystem);

            // Deactivate the asynchronous effect compilation
            ((EffectCompilerCache)EffectSystem.Compiler).CompileEffectAsynchronously = false;
            
            // load game system content
            gameSystems.LoadContent();

            // create the default fonts
            var fontItem = OfflineRasterizedSpriteFontFactory.Create();
            fontItem.FontType.Size = 22;
            DefaultFont = OfflineRasterizedFontCompiler.Compile(fontSystem.FontSystem, fontItem, true);

            // create utility members
            nullGameTime = new GameTime();
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            UIBatch = new UIBatch(GraphicsDevice);

            // create the pipeline
            SetUpPipeline();
        }

        private void SetUpPipeline()
        {
            // create the main preview scene.
            thumbnailScene = new Scene();

            // create and set the main scene instance
            sceneSystem.SceneInstance = new SceneInstance(Services, thumbnailScene, ExecutionMode.Thumbnail);
        }

        /// <summary>
        /// The micro-thread in charge of processing the thumbnail build requests and creating the thumbnails.
        /// </summary>
        private ResultStatus ProcessThumbnailRequests(ThumbnailBuildRequest request)
        {
            var status = ResultStatus.Successful;

            // Global lock so that only one rendering happens at the same time
            lock (lockObject)
            {
                try
                {
                    lock (AssetBuilderService.OutOfMicrothreadDatabaseLock)
                    {
                        MicrothreadLocalDatabases.MountCommonDatabase();

                        // set the master output
                        var renderTarget = GraphicsContext.Allocator.GetTemporaryTexture2D(request.Size.X, request.Size.Y, request.ColorSpace == ColorSpace.Linear ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
                        var depthStencil = GraphicsContext.Allocator.GetTemporaryTexture2D(request.Size.X, request.Size.Y, PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);

                        try
                        {
                            // Fake presenter
                            // TODO GRAPHICS REFACTOR: Try to remove that
                            GraphicsDevice.Presenter = new RenderTargetGraphicsPresenter(GraphicsDevice, renderTarget, depthStencil.ViewFormat);

                            // Always clear the state of the GraphicsDevice to make sure a scene doesn't start with a wrong setup 
                            GraphicsCommandList.ClearState();

                            // Setup the color space when rendering a thumbnail
                            GraphicsDevice.ColorSpace = request.ColorSpace;

                            // render the thumbnail
                            thumbnailScene.Children.Add(request.Scene);

                            // Store the graphics compositor to use, so we can dispose it when disposing this ThumbnailGenerator
                            thumbnailGraphicsCompositors.Add(request.GraphicsCompositor);
                            sceneSystem.GraphicsCompositor = request.GraphicsCompositor;

                            // Render once to setup render processors
                            // TODO GRAPHICS REFACTOR: Should not require two rendering
                            GraphicsContext.ResourceGroupAllocator.Reset(GraphicsContext.CommandList);
                            gameSystems.Draw(nullGameTime);

                            // Draw
                            gameSystems.Update(nullGameTime);
                            GraphicsContext.ResourceGroupAllocator.Reset(GraphicsContext.CommandList);
                            gameSystems.Draw(nullGameTime);

                            // write the thumbnail to the file
                            using (var thumbnailImage = renderTarget.GetDataAsImage(GraphicsCommandList))
                            using (var outputImageStream = request.FileProvider.OpenStream(request.Url, VirtualFileMode.Create, VirtualFileAccess.Write))
                            {
                                request.PostProcessThumbnail?.Invoke(thumbnailImage);

                                ThumbnailBuildHelper.ApplyThumbnailStatus(thumbnailImage, request.DependencyBuildStatus);

                                thumbnailImage.Save(outputImageStream, ImageFileType.Png);

                                request.Logger.Info($"Thumbnail creation successful [{request.Url}] to ({thumbnailImage.Description.Width}x{thumbnailImage.Description.Height},{thumbnailImage.Description.Format})");
                            }
                        }
                        finally
                        {
                            // Cleanup the scene
                            thumbnailScene.Children.Clear();
                            sceneSystem.GraphicsCompositor = null;

                            GraphicsContext.Allocator.ReleaseReference(depthStencil);
                            GraphicsContext.Allocator.ReleaseReference(renderTarget);
                        }

                        MicrothreadLocalDatabases.UnmountDatabase();
                    }
                }
                catch (Exception e)
                {
                    status = ResultStatus.Failed;
                    request.Logger.Error("An exception occurred while processing thumbnail request.", e);
                }
            }

            return status;
        }

        /// <summary>
        /// Request a thumbnail build action to the preview game.
        /// </summary>
        /// <param name="thumbnailUrl">The url of the thumbnail on the storage</param>
        /// <param name="scene">The scene to use to draw the thumbnail</param>
        /// <param name="graphicsCompositor">The graphics compositor used to render the scene</param>
        /// <param name="provider">The file provider to use when executing the build request.</param>
        /// <param name="thumbnailSize">The size of the thumbnail to create</param>
        /// <param name="colorSpace"></param>
        /// <param name="renderingMode">the rendering mode (hdr or ldr).</param>
        /// <param name="logger">The logger</param>
        /// <param name="logLevel">The dependency build status log level</param>
        /// <param name="postProcessThumbnail">The post-process code to customize thumbnail.</param>
        /// <returns>A task on which the user can wait for the thumbnail completion</returns>
        public ResultStatus BuildThumbnail(string thumbnailUrl, Scene scene, GraphicsCompositor graphicsCompositor, DatabaseFileProvider provider, Int2 thumbnailSize, ColorSpace colorSpace, RenderingMode renderingMode, ILogger logger, LogMessageType logLevel, PostProcessThumbnailDelegate postProcessThumbnail = null)
        {
            return ProcessThumbnailRequests(new ThumbnailBuildRequest(thumbnailUrl, scene, graphicsCompositor, provider, thumbnailSize, colorSpace, renderingMode, logger, logLevel) { PostProcessThumbnail = postProcessThumbnail });
        }

        public void Dispose()
        {
            // destroy all game systems
            thumbnailGraphicsCompositors.ForEach(x => x.Dispose());
            sceneSystem.Dispose();
            fontSystem.Dispose();
            EffectSystem.Dispose();
            GraphicsDevice.Dispose();
        }

        public delegate void PostProcessThumbnailDelegate(Image image);
        
        /// <summary>
        /// Class representing a thumbnail build request. 
        /// </summary>
        private class ThumbnailBuildRequest
        {
            /// <summary>
            /// The command context of the build engine that asked for the thumbnail.
            /// </summary>
            public readonly ILogger Logger;

            /// <summary>
            /// The url to where to save the created thumbnail
            /// </summary>
            public readonly string Url;

            /// <summary>
            /// The size of the thumbnail.
            /// </summary>
            public readonly Int2 Size;

            /// <summary>
            /// The color space of the thumbnail
            /// </summary>
            public readonly ColorSpace ColorSpace;

            /// <summary>
            /// The rendering mode used for the thumbnails.
            /// </summary>
            public readonly RenderingMode RenderingMode;

            /// <summary>
            /// The build drawAction to perform
            /// </summary>
            public readonly Scene Scene;

            /// <summary>
            /// The graphics compositor used to render the thumbnail
            /// </summary>
            public readonly GraphicsCompositor GraphicsCompositor;

            /// <summary>
            /// The file provider to use for the request
            /// </summary>
            public readonly DatabaseFileProvider FileProvider;

            /// <summary>
            /// The dependent build step (to check status against)
            /// </summary>
            public readonly LogMessageType DependencyBuildStatus;

            /// <summary>
            /// Post process the thumbnail.
            /// </summary>
            public PostProcessThumbnailDelegate PostProcessThumbnail;

            /// <summary>
            /// Create a new thumbnail request from entity.
            /// </summary>
            /// <param name="thumbnailUrl">The Url of the thumbnail</param>
            /// <param name="scene">The scene describing the thumbnail to draw</param>
            /// <param name="provider">The provider to use for the request.</param>
            /// <param name="thumbnailSize">The desired size of the thumbnail</param>
            /// <param name="colorSpace">The color space.</param>
            /// <param name="renderingMode">the rendering mode (hdr or ldr).</param>
            /// <param name="logger">The logger</param>
            /// <param name="logLevel">The dependency build status log level</param>
            public ThumbnailBuildRequest(string thumbnailUrl, Scene scene, GraphicsCompositor graphicsCompositor, DatabaseFileProvider provider, Int2 thumbnailSize, ColorSpace colorSpace, RenderingMode renderingMode, ILogger logger, LogMessageType logLevel)
            {
                Logger = logger;
                Url = thumbnailUrl;
                Size = thumbnailSize;
                Scene = scene;
                GraphicsCompositor = graphicsCompositor;
                FileProvider = provider;
                DependencyBuildStatus = logLevel;
                ColorSpace = colorSpace;
                RenderingMode = renderingMode;
            }
        }
    }
}

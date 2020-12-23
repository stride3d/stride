// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Assets;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using EditorSettings = Stride.Assets.EditorSettings;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// The base command to build stride thumbnails.
    /// - It uses the underlying <see cref="ThumbnailGenerator"/> to build thumbnail via scenes.
    /// - It extracts and exposes the thumbnail services from the <see cref="ThumbnailCompilerContext"/>,
    /// - It defines the <see cref="CreateScene"/>, <see cref="DestroyScene"/> base functions to be overridden.
    /// </summary>
    public abstract class StrideThumbnailCommand<TRuntimeAsset> : ThumbnailCommand, IThumbnailCommand where TRuntimeAsset : class
    {
        private readonly AssetItem assetItem;
        
        /// <summary>
        /// The compilation context.
        /// </summary>
        protected readonly ThumbnailCompilerContext CompilerContext;

        /// <summary>
        /// The game instance in charge of rendering the thumbnails and the previews.
        /// </summary>
        protected readonly ThumbnailGenerator Generator;

        /// <summary>
        /// The asset object loaded by the game.
        /// </summary>
        protected TRuntimeAsset LoadedAsset;

        protected readonly Color ThumbnailBackgroundColor = Color.FromBgra(0xFF434343);

        protected StrideThumbnailCommand(ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, ThumbnailCommandParameters parameters)
            : base(url, assetItem, parameters, assetFinder)
        {
            CompilerContext = context ?? throw new ArgumentNullException(nameof(context));
            this.assetItem = assetItem;

            // Copy GameSettings ColorSpace/RenderingMode to the parameters
            var gameSettings = context.GetGameSettingsAsset();

            var renderingSettings = gameSettings.GetOrCreate<RenderingSettings>();
            parameters.ColorSpace = renderingSettings.ColorSpace;
            parameters.RenderingMode = gameSettings.GetOrCreate<EditorSettings>().RenderingMode;

            Generator = context.Properties.Get(ThumbnailGenerator.Key) ?? throw new ArgumentException("The provided context does not contain required stride information needed to build the thumbnails.");
        }

        /// <inheritdoc />
        public LogMessageType DependencyBuildStatus { get; set; }

        protected UFile AssetUrl => assetItem.Location;

        /// <summary>
        /// The default font used to build the thumbnails.
        /// </summary>
        protected SpriteFont DefaultFont => Generator.DefaultFont;

        /// <summary>
        /// The graphics device used to build the thumbnails.
        /// </summary>
        protected GraphicsDevice GraphicsDevice => Generator.GraphicsDevice;

        /// <summary>
        /// The list of services available to build the thumbnails.
        /// </summary>
        protected IServiceRegistry Services => Generator.Services;

        /// <summary>
        /// An instance of sprite batch available to draw thumbnails.
        /// </summary>
        protected SpriteBatch SpriteBatch => Generator.SpriteBatch;

        /// <summary>
        /// An instance of ui batch available to draw thumbnails, including SDF fonts.
        /// </summary>
        protected UIBatch UIBatch => Generator.UIBatch;

        /// <summary>
        /// A unique key to identify the shared graphics compositor to use for this command. <see cref="CreateSharedGraphicsCompositor"/> will be called once for each different value of <see cref="GraphicsCompositorKey"/> that exists.
        /// </summary>
        protected abstract string GraphicsCompositorKey { get; }

        /// <inheritdoc/>
        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);
            if (DependencyBuildStatus >= LogMessageType.Warning)
                writer.Write(DependencyBuildStatus);
            var gameSettings = CompilerContext.GetGameSettingsAsset();
            if (gameSettings != null)
            {
                var editorRenderingMode = gameSettings.GetOrCreate<EditorSettings>().RenderingMode;
                var colorSpace = gameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
                writer.Write(editorRenderingMode);
                writer.Write(colorSpace);
            }
        }

        /// <inheritdoc/>
        protected sealed override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            PreloadAsset();
            var graphicsCompositor = GraphicsDevice.GetOrCreateSharedData(GraphicsCompositorKey, CreateSharedGraphicsCompositor);
            var scene = CreateScene(graphicsCompositor);
            var result = Generator.BuildThumbnail(Url, scene, graphicsCompositor, MicrothreadLocalDatabases.DatabaseFileProvider, Parameters.ThumbnailSize, Parameters.ColorSpace, Parameters.RenderingMode, commandContext.Logger, DependencyBuildStatus, CustomizeThumbnail);
            DestroyScene(scene);
            UnloadAsset();
            return Task.FromResult(result);
        }

        /// <summary>
        /// Creates the scene for capturing the thumbnail
        /// </summary>
        /// <param name="graphicsCompositor">The graphics compositor to use to render the scene.</param>
        /// <returns>A new scene ready to be rendered to generate a thumbnail.</returns>
        /// <remarks>The given <paramref name="graphicsCompositor"/> might be shared between multiple command types, hence should not be modified.</remarks>
        protected abstract Scene CreateScene(GraphicsCompositor graphicsCompositor);

        /// <summary>
        /// Creates a new instance of <see cref="GraphicsCompositor"/>, to be shared between all instances of this command.
        /// </summary>
        /// <param name="device">The graphics device in use.</param>
        /// <returns>A new instance of <see cref="GraphicsCompositor"/>.</returns>
        /// <remarks>The returned instance must be a new instance of <see cref="GraphicsCompositor"/>, since its lifetime and disposal is handled by the <see cref="ThumbnailGenerator"/> object.</remarks>
        protected abstract GraphicsCompositor CreateSharedGraphicsCompositor(GraphicsDevice device);

        /// <summary>
        /// Customizes the given image, rendered by the instance of <see cref="ThumbnailGenerator"/>.
        /// </summary>
        /// <param name="image">The image to customize.</param>
        protected virtual void CustomizeThumbnail(Image image)
        {
        }

        /// <summary>
        /// Destroys the scene used to render the thumbnail.
        /// </summary>
        /// <param name="scene">The scene to destroy, corresponding to the scene created earlier by <see cref="CreateScene"/>.</param>
        protected virtual void DestroyScene(Scene scene)
        {
        }

        /// <summary>
        /// Loads the assets that will be needed to render the thumbnails. They must have been built before running this command, therefore the <see cref="BuildStep"/> instances
        /// to build them must be set to be dependencies of this command in the <see cref="ThumbnailCompilerBase{T}.Prepare"/> method.
        /// </summary>
        /// <seealso cref="ThumbnailCompilerBase{T}"/>
        protected virtual void PreloadAsset()
        {
            LoadedAsset = Generator.ContentManager.Load<TRuntimeAsset>(AssetUrl, ContentManagerLoaderSettings.StreamingDisabled);
        }

        /// <summary>
        /// Unloads the assets loaded by <see cref="PreloadAsset"/>.
        /// </summary>
        protected virtual void UnloadAsset()
        {
            UnloadAsset(ref LoadedAsset);
        }

        /// <summary>
        /// Unloads safely the given asset, if it's not null.
        /// </summary>
        protected void UnloadAsset<T>(ref T loadedAsset) where T : class
        {
            if (loadedAsset != null)
            {
                Generator.ContentManager.Unload(loadedAsset);
                loadedAsset = null;
            }
        }
    }
}

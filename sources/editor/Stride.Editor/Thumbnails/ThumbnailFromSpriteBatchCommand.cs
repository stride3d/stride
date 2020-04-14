// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Assets;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Editor.Thumbnails
{
    internal interface IThumbnailFromSpriteBatchCommand
    {
        void RenderSprites(RenderDrawContext context);
    }

    internal static class ThumbnailFromSpriteBatchCommand
    {
        public static readonly PropertyKey<IThumbnailFromSpriteBatchCommand> Key = new PropertyKey<IThumbnailFromSpriteBatchCommand>(nameof(ThumbnailFromSpriteBatchCommand), typeof(ThumbnailFromSpriteBatchCommand));
    }

    /// <summary>
    /// A command that creates the thumbnail using the sprite batch
    /// </summary>
    /// <typeparam name="TRuntimeAsset">The type of the runtime object asset to load</typeparam>
    public abstract class ThumbnailFromSpriteBatchCommand<TRuntimeAsset> : XenkoThumbnailCommand<TRuntimeAsset>, IThumbnailFromSpriteBatchCommand where TRuntimeAsset : class
    {
        private static readonly string ThumbnailSpriteBatchGraphicsCompositorKey = nameof(ThumbnailSpriteBatchGraphicsCompositorKey);
        private readonly Scene spriteScene;
        protected EffectInstance EffectInstance;

        protected ThumbnailFromSpriteBatchCommand(ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, ThumbnailCommandParameters parameters)
            : base(context, assetItem, assetFinder, url, parameters)
        {
            spriteScene = new Scene();
            spriteScene.Tags.Add(ThumbnailFromSpriteBatchCommand.Key, this);
            EffectInstance = null;
            // Always render spritebatch in LDR mode
            Parameters.RenderingMode = RenderingMode.LDR;
        }

        /// <inheritdoc/>
        protected override string GraphicsCompositorKey => ThumbnailSpriteBatchGraphicsCompositorKey;

        /// <inheritdoc/>
        protected override Scene CreateScene(GraphicsCompositor graphicsCompositor)
        {
            return spriteScene;
        }

        /// <inheritdoc/>
        protected override GraphicsCompositor CreateSharedGraphicsCompositor(GraphicsDevice device)
        {
            return new GraphicsCompositor
            {
                Game = new SceneRendererCollection
                {
                    new ClearRenderer { Color = ThumbnailBackgroundColor },
                    new DelegateSceneRenderer(SafeRenderSprites),
                }
            };
        }

        private static void SafeRenderSprites(RenderDrawContext context)
        {
            // Note: this assumes that the Scene returned by CreateScene is the first child scene of the RootScene. Changing this in ThumbnailGenerator will break this code!
            var command = SceneInstance.GetCurrent(context.RenderContext).RootScene.Children.First().Tags.Get(ThumbnailFromSpriteBatchCommand.Key);
            command.RenderSprites(context);
        }

        /// <summary>
        /// Renders the sprites for this thumbnail.
        /// </summary>
        /// <param name="context">The <see cref="RenderDrawContext"/> to use to render the sprites.</param>
        protected abstract void RenderSprites(RenderDrawContext context);

        /// <inheritdoc/>
        void IThumbnailFromSpriteBatchCommand.RenderSprites(RenderDrawContext context)
        {
            try
            {
                // draws
                SpriteBatch.Begin(context.GraphicsContext, SpriteSortMode.Deferred, null, null, null, null, EffectInstance);
                RenderSprites(context);
            }
            finally
            {
                SpriteBatch.End();
            }
        }
    }
}

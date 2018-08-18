// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Editor.Thumbnails
{
    /// <summary>
    /// A command that creates the thumbnail using a background texture and title text
    /// </summary>
    /// <typeparam name="TRuntimeAsset">The type of the runtime object asset to load</typeparam>
    public abstract class ThumbnailFromTextureCommand<TRuntimeAsset> : ThumbnailFromSpriteBatchCommand<TRuntimeAsset> where TRuntimeAsset : class
    {
        /// <summary>
        /// The background image used when taking the thumbnail.
        /// </summary>
        protected Texture BackgroundTexture;

        /// <summary>
        /// The color used when rendering the thumbnail background.
        /// </summary>
        protected Color BackgroundColor = Color.White;

        /// <summary>
        /// The color added to the background when rendering the thumbnail.
        /// </summary>
        protected Color AdditiveColor = new Color(0, 0, 0, 0);

        /// <summary>
        /// Swizzle mode for sampling (RGBA, RRRR, RRR1)
        /// </summary>
        protected SwizzleMode Swizzle = SwizzleMode.None;

        /// <summary>
        /// The font used when taking the thumbnail.
        /// </summary>
        protected SpriteFont Font;

        /// <summary>
        /// The color used when rendering the thumbnail text.
        /// </summary>
        protected Color FontColor = Color.White;

        /// <summary>
        /// The size used when rendering the font.
        /// </summary>
        protected float FontSize = 0.95f;

        /// <summary>
        /// The text to draw when taking the thumbnail.
        /// </summary>
        protected string TitleText;

        protected ThumbnailFromTextureCommand(ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, ThumbnailCommandParameters parameters)
            : base(context, assetItem, assetFinder, url, parameters)
        {
        }

        protected override Scene CreateScene(GraphicsCompositor graphicsCompositor)
        {
            SetThumbnailParameters();

            return base.CreateScene(graphicsCompositor);
        }

        protected abstract void SetThumbnailParameters();

        protected override void RenderSprites(RenderDrawContext context)
        {
            var thumbnailSize = new Vector2(context.CommandList.RenderTarget.ViewWidth, context.CommandList.RenderTarget.ViewHeight);

            // the background texture
            if (BackgroundTexture != null)
                SpriteBatch.Draw(BackgroundTexture, new RectangleF(0, 0, thumbnailSize.X, thumbnailSize.Y), null, BackgroundColor, 0, Vector2.Zero, SpriteEffects.None, ImageOrientation.AsIs, 0, AdditiveColor, Swizzle);

            if (Font != null)
            {
                // Measure the type name to draw and calculate the scale factor needed for the name to enter the thumbnail
                var typeNameSize = Font.MeasureString(TitleText);
                var scale = FontSize * Math.Min(thumbnailSize.X / typeNameSize.X, thumbnailSize.Y / typeNameSize.Y);
                var desiredFontSize = scale * Font.Size;

                if (Font.FontType == SpriteFontType.Dynamic)
                {
                    scale = 1f;

                    // Get the exact size of the font rendered with the desired size
                    typeNameSize = Font.MeasureString(TitleText, desiredFontSize);

                    // force pre-generation of the glyph
                    Font.PreGenerateGlyphs(TitleText, new Vector2(desiredFontSize, desiredFontSize));
                }

                // the title text
                SpriteBatch.DrawString(Font, TitleText, desiredFontSize, thumbnailSize/2, FontColor, 0, typeNameSize/2, scale*Vector2.One, SpriteEffects.None, 1, TextAlignment.Center);
            }
        }
    }
}

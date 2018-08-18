// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using XenkoEffects;

using Xenko.Core.BuildEngine;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Assets.Presentation.Preview.Views;
using Xenko.Assets.Textures;
using Xenko.Editor.Preview;
using Xenko.Rendering;
using Xenko.Graphics;

namespace Xenko.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview textures.
    /// </summary>
    [AssetPreview(typeof(TextureAsset), typeof(TexturePreviewView))]
    public class TexturePreview : PreviewFromSpriteBatch<TextureAsset>
    {
        private readonly Dictionary<TextureCubePreviewMode, Texture> textureCubeViews = new Dictionary<TextureCubePreviewMode, Texture>();
        private readonly DynamicEffectInstance currentEffect;
        
        private Texture texture;
        private BlendStateDescription adequateBlendState;
        private SamplerState specificMipmapSamplerState;
        private Rectangle textureSourceRegion;

        private TextureCubePreviewMode textureCubePreviewMode = TextureCubePreviewMode.Full;
        
        public int TextureWidth => texture?.Width ?? 0;

        public int TextureHeight => texture?.Height ?? 0;

        public int TextureDepth => texture?.Depth ?? 0;

        /// <summary>
        /// Gets or sets a callback that will be invoked when the texture is loaded.
        /// </summary>
        public Action NotifyTextureLoaded { get; set; }

        public TextureDimension Dimension => texture?.ViewDimension ?? default(TextureDimension);

        public int SliceCount => texture?.ArraySize ?? -1;

        public TexturePreview()
        {
            currentEffect = new DynamicEffectInstance("PreviewTexture");
        }

        protected override Task Initialize()
        {
            specificMipmapSamplerState = SamplerState.New(Game.GraphicsDevice, new SamplerStateDescription(TextureFilter.ComparisonPoint, TextureAddressMode.Clamp));

            currentEffect.Initialize(Game.Services);

            return base.Initialize();
        }

        public override IEnumerable<int> GetAvailableMipMaps()
        {
            yield return 0;

            if (texture == null)
                yield break;

            for (var i = 1; i < texture.Description.MipLevels; ++i)
                yield return i;
        }

        public override void DisplayMipMap(int level)
        {
            var samplerDescrition = new SamplerStateDescription(TextureFilter.ComparisonPoint, TextureAddressMode.Clamp)
            {
                MinMipLevel = level,
                MaxMipLevel = level
            };

            specificMipmapSamplerState.Dispose();
            specificMipmapSamplerState = SamplerState.New(Game.GraphicsDevice, samplerDescrition);
        }

        /// <inheritdoc/>
        protected override ColorSpace DetermineColorSpace()
        {
            // Project color space
            var colorSpace = base.DetermineColorSpace();

            // Compute color space to use during rendering with hint and color space set on texture
            var textureAsset = (TextureAsset)AssetItem.Asset;
            colorSpace = textureAsset.Type.IsSRgb(colorSpace) ? ColorSpace.Linear : ColorSpace.Gamma;

            return colorSpace;
        }

        protected SwizzleMode Swizzle()
        {
            var textureAsset = (TextureAsset)AssetItem.Asset;

            if (textureAsset.Type.Hint == TextureHint.Grayscale)
                return SwizzleMode.RRR1;
            
            if (textureAsset.Type.Hint == TextureHint.NormalMap)
                return SwizzleMode.NormalMap;

            return SwizzleMode.None;
        }

        protected override Vector2 SpriteSize
        {
            get
            {
                if (texture == null)
                    return base.SpriteSize;

                var textureSize = new Vector2(texture.Width, texture.Height);

                if (texture.ViewDimension == TextureDimension.TextureCube && textureCubePreviewMode == TextureCubePreviewMode.Full)
                {
                    textureSize.X *= 4;
                    textureSize.Y *= 3;
                }

                return textureSize;
            }
        }

        public void SetCubePreviewMode(TextureCubePreviewMode mode)
        {
            textureCubePreviewMode = mode;
        }

        public void SetDepthToPreview(float depthValue)
        {
            var sliceCoordinate = 0f;

            if (texture != null && texture.Depth > 1)
                sliceCoordinate = Math.Min(1f, depthValue / (texture.Depth - 1f));

            currentEffect.Parameters.Set(Sprite3DBaseKeys.SliceCoordinate, sliceCoordinate);
        }

        protected override void LoadContent()
        {
            // determine the adequate blend state to render the font
            adequateBlendState = Asset.Type.PremultiplyAlpha ? BlendStates.AlphaBlend : BlendStates.NonPremultiplied;

            // Load the texture (but don't use streaming so it will be fully loaded)
            texture = LoadAsset<Texture>(AssetItem.Location, ContentManagerLoaderSettings.StreamingDisabled);
            NotifyTextureLoaded?.Invoke();

            // Update the effect
            MicrothreadLocalDatabases.MountCommonDatabase();
            currentEffect.Parameters.Set(PreviewTextureParameters.Is3D, texture.ViewDimension == TextureDimension.Texture3D);

            // create texture views for cube textures.
            if (texture.ViewDimension == TextureDimension.TextureCube)
            {
                for (int i = 0; i < texture.ArraySize; i++)
                    textureCubeViews[(TextureCubePreviewMode)i] = texture.ToTextureView(new TextureViewDescription { ArraySlice = i, Type = ViewType.ArrayBand });
            }

            textureSourceRegion = new Rectangle(0, 0, TextureWidth, TextureHeight);

            // TODO: Return LDR or HDR depending on texture bits (16bits is most likely HDR)
            RenderingMode = RenderingMode.LDR;
        }

        protected override void UnloadContent()
        {
            foreach (var textureView in textureCubeViews.Values)
            {
                textureView.Dispose();
            }
            textureCubeViews.Clear();

            if (texture != null)
            {
                UnloadAsset(texture);
                texture = null;
            }
        }

        public override async Task Dispose()
        {
            await base.Dispose();

            specificMipmapSamplerState.Dispose();
        }

        protected override void RenderSprite()
        {
            if (texture == null)
                return;

            var origin = SpriteSize / 2 - SpriteOffsets;
            var position = WindowSize / 2;
            var color = new Color(1f, 1f, 1f, 1f);

            var swizzle = Swizzle();
            var colorAdd = new Color4(0, 0, 0, 0);
            var layerDepth = 0f;

            SpriteBatch.Begin(Game.GraphicsContext, SpriteSortMode.Texture, adequateBlendState, specificMipmapSamplerState, effect: currentEffect);

            if (texture.ViewDimension == TextureDimension.Texture2D)
            {
                SpriteBatch.Draw(texture, position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);
            }
            else if (texture.ViewDimension == TextureDimension.TextureCube)
            {
                if (textureCubePreviewMode == TextureCubePreviewMode.Full)
                {
                    origin.X -= TextureWidth;
                    SpriteBatch.Draw(textureCubeViews[TextureCubePreviewMode.Top], position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);

                    origin.X += TextureWidth;
                    origin.Y -= TextureHeight;
                    SpriteBatch.Draw(textureCubeViews[TextureCubePreviewMode.Left], position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);

                    origin.X -= TextureWidth;
                    SpriteBatch.Draw(textureCubeViews[TextureCubePreviewMode.Front], position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);

                    origin.X -= TextureWidth;
                    SpriteBatch.Draw(textureCubeViews[TextureCubePreviewMode.Right], position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);

                    origin.X -= TextureWidth;
                    SpriteBatch.Draw(textureCubeViews[TextureCubePreviewMode.Back], position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);

                    origin.X += 2 * TextureWidth;
                    origin.Y -= TextureHeight;
                    SpriteBatch.Draw(textureCubeViews[TextureCubePreviewMode.Bottom], position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);
                }
                else
                {
                    SpriteBatch.Draw(textureCubeViews[textureCubePreviewMode], position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);
                }
            }
            else if (texture.ViewDimension == TextureDimension.Texture3D)
            {
                SpriteBatch.Draw(texture, position, textureSourceRegion, color, 0, origin, SpriteScale, SpriteEffects.None, ImageOrientation.AsIs, layerDepth, colorAdd, swizzle);
            }
            
            SpriteBatch.End();
        }
    }
}

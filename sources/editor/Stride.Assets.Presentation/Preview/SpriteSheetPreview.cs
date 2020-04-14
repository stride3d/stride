// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.Sprite;
using Stride.Assets.Textures;
using Stride.Editor.Preview;
using Stride.Graphics;

namespace Stride.Assets.Presentation.Preview
{
    public enum SpriteSheetDisplayMode
    {
        [Display("Sprites")]
        Sprites,
        [Display("Sprite textures")]
        SpriteTextures,
    }

    /// <summary>
    /// A base preview implementation for all asset inheriting from <see cref="SpriteSheetAsset"/>.
    /// </summary>
    [AssetPreview(typeof(SpriteSheetAsset), typeof(SpriteSheetPreviewView))]
    public class SpriteSheetPreview : PreviewFromSpriteBatch<SpriteSheetAsset>
    {
        protected SpriteSheet SpriteSheet;
        private BlendStateDescription adequateBlendState;
        private SpriteSheetDisplayMode mode;
        private List<Texture> spriteTextures;

        /// <summary>
        /// Gets or sets the mode of the display of the preview
        /// </summary>
        public SpriteSheetDisplayMode Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                CurrentFrame = Math.Min(CurrentFrame, FrameCount - 1);
                FitOnScreen();
            }
        }

        /// <summary>
        /// Gets the number of frames in the sprite.
        /// </summary>
        public int FrameCount
        {
            get
            {
                if (SpriteSheet?.Sprites == null)
                    return 0;

                switch (Mode)
                {
                    case SpriteSheetDisplayMode.Sprites:
                        return SpriteSheet.Sprites.Count;
                    case SpriteSheetDisplayMode.SpriteTextures:
                        return spriteTextures.Count;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the frame being currently previewed.
        /// </summary>
        public int CurrentFrame { get; set; }

        protected override Vector2 SpriteSize
        {
            get
            {
                if (SpriteSheet?.Sprites == null || SpriteSheet.Sprites.Count == 0)
                    return base.SpriteSize;

                // return the max of all sprites of the group in order to be able to pass 
                // from one element to other in the preview without having to change zooming
                var max = Vector2.Zero;
                foreach (var image in SpriteSheet.Sprites)
                {
                    var width = 0f;
                    var height = 0f;

                    if (Mode == SpriteSheetDisplayMode.Sprites)
                    {
                        width = image.SizeInPixels.X;
                        height = image.SizeInPixels.Y;
                    }
                    else
                    {
                        if (image.Texture != null)
                        {
                            width = image.Texture.Width;
                            height = image.Texture.Height;
                        }
                    }

                    max[0] = Math.Max(max[0], width);
                    max[1] = Math.Max(max[1], height);
                }
                return max;
            }
        }

        protected virtual Vector2 ImageCenter
        {
            get
            {
                if (SpriteSheet?.Sprites == null || SpriteSheet.Sprites.Count == 0)
                    return Vector2.Zero;

                Vector2 imageSize;
                if (Mode == SpriteSheetDisplayMode.Sprites)
                {
                    var image = SpriteSheet.Sprites[CurrentFrame];
                    imageSize = new Vector2(image.Region.Width, image.Region.Height);
                }
                else
                {
                    var image = spriteTextures[CurrentFrame];
                    imageSize = new Vector2(image.Width, image.Height);
                }

                return imageSize / 2f;
            }
        }

        protected override void LoadContent()
        {
            SpriteSheet = LoadAsset<SpriteSheet>(AssetItem.Location);
            spriteTextures = new List<Texture>();
            if (SpriteSheet?.Sprites != null)
                spriteTextures.AddRange(SpriteSheet.Sprites.Select(s => s.Texture).Distinct());

            // look for the texture asset to determine the blend state
            var spriteGroupItem = AssetItem.Package.Session.FindAsset(Asset.Id);
            var spriteGroupAsset = (SpriteSheetAsset)spriteGroupItem.Asset;
            adequateBlendState = BlendStates.Opaque;
            if (spriteGroupAsset.Alpha != AlphaFormat.None)
                adequateBlendState = spriteGroupAsset.PremultiplyAlpha ? BlendStates.AlphaBlend : BlendStates.NonPremultiplied;

            // Always use LDR
            RenderingMode = RenderingMode.LDR;
        }

        protected override void UnloadContent()
        {
            if (SpriteSheet != null)
            {
                UnloadAsset(SpriteSheet);
                SpriteSheet = null;
                spriteTextures = null;
            }
        }

        protected override void RenderSprite()
        {
            if (SpriteSheet?.Sprites == null || SpriteSheet.Sprites.Count == 0)
                return;

            // check that the current texture is not null
            var texture = Mode == SpriteSheetDisplayMode.Sprites ? SpriteSheet.Sprites[CurrentFrame].Texture : spriteTextures[CurrentFrame];
            if (texture == null)
                return;

            var origin = ImageCenter - SpriteOffsets;
            var image = SpriteSheet.Sprites[CurrentFrame];
            var region = Mode == SpriteSheetDisplayMode.Sprites ? image.Region : new RectangleF(0, 0, texture.Width, texture.Height);
            var orientation = Mode == SpriteSheetDisplayMode.Sprites ? image.Orientation : ImageOrientation.AsIs;

            SpriteBatch.Begin(Game.GraphicsContext, SpriteSortMode.Texture, adequateBlendState);
            SpriteBatch.Draw(texture, WindowSize / 2, region, Color.White, 0, origin, SpriteScale * Vector2.One, SpriteEffects.None, orientation);
            SpriteBatch.End();
        }
    }
}

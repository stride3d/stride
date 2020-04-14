// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Preview
{
    public abstract class FontPreview<T> : PreviewFromSpriteBatch<T> where T: Asset
    {
        private string previewText = "";

        private BlendStateDescription adequateBlendState;

        private Graphics.SpriteFont spriteFont;

        private static EffectInstance sdfFontEffect;

        private static EffectInstance GetSDFFontEffect(GraphicsDevice device)
        {
            return sdfFontEffect ?? (sdfFontEffect = new EffectInstance(new Graphics.Effect(device, SpriteSignedDistanceFieldFontShader.Bytecode) { Name = "SDFFontEffectAssetPreview" }));
        }

        /// <summary>
        /// Sets a string to preview using the sprite font. If the given string is null or empty, it will display the
        /// sprite font texture instead.
        /// </summary>
        /// <param name="str">The string to preview.</param>
        public void SetPreviewString(string str)
        {
            previewText = str;
        }

        protected abstract bool IsFontNotPremultiplied();

        protected override void LoadContent()
        {
            // determine the adequate blend state to render the font
            adequateBlendState = IsFontNotPremultiplied()? BlendStates.NonPremultiplied : BlendStates.AlphaBlend;

            // load the sprite font
            spriteFont = LoadAsset<Graphics.SpriteFont>(AssetItem.Location);

            // Always use LDR for fonts
            RenderingMode = RenderingMode.LDR;
        }

        protected override void UnloadContent()
        {
            if (spriteFont != null)
            {
                UnloadAsset(spriteFont);
                spriteFont = null;
            }
        }

        protected override void RenderSprite()
        {
            if (spriteFont == null)
                return;

            var textToDisplay = string.IsNullOrEmpty(previewText) ? "Enter the text to preview" : previewText;

            var textSize = spriteFont.MeasureString(textToDisplay);
            var windowSize = new Vector2(Game.Window.ClientBounds.Width, Game.Window.ClientBounds.Height);
            var position = SpriteOffsets + (windowSize - textSize) / 2;

            var effectInstance = (spriteFont.FontType == SpriteFontType.SDF) ? GetSDFFontEffect(Game.GraphicsDevice) : null;

            SpriteBatch.Begin(Game.GraphicsContext, SpriteSortMode.Texture, adequateBlendState, null, null, null, effectInstance);
            SpriteBatch.DrawString(spriteFont, textToDisplay, position, Color.White);
            SpriteBatch.End();
        }
    }
}

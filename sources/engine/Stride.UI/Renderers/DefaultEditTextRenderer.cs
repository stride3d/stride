// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.UI.Controls;
using IServiceRegistry = Stride.Core.IServiceRegistry;
using Vector3 = Stride.Core.Mathematics.Vector3;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="EditText"/>.
    /// </summary>
    internal class DefaultEditTextRenderer : ElementRenderer
    {
        public DefaultEditTextRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        private void RenderSelection([NotNull] EditText editText, [NotNull] UIRenderingContext context, in Vector2 textRegion, int start, int length, Color color, out Matrix caret, out float caretHeight)
        {
            var snapText = context.ShouldSnapText && !editText.DoNotSnapText;
            var requestedFontSize = editText.ActualTextSize;
            var realVirtualResolutionRatio = editText.LayoutingContext.RealVirtualResolutionRatio;
            var font = editText.Font;

            font.TypeSpecificRatios(requestedFontSize, ref snapText, ref realVirtualResolutionRatio, out var fontSize);

            var totalSize = Vector2.Zero;
            var sizeToSelection = Vector2.Zero;
            var sizeToEnd = Vector2.Zero;
            foreach (var glyphInfo in new SpriteFont.GlyphEnumerator(null, new SpriteFont.StringProxy(editText.TextToDisplay), fontSize, false, 0, editText.TextToDisplay.Length, font))
            {
                font.MeasureStringGlyph(ref totalSize, in fontSize, glyphInfo);
                if (glyphInfo.index < start)
                    sizeToEnd = sizeToSelection = totalSize;
                else if (glyphInfo.index < start + length)
                    sizeToEnd = totalSize;
            }

            float signedAlignment = editText.TextAlignment switch
            {
                TextAlignment.Left => -1f,
                TextAlignment.Center => 0f,
                TextAlignment.Right => 1f,
                _ => throw new ArgumentOutOfRangeException()
            };

            var regionHalf = textRegion / 2;
            var lineHalf = totalSize / 2;
            var selectionRect = new Vector3(sizeToEnd.X - sizeToSelection.X, totalSize.Y, 0);

            Vector2 offset2D;
            offset2D.Y = -regionHalf.Y; // Top box corner
            offset2D.Y += lineHalf.Y; // Align with top of the text
            
            offset2D.X = regionHalf.X * signedAlignment; // Which side corner to align to
            offset2D.X -= lineHalf.X * signedAlignment; // Align with the left or the right of the text
            offset2D.X -= lineHalf.X; // Rect grows from the center, let's start from the left edge,
            offset2D.X += sizeToSelection.X; // Move to the start of the selection

            var worldMatrix = editText.WorldMatrixInternal;
            worldMatrix.TranslationVector += worldMatrix.Right * offset2D.X + worldMatrix.Up * offset2D.Y;
            caret = worldMatrix;

            worldMatrix.TranslationVector += worldMatrix.Right * (selectionRect.X / 2); // Move it by half its expected size since the rect is supposed to be centered
            
            Batch.DrawRectangle(ref worldMatrix, ref selectionRect, ref color, context.DepthBias + 1);

            caretHeight = totalSize.Y;
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var editText = (EditText)element;

            if (editText.Font == null)
                return;
            
            // determine the image to draw in background of the edit text
            var fontScale = element.LayoutingContext.RealVirtualResolutionRatio;
            var color = editText.RenderOpacity * Color.White;
            var provider = editText.IsSelectionActive ? editText.ActiveImage : editText.MouseOverState == MouseOverState.MouseOverElement ? editText.MouseOverImage : editText.InactiveImage;
            var image = provider?.GetSprite();

            if (image?.Texture != null)
            {
                Batch.DrawImage(image.Texture, ref editText.WorldMatrixInternal, ref image.RegionInternal, ref editText.RenderSizeInternal, ref image.BordersInternal, ref color, context.DepthBias, image.Orientation);
            }
            
            // calculate the size of the text region by removing padding
            var textRegionSize = editText.GetTextRegionSize();

            var font = editText.Font;
            var caretColor = editText.RenderOpacity * editText.CaretColor;

            var caretMatrix = Matrix.Identity;
            var caretHeight = 0f;

            // Draw the composition selection
            if (editText.Composition.Length > 0)
            {
                var imeSelectionColor = editText.RenderOpacity * editText.IMESelectionColor;
                RenderSelection(editText, context, textRegionSize, editText.SelectionStart, editText.Composition.Length, imeSelectionColor, out caretMatrix, out caretHeight);
            }
            // Draw the regular selection
            else if (editText.IsSelectionActive)
            {
                var selectionColor = editText.RenderOpacity * editText.SelectionColor;
                RenderSelection(editText, context, textRegionSize, editText.SelectionStart, editText.SelectionLength, selectionColor, out caretMatrix, out caretHeight);
            }

            // create the text draw command
            var drawCommand = new SpriteFont.InternalUIDrawCommand
            {
                Color = editText.RenderOpacity * editText.TextColor,
                DepthBias = context.DepthBias + 2,
                RealVirtualResolutionRatio = fontScale,
                RequestedFontSize = editText.ActualTextSize,
                Batch = Batch,
                SnapText = context.ShouldSnapText && !editText.DoNotSnapText,
                Matrix = editText.WorldMatrixInternal,
                Alignment = editText.TextAlignment,
                TextBoxSize = textRegionSize
            };

            if (editText.Font.FontType == SpriteFontType.SDF)
            {
                Batch.End();
                Batch.BeginCustom(context.GraphicsContext, 1);
            }

            // Draw the text
            Batch.DrawString(font, editText.TextToDisplay, ref drawCommand);

            if (editText.Font.FontType == SpriteFontType.SDF)
            {
                Batch.End();
                Batch.BeginCustom(context.GraphicsContext, 0);
            }

            // Draw the cursor
            if (editText.IsCaretVisible)
            {
                var caretScaleVector = new Vector3(editText.CaretWidth / fontScale.X, caretHeight, 0);
                Batch.DrawRectangle(ref caretMatrix, ref caretScaleVector, ref caretColor, context.DepthBias + 3);
            }
        }
    }
}

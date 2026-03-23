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

            var lineHeight = font.GetTotalLineSpacing(fontSize.Y);

            var worldMatrix = editText.WorldMatrixInternal;
            worldMatrix.TranslationVector -= worldMatrix.Right * textRegion.X * 0.5f + worldMatrix.Up * (textRegion.Y * 0.5f - lineHeight * 0.5f);

            Vector2 selectionStart = default, selectionEnd = default, lineStart = default, lineEnd = default;
            var end = start + length;
            foreach (var glyphInfo in new SpriteFont.GlyphEnumerator(null, new SpriteFont.StringProxy(editText.TextToDisplay), fontSize, false, 0, editText.TextToDisplay.Length, font, (editText.TextAlignment, textRegion)))
            {
                if (glyphInfo.Index < start)
                {
                    lineEnd = lineStart = selectionEnd = selectionStart = new Vector2(glyphInfo.NextX, glyphInfo.Position.Y);
                }
                else if (glyphInfo.Index == start)
                {
                    lineStart = selectionEnd = selectionStart = glyphInfo.Position;
                    lineEnd = new Vector2(glyphInfo.NextX, glyphInfo.Position.Y);
                }
                else if (glyphInfo.Index <= end)
                {
                    // We're between start and end
                    if (lineStart.Y != glyphInfo.Y) // Skipped a line, draw a selection rect between the edges of the previous line
                    {
                        DrawSelectionOnGlyphRange(context, color, worldMatrix, lineStart, lineEnd, lineHeight);
                        lineStart = glyphInfo.Position;
                    }

                    lineEnd = new Vector2(glyphInfo.NextX, glyphInfo.Position.Y);
                    if (glyphInfo.Index < end)
                        selectionEnd = new Vector2(glyphInfo.NextX, glyphInfo.Position.Y);
                    else
                        selectionEnd = glyphInfo.Position;
                }
                else
                {
                    break;
                }
            }

            if (end == editText.TextToDisplay.Length) // Edge case for single character selected at the end of a string
            {
                selectionEnd.X = lineEnd.X;
            }

            DrawSelectionOnGlyphRange(context, color, worldMatrix, lineStart, selectionEnd, lineHeight);

            caretHeight = lineHeight;
            caret = worldMatrix;
            caret.TranslationVector += caret.Right * selectionStart.X + caret.Up * selectionStart.Y;
        }

        private void DrawSelectionOnGlyphRange(UIRenderingContext context, Color color, in Matrix worldMatrix, Vector2 start, Vector2 end, float lineHeight)
        {
            var tempMatrix = worldMatrix;
            var selectionRect = new Vector3(end.X - start.X, lineHeight, 0);
            tempMatrix.TranslationVector += worldMatrix.Right * (start.X + selectionRect.X * 0.5f) + worldMatrix.Up * start.Y;
            Batch.DrawRectangle(ref tempMatrix, ref selectionRect, ref color, context.DepthBias + 1);
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

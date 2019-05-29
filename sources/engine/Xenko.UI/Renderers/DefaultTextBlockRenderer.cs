// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.Font;
using Xenko.UI.Controls;

namespace Xenko.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="TextBlock"/>.
    /// </summary>
    internal class DefaultTextBlockRenderer : ElementRenderer
    {
        public DefaultTextBlockRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var textBlock = (TextBlock)element;

            if (textBlock.Font == null || textBlock.TextToDisplay == null)
                return;

            var drawCommand = new SpriteFont.InternalUIDrawCommand {
                Color = textBlock.RenderOpacity * textBlock.TextColor,
                DepthBias = context.DepthBias,
                RealVirtualResolutionRatio = element.LayoutingContext.RealVirtualResolutionRatio,
                RequestedFontSize = textBlock.ActualTextSize,
                Batch = Batch,
                SnapText = context.ShouldSnapText && !textBlock.DoNotSnapText,
                Matrix = textBlock.WorldMatrixInternal,
                Alignment = textBlock.TextAlignment,
                VertAlignment = textBlock.TextVerticalAlignment,
                TextBoxSize = new Vector2(textBlock.ActualWidth, textBlock.ActualHeight),
                IsFullscreen = context.IsFullscreen
            };

            if (textBlock.Font.FontType == SpriteFontType.SDF)
            {
                Batch.End();

                Batch.BeginCustom(context.GraphicsContext, 1);                
            }

            Batch.DrawString(textBlock.Font, textBlock.TextToDisplay, ref drawCommand);

            if (textBlock.Font.FontType == SpriteFontType.SDF)
            {
                Batch.End();

                Batch.BeginCustom(context.GraphicsContext, 0);
            }
        }
    }
}

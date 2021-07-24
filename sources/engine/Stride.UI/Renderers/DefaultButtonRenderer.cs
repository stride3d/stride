// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="Button"/>.
    /// </summary>
    internal class DefaultButtonRenderer : ElementRenderer
    {
        public DefaultButtonRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var button = (Button)element;
            var sprite = button.ButtonImage;
            if (sprite?.Texture == null)
                return;

            var color = element.RenderOpacity * Color.White;
            Batch.DrawImage(sprite.Texture, ref element.WorldMatrixInternal, ref sprite.RegionInternal, ref element.RenderSizeInternal, ref sprite.BordersInternal, ref color, context.DepthBias, sprite.Orientation);
        }
    }
}

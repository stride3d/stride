// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.UI.Controls;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="ImageElement"/>.
    /// </summary>
    internal class DefaultImageRenderer : ElementRenderer
    {
        public DefaultImageRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var image = (ImageElement)element;
            var sprite = image.Source?.GetSprite();
            if (sprite?.Texture == null)
                return;

            var color = element.RenderOpacity * image.Color;
            Batch.DrawImage(sprite.Texture, ref element.WorldMatrixInternal, ref sprite.RegionInternal, ref element.RenderSizeInternal, ref sprite.BordersInternal, ref color, context.DepthBias, sprite.Orientation);
        }
    }
}

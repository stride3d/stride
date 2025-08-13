// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="ContentDecorator"/>.
    /// </summary>
    internal class DefaultContentDecoratorRenderer : ElementRenderer
    {
        public DefaultContentDecoratorRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var decorator = (ContentDecorator)element;
            var sprite = decorator.BackgroundImage?.GetSprite();
            if (sprite?.Texture == null)
                return;

            var color = element.RenderOpacity * Color.White;
            var size = new Vector3(element.RenderSizeInternal.Width, element.RenderSizeInternal.Height, 1);
            Batch.DrawImage(sprite.Texture, ref element.WorldMatrixInternal, ref sprite.RegionInternal, ref size, ref sprite.BordersInternal, ref color, context.DepthBias, sprite.Orientation);
        }
    }
}

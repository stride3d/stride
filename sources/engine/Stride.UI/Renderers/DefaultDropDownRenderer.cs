// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="DropDown"/>.
    /// </summary>
    internal class DefaultDropDownRenderer : ElementRenderer
    {
        public DefaultDropDownRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var dropDown = (DropDown)element;

            var headerSprite = dropDown.HeaderImage;
            if (headerSprite?.Texture != null)
            {
                var color = element.RenderOpacity * dropDown.HeaderColor;
                Batch.DrawImage(headerSprite.Texture, ref element.WorldMatrixInternal, ref headerSprite.RegionInternal, ref element.RenderSizeInternal, ref headerSprite.BordersInternal, ref color, context.DepthBias, headerSprite.Orientation);
            }

            context.DepthBias += 1;

            if (!dropDown.IsOpen)
                return;

            var listSprite = dropDown.ListBackgroundSprite;
            if (listSprite?.Texture != null)
            {
                var listColor = element.RenderOpacity * dropDown.ListColor;
                Batch.DrawImage(listSprite.Texture, ref dropDown.PopupWorldMatrix, ref listSprite.RegionInternal, ref dropDown.PopupRenderSize, ref listSprite.BordersInternal, ref listColor, context.DepthBias, listSprite.Orientation);
            }

            context.DepthBias += 1;
        }
    }
}

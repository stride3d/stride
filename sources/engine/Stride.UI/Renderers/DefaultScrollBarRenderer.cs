// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core;
using Stride.UI.Controls;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="ScrollBar"/>.
    /// </summary>
    internal class DefaultScrollBarRenderer : ElementRenderer
    {
        public DefaultScrollBarRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);
            
            var bar = (ScrollBar)element;

            // round the size of the bar to nearest pixel modulo to avoid to have a bar varying by one pixel length while scrolling
            var barSize = bar.RenderSizeInternal;
            var realVirtualRatio = bar.LayoutingContext.RealVirtualResolutionRatio;
            for (var i = 0; i < 2; i++)
                barSize[i] = (float)(Math.Ceiling(barSize[i] * realVirtualRatio[i]) / realVirtualRatio[i]);
            
            Batch.DrawRectangle(ref element.WorldMatrixInternal, ref barSize, ref bar.BarColorInternal, context.DepthBias);
        }
    }
}

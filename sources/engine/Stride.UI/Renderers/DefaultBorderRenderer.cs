// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.


using Stride.Core;
using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="Border"/>.
    /// </summary>
    public class DefaultBorderRenderer : ElementRenderer
    {
        public DefaultBorderRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            Vector3 offsets;
            Vector3 borderSize;

            var border = (Border)element;

            var borderColor = border.RenderOpacity * border.BorderColorInternal;
            // optimization: don't draw the border if transparent
            if (borderColor == new Color())
                return;

            var borderThickness = border.BorderThickness;
            var elementHalfBorders = borderThickness / 2;
            var elementSize = element.RenderSizeInternal;
            var elementHalfSize = elementSize / 2;

            // left
            offsets = new Vector3(-elementHalfBorders.Left, 0, element.TotalDepthOffset);
            borderSize = new Vector3(borderThickness.Left, elementSize.Height, 50);
            DrawBorder(border, ref offsets, ref borderSize, ref borderColor, context);
            
            // right
            offsets = new Vector3(elementHalfSize.Width - elementHalfBorders.Right, 0, element.TotalDepthOffset);
            borderSize = new Vector3(borderThickness.Right, elementSize.Height, 50);
            DrawBorder(border, ref offsets, ref borderSize, ref borderColor, context);
            
            // top
            offsets = new Vector3(0, -elementHalfBorders.Top, element.TotalDepthOffset);
            borderSize = new Vector3(elementSize.Width, borderThickness.Top, 50);
            DrawBorder(border, ref offsets, ref borderSize, ref borderColor, context);
            
            // bottom
            offsets = new Vector3(0, elementHalfSize.Height - elementHalfBorders.Bottom, element.TotalDepthOffset);
            borderSize = new Vector3(elementSize.Width, borderThickness.Bottom, 50);
            DrawBorder(border, ref offsets, ref borderSize, ref borderColor, context);
        }

        private void DrawBorder(Border border, ref Vector3 offsets, ref Vector3 borderSize, ref Color borderColor, UIRenderingContext context)
        {
            var worldMatrix = border.WorldMatrixInternal;
            worldMatrix.M41 += worldMatrix.M11 * offsets.X + worldMatrix.M21 * offsets.Y + worldMatrix.M31 * offsets.Z;
            worldMatrix.M42 += worldMatrix.M12 * offsets.X + worldMatrix.M22 * offsets.Y + worldMatrix.M32 * offsets.Z;
            worldMatrix.M43 += worldMatrix.M13 * offsets.X + worldMatrix.M23 * offsets.Y + worldMatrix.M33 * offsets.Z;
            
            Batch.DrawCube(ref worldMatrix, ref borderSize, ref borderColor, context.DepthBias);
        }
    }
}

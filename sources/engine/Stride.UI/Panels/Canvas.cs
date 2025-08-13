// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.UI.Panels
{
    /// <summary> 
    /// Defines an area within which you can position and size child elements with respect to in the Canvas area size.
    /// </summary>
    [DataContract(nameof(Canvas))]
    [DebuggerDisplay("Canvas - Name={Name}")]
    public class Canvas : Panel
    {
        /// <summary>
        /// The key to the UseAbsolutionPosition dependency property. This indicates whether to use the AbsolutePosition or the RelativePosition to place to element.
        /// </summary>
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<bool> UseAbsolutePositionPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(UseAbsolutePositionPropertyKey), typeof(Canvas), true, InvalidateCanvasMeasure);

        /// <summary>
        /// The key to the AbsolutePosition dependency property. AbsolutePosition indicates where the <see cref="UIElement"/> is pinned in the canvas.
        /// </summary>
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<Vector2> AbsolutePositionPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(AbsolutePositionPropertyKey), typeof(Canvas), Vector2.Zero, InvalidateCanvasMeasure);

        /// <summary>
        /// The key to the RelativePosition dependency property. RelativePosition indicates where the <see cref="UIElement"/> is pinned in the canvas.
        /// </summary>
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<Vector2> RelativePositionPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(RelativePositionPropertyKey), typeof(Canvas), Vector2.Zero, InvalidateCanvasMeasure);

        /// <summary>
        /// The key to the RelativeSize dependency property. RelativeSize indicates the ratio of the size of the <see cref="UIElement"/> with respect to the parent size.
        /// </summary>
        /// <remarks>Relative size must be strictly positive</remarks>
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<Size2F> RelativeSizePropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(RelativeSizePropertyKey), typeof(Canvas), new Size2F(float.NaN), CoerceRelativeSize, InvalidateCanvasMeasure);

        /// <summary>
        /// The key to the PinOrigin dependency property. The PinOrigin indicate which point of the <see cref="UIElement"/> should be pinned to the canvas. 
        /// </summary>
        /// <remarks>
        /// Those values are normalized between 0 and 1. (0,0) represent the Left/Top corner and (1,1) represent the Right/Bottom corner. 
        /// <see cref="UIElement"/>'s margins are included in the normalization. 
        /// Values beyond [0,1] are clamped.</remarks>
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<Vector2> PinOriginPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(PinOriginPropertyKey), typeof(Canvas), Vector2.Zero, CoercePinOriginValue, InvalidateCanvasMeasure);
        
        private static void CoercePinOriginValue(ref Vector2 value)
        {
            // Values must be in the range [0, 1]
            value.X = MathUtil.Clamp(value.X, 0.0f, 1.0f);
            value.Y = MathUtil.Clamp(value.Y, 0.0f, 1.0f);
        }

        private static void CoerceRelativeSize(ref Size2F value)
        {
            // All the components of the relative size must be positive
            value.Width = Math.Abs(value.Width);
            value.Height = Math.Abs(value.Height);
        }

        private static void InvalidateCanvasMeasure<T>(object propertyOwner, PropertyKey<T> propertyKey, T propertyOldValue)
        {
            var element = (UIElement)propertyOwner;
            var parentCanvas = element.Parent as Canvas;

            parentCanvas?.InvalidateMeasure();
        }

        /// <inheritdoc/>
        protected override Size2F MeasureOverride(Size2F availableSizeWithoutMargins)
        {
            // Measure all the children
            // Note: canvas does not take into account possible collisions between children
            foreach (var child in VisualChildrenCollection)
            {
                var childAvailableSizeWithoutMargins = new Size2F(float.PositiveInfinity, float.PositiveInfinity);
                // override the available space if the child size is relative to its parent's.
                var childRelativeSize = child.DependencyProperties.Get(RelativeSizePropertyKey);
                
                if (!float.IsNaN(childRelativeSize.Width)) // relative size is not set
                    childAvailableSizeWithoutMargins.Width = childRelativeSize.Width > 0 ? childRelativeSize.Width * availableSizeWithoutMargins.Width : 0f; // avoid NaN due to 0 x Infinity
                
                if (!float.IsNaN(childRelativeSize.Height)) // relative size is not set
                    childAvailableSizeWithoutMargins.Height = childRelativeSize.Height > 0 ? childRelativeSize.Height * availableSizeWithoutMargins.Height : 0f; // avoid NaN due to 0 x Infinity

                child.Measure(childAvailableSizeWithoutMargins + child.MarginInternal);
            }

            return Size2F.Zero;
        }

        /// <inheritdoc/>
        protected override Size2F ArrangeOverride(Size2F finalSizeWithoutMargins)
        {
            // Arrange all the children
            foreach (var child in VisualChildrenCollection)
            {
                // arrange the child
                child.Arrange(child.DesiredSizeWithMargins, IsCollapsed);

                // compute the child offsets wrt parent (left,top,front) corner
                var pinOrigin = child.DependencyProperties.Get(PinOriginPropertyKey);
                var childOrigin = ComputeAbsolutePinPosition(child, ref finalSizeWithoutMargins) - Vector2.Modulate(pinOrigin, (Vector2)child.RenderSize);

                // compute the child offsets wrt parent origin (0,0,0). 
                var childOriginParentCenter = childOrigin - (Vector2)finalSizeWithoutMargins / 2;

                // set the panel arrange matrix for the child
                child.DependencyProperties.Set(PanelArrangeMatrixPropertyKey, Matrix.Translation(new Vector3(childOriginParentCenter.X, childOriginParentCenter.Y, 0)));

            }

            return base.ArrangeOverride(finalSizeWithoutMargins);
        }

        /// <summary>
        /// Compute the child absolute position in the canvas according to parent size and the child layout properties.
        /// </summary>
        /// <param name="child">The child to place</param>
        /// <param name="parentSize">The parent size</param>
        /// <returns>The child absolute position offset</returns>
        protected Vector2 ComputeAbsolutePinPosition(UIElement child, ref Size2F parentSize)
        {
            var relativePosition = child.DependencyProperties.Get(RelativePositionPropertyKey);
            var absolutePosition = child.DependencyProperties.Get(AbsolutePositionPropertyKey);
            var useAbsolutePosition = child.DependencyProperties.Get(UseAbsolutePositionPropertyKey);
            
            if (float.IsNaN(absolutePosition.X) || !useAbsolutePosition && !float.IsNaN(relativePosition.X))
                absolutePosition.X = relativePosition.X == 0f ? 0f : relativePosition.X * parentSize.Width;
            
            if (float.IsNaN(absolutePosition.Y) || !useAbsolutePosition && !float.IsNaN(relativePosition.Y))
                absolutePosition.Y = relativePosition.Y == 0f ? 0f : relativePosition.Y * parentSize.Height;

            return absolutePosition;
        }
    }
}

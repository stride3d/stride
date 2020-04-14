// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.UI.Panels;

namespace Stride.UI
{
    /// <summary>
    /// Extensions methods for <see cref="UIElement"/>
    /// </summary>
    public static class UIElementExtensions
    {
        /// <summary>
        /// Sets the Panel Z-index value for this element. 
        /// The Panel Z-index value is used to determine which child of a same panel should be drawn on top. 
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="Panel.ZIndexPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="index">The Panel Z-index value</param>
        public static void SetPanelZIndex(this UIElement element, int index)
        {
            element.DependencyProperties.Set(Panel.ZIndexPropertyKey, index);
        }

        /// <summary>
        /// Sets the Panel Z-index value for this element. 
        /// The Panel Z-index value is used to determine which child of a same panel should be drawn on top. 
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="Panel.ZIndexPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The Panel Z-index value of the element</returns>
        public static int GetPanelZIndex(this UIElement element)
        {
            return element.DependencyProperties.Get(Panel.ZIndexPropertyKey);
        }

        /// <summary>
        /// Sets the relative position of the element with respect to its parent canvas.
        /// Set the value of any component to <value>float.NaN</value> to let the element measure itself in this axis.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="Canvas.RelativeSizePropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="size">The relative position of the element</param>
        public static void SetCanvasRelativeSize(this UIElement element, Vector3 size)
        {
            element.DependencyProperties.Set(Canvas.RelativeSizePropertyKey, size);
        }

        /// <summary>
        /// Gets the relative position of the element with respect to its parent canvas. <value>float.NaN</value> when not specified.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="Canvas.RelativeSizePropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The relative position of the element to its parent canvas</returns>
        public static Vector3 GetCanvasRelativeSize(this UIElement element)
        {
            return element.DependencyProperties.Get(Canvas.RelativeSizePropertyKey);
        }

        /// <summary>
        /// Sets the relative position of the element into its parent canvas.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="Canvas.RelativePositionPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="position">The relative position normalized between [0,1]</param>
        public static void SetCanvasRelativePosition(this UIElement element, Vector3 position)
        {
            element.DependencyProperties.Set(Canvas.RelativePositionPropertyKey, position);
            element.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, false);
        }

        /// <summary>
        /// Gets the relative position of the element into its parent canvas. Position is normalized between [0,1].
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="Canvas.RelativePositionPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The relative position of the element into its parent canvas</returns>
        public static Vector3 GetCanvasRelativePosition(this UIElement element)
        {
            return element.DependencyProperties.Get(Canvas.RelativePositionPropertyKey);
        }

        /// <summary>
        /// Sets the absolute position of the element into its parent canvas.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="Canvas.AbsolutePositionPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="position">The absolute position in virtual pixels</param>
        public static void SetCanvasAbsolutePosition(this UIElement element, Vector3 position)
        {
            element.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, position);
            element.DependencyProperties.Set(Canvas.UseAbsolutePositionPropertyKey, true);
        }

        /// <summary>
        /// Gets the absolute position of the element into its parent canvas. The position is in virtual pixels.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="Canvas.AbsolutePositionPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The absolute position of the element into its parent canvas</returns>
        public static Vector3 GetCanvasAbsolutePosition(this UIElement element)
        {
            return element.DependencyProperties.Get(Canvas.AbsolutePositionPropertyKey);
        }

        /// <summary>
        /// Sets the origin of the element used when pinning it into its parent canvas.
        /// This value is normalized between [0,1]. (0,0,0) represents the left/top/back corner, (1,1,1) represents the right/bottom/front corner, etc...
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="Canvas.PinOriginPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="origin">The pin origin value</param>
        public static void SetCanvasPinOrigin(this UIElement element, Vector3 origin)
        {
            element.DependencyProperties.Set(Canvas.PinOriginPropertyKey, origin);
        }

        /// <summary>
        /// Gets the origin of the element used when pinning it into its parent canvas.
        /// This value is normalized between [0,1]. (0,0,0) represents the left/top/back corner, (1,1,1) represents the right/bottom/front corner, etc...
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="Canvas.PinOriginPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The pin origin of the element</returns>
        public static Vector3 GetCanvasPinOrigin(this UIElement element)
        {
            return element.DependencyProperties.Get(Canvas.PinOriginPropertyKey);
        }

        /// <summary>
        /// Sets the column index of the grid in which resides the element.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="GridBase.ColumnPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="index">The 0-based column index</param>
        public static void SetGridColumn(this UIElement element, int index)
        {
            element.DependencyProperties.Set(GridBase.ColumnPropertyKey, index);
        }

        /// <summary>
        /// Gets the column index of the grid in which resides the element.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="GridBase.ColumnPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The 0-based column index of the element</returns>
        public static int GetGridColumn(this UIElement element)
        {
            return element.DependencyProperties.Get(GridBase.ColumnPropertyKey);
        }

        /// <summary>
        /// Sets the row index of the grid in which resides the element.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="GridBase.RowPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="index">The 0-based row index</param>
        public static void SetGridRow(this UIElement element, int index)
        {
            element.DependencyProperties.Set(GridBase.RowPropertyKey, index);
        }

        /// <summary>
        /// Gets the row index of the grid in which resides the element.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="GridBase.RowPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The 0-based row index of the element</returns>
        public static int GetGridRow(this UIElement element)
        {
            return element.DependencyProperties.Get(GridBase.RowPropertyKey);
        }

        /// <summary>
        /// Sets the layer index of the grid in which resides the element.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="GridBase.LayerPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="index">The 0-based layer index</param>
        public static void SetGridLayer(this UIElement element, int index)
        {
            element.DependencyProperties.Set(GridBase.LayerPropertyKey, index);
        }

        /// <summary>
        /// Gets the layer index of the grid in which resides the element.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="GridBase.LayerPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The 0-based layer index of the element</returns>
        public static int GetGridLayer(this UIElement element)
        {
            return element.DependencyProperties.Get(GridBase.LayerPropertyKey);
        }

        /// <summary>
        /// Sets the number of column spans that the element occupies in the grid.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="GridBase.ColumnSpanPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="index">The number of column spans occupied</param>
        public static void SetGridColumnSpan(this UIElement element, int index)
        {
            element.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, index);
        }

        /// <summary>
        /// Gets the number of column spans that the element occupies in the grid.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="GridBase.ColumnSpanPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The number column of spans occupied by the element</returns>
        public static int GetGridColumnSpan(this UIElement element)
        {
            return element.DependencyProperties.Get(GridBase.ColumnSpanPropertyKey);
        }

        /// <summary>
        /// Sets the number of row spans that the element occupies in the grid.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="GridBase.RowSpanPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="index">The number of row spans occupied</param>
        public static void SetGridRowSpan(this UIElement element, int index)
        {
            element.DependencyProperties.Set(GridBase.RowSpanPropertyKey, index);
        }

        /// <summary>
        /// Gets the number of row spans that the element occupies in the grid.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="GridBase.RowSpanPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The number of row spans occupied by the element</returns>
        public static int GetGridRowSpan(this UIElement element)
        {
            return element.DependencyProperties.Get(GridBase.RowSpanPropertyKey);
        }

        /// <summary>
        /// Sets the number of layer spans that the element occupies in the grid.
        /// </summary>
        /// <remarks>Equivalent to set the <see cref="GridBase.LayerSpanPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <param name="index">The number of layer spans occupied</param>
        public static void SetGridLayerSpan(this UIElement element, int index)
        {
            element.DependencyProperties.Set(GridBase.LayerSpanPropertyKey, index);
        }

        /// <summary>
        /// Gets the number of layer spans that the element occupies in the grid.
        /// </summary>
        /// <remarks>Equivalent to get the <see cref="GridBase.LayerSpanPropertyKey"/> of the element</remarks>
        /// <param name="element">The element</param>
        /// <returns>The number of layer spans occupied by the element</returns>
        public static int GetGridLayerSpan(this UIElement element)
        {
            return element.DependencyProperties.Get(GridBase.LayerSpanPropertyKey);
        }
    }
}

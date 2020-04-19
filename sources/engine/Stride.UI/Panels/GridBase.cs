// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.UI.Panels
{
    /// <summary>
    /// Represents the base primitive for all the grid-like controls
    /// </summary>
    [DataContract(nameof(GridBase))]
    [DebuggerDisplay("GridBase - Name={Name}")]
    public abstract class GridBase : Panel
    {
        /// <summary>
        /// The key to the Row attached dependency property. This defines the row an item is inserted into.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="int.MaxValue"/>].</remarks>
        /// <remarks>First row has 0 as index</remarks>
        [DataMemberRange(0, 0)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> RowPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(RowPropertyKey), typeof(GridBase), 0, CoerceGridPositionsValue, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the RowSpan attached dependency property. This defines the number of rows an item takes.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="int.MaxValue"/>].</remarks>
        [DataMemberRange(1, 0)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> RowSpanPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(RowSpanPropertyKey), typeof(GridBase), 1, CoerceSpanValue, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the Column attached dependency property. This defines the column an item is inserted into.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="int.MaxValue"/>].</remarks>
        /// <remarks>First column has 0 as index</remarks>
        [DataMemberRange(0, 0)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> ColumnPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(ColumnPropertyKey), typeof(GridBase), 0, CoerceGridPositionsValue, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the ColumnSpan attached dependency property. This defines the number of columns an item takes.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="int.MaxValue"/>].</remarks>
        [DataMemberRange(1, 0)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> ColumnSpanPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(ColumnSpanPropertyKey), typeof(GridBase), 1, CoerceSpanValue, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the Layer attached dependency property. This defines the layer an item is inserted into.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="int.MaxValue"/>].</remarks>
        /// <remarks>First layer has 0 as index</remarks>
        [DataMemberRange(0, 0)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> LayerPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(LayerPropertyKey), typeof(GridBase), 0, CoerceGridPositionsValue, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the LayerSpan attached dependency property. This defines the number of layers an item takes.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="int.MaxValue"/>].</remarks>
        [DataMemberRange(1, 0)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> LayerSpanPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(LayerSpanPropertyKey), typeof(GridBase), 1, CoerceSpanValue, InvalidateParentGridMeasure);

        private static void InvalidateParentGridMeasure(object propertyowner, PropertyKey<int> propertykey, int propertyoldvalue)
        {
            var element = (UIElement)propertyowner;
            var parentGridBase = element.Parent as GridBase;

            parentGridBase?.InvalidateMeasure();
        }

        /// <summary>
        /// Coerce the value of <see cref="ColumnPropertyKey"/> <see cref="LayerPropertyKey"/>, or <see cref="RowPropertyKey"/> between 0 and <see cref="int.MaxValue"/>.
        /// </summary>
        /// <param name="value"></param>
        private static void CoerceGridPositionsValue(ref int value)
        {
            value = MathUtil.Clamp(value, 0, int.MaxValue);
        }

        /// <summary>
        /// Coerce the value of <see cref="ColumnSpanPropertyKey"/> <see cref="LayerSpanPropertyKey"/>, ir <see cref="RowSpanPropertyKey"/> between 1 and <see cref="int.MaxValue"/>.
        /// </summary>
        /// <param name="value"></param>
        private static void CoerceSpanValue(ref int value)
        {
            value = MathUtil.Clamp(value, 1, int.MaxValue);
        }

        /// <summary>
        /// Get an element span values as an <see cref="Int3"/>.
        /// </summary>
        /// <param name="element">The element from which extract the span values</param>
        /// <returns>The span values of the element</returns>
        protected virtual Int3 GetElementSpanValues(UIElement element)
        {
            return new Int3(
                element.DependencyProperties.Get(ColumnSpanPropertyKey),
                element.DependencyProperties.Get(RowSpanPropertyKey),
                element.DependencyProperties.Get(LayerSpanPropertyKey));
        }

        /// <summary>
        /// Get the positions of an element in the grid as an <see cref="Int3"/>.
        /// </summary>
        /// <param name="element">The element from which extract the position values</param>
        /// <returns>The position of the element</returns>
        protected virtual Int3 GetElementGridPositions(UIElement element)
        {
            return new Int3(
                element.DependencyProperties.Get(ColumnPropertyKey),
                element.DependencyProperties.Get(RowPropertyKey),
                element.DependencyProperties.Get(LayerPropertyKey));
        }
    }
}

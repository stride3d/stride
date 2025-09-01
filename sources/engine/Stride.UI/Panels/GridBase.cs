// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
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

        private float columnGap = 0f;
        private float rowGap = 0f;
        private float layerGap = 0f;

        /// <summary>
        /// Gets or sets the gap between columns in virtual pixels.
        /// </summary>
        /// <userdoc>The gap between columns in virtual pixels.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(0f)]
        public float ColumnGap
        {
            get { return columnGap; }
            set
            {
                if (columnGap != value)
                {
                    columnGap = Math.Max(0, value);
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Gets or sets the gap between rows in virtual pixels.
        /// </summary>
        /// <userdoc>The gap between rows in virtual pixels.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(0f)]
        public float RowGap
        {
            get { return rowGap; }
            set
            {
                if (rowGap != value)
                {
                    rowGap = Math.Max(0, value);
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Gets or sets the gap between layers in virtual pixels.
        /// </summary>
        /// <userdoc>The gap between layers in virtual pixels.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(0f)]
        public float LayerGap
        {
            get { return layerGap; }
            set
            {
                if (layerGap != value)
                {
                    layerGap = Math.Max(0, value);
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Calculates the total gap size for a dimension based on the number of cells.
        /// </summary>
        /// <param name="dimension">The dimension (0=Column, 1=Row, 2=Layer)</param>
        /// <param name="cellCount">The number of cells in this dimension</param>
        /// <returns>The total gap size</returns>
        public float CalculateTotalGapSize(int dimension, int cellCount)
        {
            if (cellCount <= 1) return 0f;
            return GetGapForDimension(dimension) * (cellCount - 1);
        }

        /// <summary>
        /// Gets the gap value for the specified dimension.
        /// </summary>
        /// <param name="dimension">The dimension (0=Column, 1=Row, 2=Layer)</param>
        /// <returns>The gap value</returns>
        protected float GetGapForDimension(int dimension)
        {
            return dimension switch
            {
                0 => columnGap,
                1 => rowGap,
                2 => layerGap,
                _ => 0f
            };
        }

        /// <summary>
        /// Calculates and returns the spacing gaps between columns, rows, and layers.
        /// </summary>
        /// <returns>A <see cref="Vector3"/> where the X, Y, and Z components represent the column gap, row gap, and layer gap,
        /// respectively.</returns>
        protected Vector3 GetGaps()
        {
            return new Vector3(columnGap, rowGap, layerGap);
        }

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

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
    /// Represents the grid where all the rows and columns have an uniform size.
    /// </summary>
    [DataContract(nameof(UniformGrid))]
    [DebuggerDisplay("UniformGrid - Name={Name}")]
    public class UniformGrid : GridBase
    {
        /// <summary>
        /// The final size of one cell
        /// </summary>
        private Size2F finalForOneCell;

        private int rows = 1;
        private int columns = 1;

        /// <summary>
        /// Gets or sets the number of rows that the <see cref="UniformGrid"/> has.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="int.MaxValue"/>].</remarks>
        /// <userdoc>The number of rows.</userdoc>
        [DataMember]
        [DataMemberRange(1, 0)]
        [Display(category: LayoutCategory)]
        [DefaultValue(1)]
        public int Rows
        {
            get { return rows; }
            set
            {
                rows = MathUtil.Clamp(value, 1, int.MaxValue);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the number of columns that the <see cref="UniformGrid"/> has.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="int.MaxValue"/>].</remarks>
        /// <userdoc>The number of columns.</userdoc>
        [DataMember]
        [DataMemberRange(1, 0)]
        [Display(category: LayoutCategory)]
        [DefaultValue(1)]
        public int Columns
        {
            get { return columns; }
            set
            {
                columns = MathUtil.Clamp(value, 1, int.MaxValue);
                InvalidateMeasure();
            }
        }

        protected override Size2F MeasureOverride(Size2F availableSizeWithoutMargins)
        {
            // compute the size available for one cell
            var gridSize = new Size2F(Columns, Rows);
            var availableForOneCell = new Size2F(availableSizeWithoutMargins.Width / gridSize.Width, availableSizeWithoutMargins.Height / gridSize.Height);

            // measure all the children
            var neededForOneCell = Size2F.Zero;
            foreach (var child in VisualChildrenCollection)
            {
                // compute the size available for the child depending on its spans values
                var childSpans = GetElementSpanValues(child).AsFloat();
                var availableForChildWithMargin = Size2F.Modulate((Size2F)childSpans, availableForOneCell);

                child.Measure(availableForChildWithMargin);

                neededForOneCell = new Size2F(
                    Math.Max(neededForOneCell.Width, child.DesiredSizeWithMargins.Width / childSpans.X),
                    Math.Max(neededForOneCell.Height, child.DesiredSizeWithMargins.Height / childSpans.Y));
            }

            return Size2F.Modulate(gridSize, neededForOneCell);
        }

        protected override Size2F ArrangeOverride(Size2F finalSizeWithoutMargins)
        {
            // compute the size available for one cell
            finalForOneCell = new Size2F(finalSizeWithoutMargins.Width / Columns, finalSizeWithoutMargins.Height / Rows);

            // arrange all the children
            foreach (var child in VisualChildrenCollection)
            {
                // compute the final size of the child depending on its spans values
                var childSpans = GetElementSpanValues(child).AsFloat();
                var finalForChildWithMargin = Size2F.Modulate((Size2F)childSpans, finalForOneCell);

                // set the arrange matrix of the child
                var childOffsets = GetElementGridPositions(child).AsFloat();
                var totalChildOffset = Vector2.Modulate(childOffsets, (Vector2)finalForOneCell) - (Vector2)finalSizeWithoutMargins / 2;
                child.DependencyProperties.Set(PanelArrangeMatrixPropertyKey, Matrix.Translation(new Vector3(totalChildOffset.X, totalChildOffset.Y, 0)));

                // arrange the child
                child.Arrange(finalForChildWithMargin, IsCollapsed);
            }

            return finalSizeWithoutMargins;
        }
        
        private void CalculateDistanceToSurroundingModulo(float position, float modulo, float elementCount, out Vector2 distances)
        {
            if (modulo <= 0)
            {
                distances = Vector2.Zero;
                return;
            }

            var validPosition = Math.Max(0, Math.Min(position, elementCount * modulo));
            var inferiorQuotient = Math.Min(elementCount - 1, MathF.Floor(validPosition / modulo));

            distances.X = (inferiorQuotient+0) * modulo - validPosition;
            distances.Y = (inferiorQuotient+1) * modulo - validPosition;
        }

        public override Vector2 GetSurroudingAnchorDistances(Orientation direction, float position)
        {
            Vector2 distances;
            var gridElements = new Vector2(Columns, Rows);
            
            CalculateDistanceToSurroundingModulo(position, finalForOneCell[(int)direction], gridElements[(int)direction], out distances);

            return distances;
        }
    }
}

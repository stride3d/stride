// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Controls
{
    /// <summary>
    /// This control represents a <see cref="Grid"/> with two columns, the first one representing keys and the second one representing
    /// values. <see cref="Grid.ColumnDefinitions"/> and <see cref="Grid.RowDefinitions"/> should not be modified for this control. Every 
    /// child content added in this control will either create a new row and be placed on its left column, or placed on the second column
    /// of the last row.
    /// </summary>
    /// <remarks>The column for the keys has an <see cref="GridUnitType.Auto"/> width.</remarks>
    /// <remarks>The column for the values has an <see cref="GridUnitType.Star"/> width.</remarks>
    public class KeyValueGrid : Grid
    {
        private bool gridParametersInvalidated;

        public static readonly DependencyProperty UseFullRowProperty = DependencyProperty.RegisterAttached("UseFullRow", typeof(bool), typeof(KeyValueGrid));

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueGrid"/> class.
        /// </summary>
        public KeyValueGrid()
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.0, GridUnitType.Auto) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        }

        public static bool GetUseFullRow([NotNull] DependencyObject obj)
        {
            return (bool)obj.GetValue(UseFullRowProperty);
        }

        public static void SetUseFullRow([NotNull] DependencyObject obj, bool value)
        {
            obj.SetValue(UseFullRowProperty, value);
        }

        /// <summary>
        /// Recomputes rows and update children.
        /// </summary>
        private void InvalidateGridParameters()
        {
            var rowCollection = RowDefinitions;
            var children = Children;

            // Determine how many rows we need
            int remainder;
            var neededRowCount = Math.DivRem(children.Count, 2, out remainder) + remainder;

            var currentRowCount = rowCollection.Count;
            var deltaRowCount = neededRowCount - currentRowCount;

            // Add/remove rows
            if (deltaRowCount > 0)
            {
                for (var i = 0; i < deltaRowCount; i++)
                    rowCollection.Add(new RowDefinition { Height = new GridLength(0.0, GridUnitType.Auto) });
            }
            else if (deltaRowCount < 0)
            {
                rowCollection.RemoveRange(currentRowCount + deltaRowCount, -deltaRowCount);
            }

            // Update Grid.Row and Grid.Column dependency properties on each child control
            var row = 0;
            var column = 0;
            foreach (UIElement element in children)
            {
                element.SetValue(ColumnProperty, column);
                element.SetValue(RowProperty, row);

                if (column == 0 && GetUseFullRow(element))
                {
                    element.SetValue(ColumnSpanProperty, 2);
                    row++;
                }
                else
                {
                    column++;
                    if (column > 1)
                    {
                        column = 0;
                        row++;
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            if (gridParametersInvalidated)
            {
                gridParametersInvalidated = false;
                InvalidateGridParameters();
            }

            return base.MeasureOverride(constraint);
        }

        /// <inheritdoc/>
        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            gridParametersInvalidated = true;
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }
    }
}

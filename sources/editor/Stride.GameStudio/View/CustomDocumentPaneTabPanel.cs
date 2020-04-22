// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using AvalonDock.Controls;

namespace Stride.GameStudio.View
{
    /// <summary>
    /// A custom implementation of AvalonDock's DocumentPaneTabPanel class with a different implementation of the <see cref="MeasureOverride"/>
    /// and <see cref="ArrangeOverride"/> passes in order to use text trimming instead of hidding whole tabs.
    /// </summary>
    public class CustomDocumentPaneTabPanel : DocumentPaneTabPanel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            // Note: this implementation is similar to the WrapPanel
            var rowSize = new Size();
            var totalSize = new Size();
            var internalChildren = InternalChildren;
            var count = internalChildren.Count;
            for (var i = 0; i < count; ++i)
            {
                var uiElement = internalChildren[i];
                if (uiElement != null)
                {
                    uiElement.Measure(availableSize);
                    var desiredSize = uiElement.DesiredSize;
                    if (GreaterThan(rowSize.Width + desiredSize.Width, availableSize.Width))
                    {
                        totalSize.Width = Math.Max(rowSize.Width, totalSize.Width);
                        totalSize.Height += rowSize.Height;
                        rowSize = desiredSize;
                        if (GreaterThan(desiredSize.Width, availableSize.Width))
                        {
                            totalSize.Width = Math.Max(desiredSize.Width, totalSize.Width);
                            totalSize.Height += desiredSize.Height;
                            rowSize = new Size();
                        }
                    }
                    else
                    {
                        rowSize.Width += desiredSize.Width;
                        rowSize.Height = Math.Max(desiredSize.Height, rowSize.Height);
                    }
                }
            }
            totalSize.Width = Math.Max(rowSize.Width, totalSize.Width);
            totalSize.Height += rowSize.Height;
            return totalSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Note: this implementation is similar to the WrapPanel
            var start = 0;
            var offsetY = 0.0;
            var rowSize = new Size();
            var internalChildren = InternalChildren;
            var count = internalChildren.Count;
            for (var i = 0; i < count; ++i)
            {
                var uiElement = internalChildren[i];
                if (uiElement != null)
                {
                    var desiredSize = uiElement.DesiredSize;
                    if (GreaterThan(rowSize.Width + desiredSize.Width, finalSize.Width))
                    {
                        ArrangeRow(offsetY, rowSize.Height, start, i);
                        offsetY += rowSize.Height;
                        rowSize = desiredSize;
                        if (GreaterThan(desiredSize.Width, finalSize.Width))
                        {
                            ArrangeRow(offsetY, desiredSize.Height, i, ++i);
                            offsetY += desiredSize.Height;
                            rowSize = new Size();
                        }
                        start = i;
                    }
                    else
                    {
                        rowSize.Width += desiredSize.Width;
                        rowSize.Height = Math.Max(desiredSize.Height, rowSize.Height);
                    }
                }
            }
            if (start < internalChildren.Count)
                ArrangeRow(offsetY, rowSize.Height, start, internalChildren.Count);

            return finalSize;
        }

        private void ArrangeRow(double offsetY, double rowHeight, int start, int end)
        {
            var offsetX = 0.0;
            var internalChildren = InternalChildren;
            for (var index = start; index < end; ++index)
            {
                var uiElement = internalChildren[index];
                if (uiElement != null)
                {
                    var desiredSize = uiElement.DesiredSize;
                    uiElement.Arrange(new Rect(offsetX, offsetY, desiredSize.Width, rowHeight));
                    offsetX += desiredSize.Width;
                }
            }
        }

        private static bool GreaterThan(double value1, double value2)
        {
            if (value1 > value2)
            {
                var num1 = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.22044604925031E-16;
                var num2 = value1 - value2;
                var result = -num1 < num2 && num1 > num2;
                return !result;
            }
            return false;
        }
    }
}

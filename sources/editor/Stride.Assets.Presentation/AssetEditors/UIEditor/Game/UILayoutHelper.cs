// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.ViewModel;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Game
{
    /// <summary>
    /// Helper class for updating the layout of a <see cref="UIElement"/>. Supports moving and resizing.
    /// </summary>
    internal static class UILayoutHelper
    {
        /// <summary>
        /// Moves an element.
        /// </summary>
        /// <param name="element">The element to move.</param>
        /// <param name="delta"></param>
        /// <param name="magnetDistance">The maximum distance at which magnet will be applied</param>
        /// <param name="resolution">The resolution of the UI.</param>
        /// <returns><c>true</c> if the element has moved; otherwise, <c>false</c>.</returns>
        public static bool Move([NotNull] UIElement element, ref Vector3 delta, float magnetDistance, ref Vector3 resolution)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            if (element.VisualParent is ContentControl)
                return false;

            // moving is almost equivalent to resizing in all direction
            var parameters = new LayoutParameters
            {
                Left = true,
                Right = true,
                Top = true,
                Bottom = true,
                CanResize = false,
            };
            UpdateElementLayout(element, ref delta, magnetDistance, ref resolution, parameters);
            return true;
        }

        /// <summary>
        /// Resizes an element.
        /// </summary>
        /// <param name="element">The element to move.</param>
        /// <param name="resizingDirection"></param>
        /// <param name="delta"></param>
        /// <param name="magnetDistance">The maximum distance at which magnet will be applied</param>
        /// <param name="resolution">The resolution of the UI.</param>
        /// <returns></returns>
        public static bool Resize([NotNull] UIElement element, ResizingDirection resizingDirection, ref Vector3 delta, float magnetDistance, ref Vector3 resolution)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            var parameters = new LayoutParameters
            {
                Left = resizingDirection.HasFlag(ResizingDirection.Left),
                Right = resizingDirection.HasFlag(ResizingDirection.Right),
                Top = resizingDirection.HasFlag(ResizingDirection.Top),
                Bottom = resizingDirection.HasFlag(ResizingDirection.Bottom),
                CanResize = true,
            };
            UpdateElementLayout(element, ref delta, magnetDistance, ref resolution, parameters);
            return true;
        }

        private class RectData
        {
            public RectangleF Container;
            public RectangleF Element;
            public RectangleF Parent;
            public RectangleF[] Siblings;
        }

        private struct LayoutParameters
        {
            /// <summary>
            /// <c>true</c> if layout should be updated on the left side; otherwise, <c>false</c>.
            /// </summary>
            public bool Left;
            /// <summary>
            /// <c>true</c> if layout should be updated on the right side; otherwise, <c>false</c>.
            /// </summary>
            public bool Right;
            /// <summary>
            /// <c>true</c> if layout should be updated on the top side; otherwise, <c>false</c>.
            /// </summary>
            public bool Top;
            /// <summary>
            /// <c>true</c> if layout should be updated on the right side; otherwise, <c>false</c>.
            /// </summary>
            public bool Bottom;
            /// <summary>
            /// <c>true</c> if the size of the element can be changed during the layout update; otherwise, <c>false</c>.
            /// </summary>
            public bool CanResize;
        }

        /// <summary>
        /// Adapts the alignment relative to the position of the element in its container.
        /// </summary>
        /// <param name="elementRect"></param>
        /// <param name="containerRect"></param>
        /// <param name="horizontalAlignment"></param>
        /// <param name="verticalAlignment"></param>
        private static void AdjustAlignmentInContainer(ref RectangleF elementRect, ref RectangleF containerRect, out HorizontalAlignment horizontalAlignment, out VerticalAlignment verticalAlignment)
        {
            var containerCenter = containerRect.Center;
            // adjust horizontal alignment
            horizontalAlignment = HorizontalAlignment.Stretch;
            if (elementRect.Width < 0.5f*containerRect.Width)
            {
                if (elementRect.Right < containerCenter.X)
                    horizontalAlignment = HorizontalAlignment.Left;
                else if (elementRect.Left > containerCenter.X)
                    horizontalAlignment = HorizontalAlignment.Right;
            }
            // adjust vertical alignment
            verticalAlignment = VerticalAlignment.Stretch;
            if (elementRect.Height < 0.5f*containerRect.Height)
            {
                if (elementRect.Bottom < containerCenter.Y)
                    verticalAlignment = VerticalAlignment.Top;
                else if (elementRect.Top > containerCenter.Y)
                    verticalAlignment = VerticalAlignment.Bottom;
            }
        }

        /// <summary>
        /// Calculates the rect of the element, its parent, its container (if different from the parent, e.g. in a grid) and its siblings elements.
        /// </summary>
        /// <param name="element">The element to calculate the rects from.</param>
        /// <param name="resolution">The resolution of the UI.</param>
        /// <returns></returns>
        [NotNull]
        private static RectData ExtractRects([NotNull] UIElement element, ref Vector3 resolution)
        {
            var rects = new RectData
            {
                // note: render offset is calculated relatively to the container (which can be the same as the parent)
                Element = new RectangleF(element.RenderOffsets.X, element.RenderOffsets.Y, element.ActualWidth, element.ActualHeight)
            };
            var parent = element.VisualParent;
            if (parent is GridBase)
            {
                var rowIndex = element.GetGridRow();
                var rowSpan = element.GetGridRowSpan();
                var colIndex = element.GetGridColumn();
                var colSpan = element.GetGridColumnSpan();
                var grid = parent as Grid;
                if (grid != null)
                {
                    var actualColumnDefinitions = grid.ActualColumnDefinitions;
                    var actualRowDefinitions = grid.ActualRowDefinitions;

                    var accWidth = 0.0f;
                    for (var i = 0; i < colIndex && i < actualColumnDefinitions.Count; i++)
                    {
                        var definition = actualColumnDefinitions[i];
                        accWidth += definition.ActualSize;
                    }
                    var accHeight = 0.0f;
                    for (var i = 0; i < rowIndex && i < actualRowDefinitions.Count; i++)
                    {
                        var definition = actualRowDefinitions[i];
                        accHeight += definition.ActualSize;
                    }
                    rects.Parent = new RectangleF
                    {
                        X = -accWidth, Y = -accHeight, Width = parent.ActualWidth, Height = parent.ActualHeight,
                    };
                    accWidth = 0.0f;
                    for (var i = colIndex; i < colIndex + colSpan && i < actualColumnDefinitions.Count; i++)
                    {
                        var definition = actualColumnDefinitions[i];
                        accWidth += definition.ActualSize;
                    }
                    accHeight = 0.0f;
                    for (var i = rowIndex; i < rowIndex + rowSpan && i < actualRowDefinitions.Count; i++)
                    {
                        var definition = actualRowDefinitions[i];
                        accHeight += definition.ActualSize;
                    }
                    rects.Container = new RectangleF
                    {
                        X = 0, Y = 0, Width = accWidth, Height = accHeight,
                    };
                }
                var uniformGrid = parent as UniformGrid;
                if (uniformGrid != null)
                {
                    var cellWidth = uniformGrid.ActualWidth/uniformGrid.Columns;
                    var cellHeight = uniformGrid.ActualHeight/uniformGrid.Rows;
                    rects.Parent = new RectangleF
                    {
                        X = -MathUtil.Clamp(colIndex, 0, uniformGrid.Columns - 1)*cellWidth, Y = -MathUtil.Clamp(rowIndex, 0, uniformGrid.Rows - 1)*cellHeight, Width = parent.ActualWidth, Height = parent.ActualHeight,
                    };
                    rects.Container = new RectangleF
                    {
                        X = 0, Y = 0, Width = MathUtil.Clamp(uniformGrid.Columns - colIndex, 1, colSpan)*cellWidth, Height = MathUtil.Clamp(uniformGrid.Rows - rowIndex, 1, rowSpan)*cellHeight,
                    };
                }
                rects.Siblings = (from c in parent.VisualChildren where c != element && c.GetGridRow() == rowIndex && c.GetGridColumn() == colIndex select new RectangleF(c.RenderOffsets.X, c.RenderOffsets.Y, c.ActualWidth, c.ActualHeight)).ToArray();
            }
            else if (parent != null)
            {
                rects.Parent = new RectangleF(0, 0, parent.ActualWidth, parent.ActualHeight);
                rects.Container = rects.Parent;
                rects.Siblings = (from c in parent.VisualChildren where c != element select new RectangleF(c.RenderOffsets.X, c.RenderOffsets.Y, c.ActualWidth, c.ActualHeight)).ToArray();
            }
            else
            {
                // no parent, take the whole UI resolution
                rects.Parent = new RectangleF(0, 0, resolution.X, resolution.Y);
                rects.Container = rects.Parent;
                rects.Siblings = new RectangleF[0];
            }
            return rects;
        }

        /// <summary>
        /// Snaps the bottom side of the element bounds to the magnet bounds.
        /// </summary>
        /// <param name="elementRect"></param>
        /// <param name="magnetRect"></param>
        /// <param name="verticallyMagnetized"></param>
        /// <param name="magnetDistance"></param>
        /// <param name="resize"></param>
        private static void MagnetizeBottom(ref RectangleF elementRect, ref RectangleF magnetRect, ref bool verticallyMagnetized, float magnetDistance, bool resize)
        {
            if (verticallyMagnetized)
                return;

            // element's bottom/ magnet's top
            var diffTop = elementRect.Bottom - magnetRect.Top;
            if (Math.Abs(diffTop) < magnetDistance)
            {
                if (resize)
                    elementRect.Height -= diffTop;
                else
                    elementRect.Top = magnetRect.Top - elementRect.Height;
                verticallyMagnetized = true;
                return;
            }

            // element's bottom/ magnet's bottom
            var diffBottom = elementRect.Bottom - magnetRect.Bottom;
            if (Math.Abs(diffBottom) < magnetDistance)
            {
                if (resize)
                    elementRect.Height -= diffBottom;
                else
                    elementRect.Top = magnetRect.Bottom - elementRect.Height;
                verticallyMagnetized = true;
            }
        }

        /// <summary>
        /// Snaps the left side of the element bounds to the magnet bounds.
        /// </summary>
        /// <param name="elementRect"></param>
        /// <param name="magnetRect"></param>
        /// <param name="horizontallyMagnetized"></param>
        /// <param name="magnetDistance"></param>
        /// <param name="resize"></param>
        private static void MagnetizeLeft(ref RectangleF elementRect, ref RectangleF magnetRect, ref bool horizontallyMagnetized, float magnetDistance, bool resize)
        {
            if (horizontallyMagnetized)
                return;

            // element's left / magnet's left
            var diffLeft = elementRect.Left - magnetRect.Left;
            if (Math.Abs(diffLeft) < magnetDistance)
            {
                elementRect.Left = magnetRect.Left;
                if (resize)
                    elementRect.Width += diffLeft;
                horizontallyMagnetized = true;
                return;
            }

            // element's left / magnet's right
            var diffRight = elementRect.Left - magnetRect.Right;
            if (Math.Abs(elementRect.Left - magnetRect.Right) < magnetDistance)
            {
                elementRect.Left = magnetRect.Right;
                if (resize)
                    elementRect.Width += diffRight;
                horizontallyMagnetized = true;
            }
        }

        /// <summary>
        /// Snaps the top side of the element bounds to the magnet bounds.
        /// </summary>
        /// <param name="elementRect"></param>
        /// <param name="magnetRect"></param>
        /// <param name="verticallyMagnetized"></param>
        /// <param name="magnetDistance"></param>
        /// <param name="resize"></param>
        private static void MagnetizeTop(ref RectangleF elementRect, ref RectangleF magnetRect, ref bool verticallyMagnetized, float magnetDistance, bool resize)
        {
            if (verticallyMagnetized)
                return;

            // element's top/ magnet's top
            var diffTop = elementRect.Top - magnetRect.Top;
            if (Math.Abs(diffTop) < magnetDistance)
            {
                elementRect.Top = magnetRect.Top;
                if (resize)
                    elementRect.Height += diffTop;
                verticallyMagnetized = true;
                return;
            }

            // element's top/ magnet's bottom
            var diffBottom = elementRect.Top - magnetRect.Bottom;
            if (Math.Abs(diffBottom) < magnetDistance)
            {
                elementRect.Top = magnetRect.Bottom;
                if (resize)
                    elementRect.Height += diffBottom;
                verticallyMagnetized = true;
            }
        }

        /// <summary>
        /// Snaps the right side of the element bounds to the magnet bounds.
        /// </summary>
        /// <param name="elementRect"></param>
        /// <param name="magnetRect"></param>
        /// <param name="horizontallyMagnetized"></param>
        /// <param name="magnetDistance"></param>
        /// <param name="resize"></param>
        private static void MagnetizeRight(ref RectangleF elementRect, ref RectangleF magnetRect, ref bool horizontallyMagnetized, float magnetDistance, bool resize)
        {
            if (horizontallyMagnetized)
                return;

            // element's right / magnet's left
            var diffLeft = elementRect.Right - magnetRect.Left;
            if (Math.Abs(diffLeft) < magnetDistance)
            {
                if (resize)
                    elementRect.Width -= diffLeft;
                else
                    elementRect.Left = magnetRect.Left - elementRect.Width;
                horizontallyMagnetized = true;
                return;
            }

            // element's right / magnet's right
            var diffRight = elementRect.Right - magnetRect.Right;
            if (Math.Abs(diffRight) < magnetDistance)
            {
                if (resize)
                    elementRect.Width -= diffRight;
                else
                    elementRect.Left = magnetRect.Right - elementRect.Width;
                horizontallyMagnetized = true;
            }
        }

        private static void UpdateElementLayout([NotNull] UIElement element, ref Vector3 delta, float magnetDistance, ref Vector3 resolution, LayoutParameters parameters)
        {
            // Retrieve all notable rects from the parent (i.e. siblings, areas such as grid cell container, etc.)
            var rects = ExtractRects(element, ref resolution);
            // copy element's current rect
            var currentElementRect = rects.Element;
            // apply resizing delta to the element rect
            if (parameters.Left)
            {
                rects.Element.X += delta.X;
                rects.Element.Width -= delta.X;
            }
            if (parameters.Right)
                rects.Element.Width += delta.X;
            if (parameters.Top)
            {
                rects.Element.Y += delta.Y;
                rects.Element.Height -= delta.Y;
            }
            if (parameters.Bottom)
                rects.Element.Height += delta.Y;

            // magnetize
            var horizontallyMagnetized = false;
            var verticallyMagnetized = false;
            // .. parent
            var contentControl = element.VisualParent as ContentControl;
            if (contentControl == null || !float.IsNaN(contentControl.Width))
            {
                if (parameters.Left)
                    MagnetizeLeft(ref rects.Element, ref rects.Parent, ref horizontallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Right)
                    MagnetizeRight(ref rects.Element, ref rects.Parent, ref horizontallyMagnetized, magnetDistance, parameters.CanResize);
            }
            if (contentControl == null || !float.IsNaN(contentControl.Height))
            {
                if (parameters.Top)
                    MagnetizeTop(ref rects.Element, ref rects.Parent, ref verticallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Bottom)
                    MagnetizeBottom(ref rects.Element, ref rects.Parent, ref verticallyMagnetized, magnetDistance, parameters.CanResize);
            }
            // .. container
            if (rects.Parent != rects.Container)
            {
                if (parameters.Left)
                    MagnetizeLeft(ref rects.Element, ref rects.Container, ref horizontallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Right)
                    MagnetizeRight(ref rects.Element, ref rects.Container, ref horizontallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Top)
                    MagnetizeTop(ref rects.Element, ref rects.Container, ref verticallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Bottom)
                    MagnetizeBottom(ref rects.Element, ref rects.Container, ref verticallyMagnetized, magnetDistance, parameters.CanResize);
            }
            // .. sibling in same container
            for (var i = 0; i < rects.Siblings.Length; i++)
            {
                if (parameters.Left)
                    MagnetizeLeft(ref rects.Element, ref rects.Siblings[i], ref horizontallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Right)
                    MagnetizeRight(ref rects.Element, ref rects.Siblings[i], ref horizontallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Top)
                    MagnetizeTop(ref rects.Element, ref rects.Siblings[i], ref verticallyMagnetized, magnetDistance, parameters.CanResize);
                if (parameters.Bottom)
                    MagnetizeBottom(ref rects.Element, ref rects.Siblings[i], ref verticallyMagnetized, magnetDistance, parameters.CanResize);
            }

            // calculate resulting margin
            var margin = new Thickness
            {
                Left = rects.Element.Left,
                Top = rects.Element.Top
            };
            var horizontalAlignment = element.HorizontalAlignment;
            var verticalAlignment = element.VerticalAlignment;
            if (element.VisualParent is Canvas)
            {
                // inside a Canvas, the alignment should be left-top
                horizontalAlignment = HorizontalAlignment.Left;
                verticalAlignment = VerticalAlignment.Top;
            }
            else if (element.VisualParent is ContentControl)
            {
                // inside a ContentControl, the margin remains unchanged ; only the size will be updated.
                margin = element.Margin;
            }
            else
            {
                margin.Right = rects.Container.Right - rects.Element.Right;
                margin.Bottom = rects.Container.Bottom - rects.Element.Bottom;

                var stackPanel = element.VisualParent as StackPanel;
                if (stackPanel != null)
                {
                    // inside a stackpanel the alignment depends on the panel orientation
                    if (stackPanel.Orientation == Orientation.Horizontal)
                        horizontalAlignment = HorizontalAlignment.Left;
                    else
                        verticalAlignment = VerticalAlignment.Top;
                }
                else if (rects.Container == rects.Parent)
                {
                    // adjust alignment relatively to the parent/container
                    AdjustAlignmentInContainer(ref rects.Element, ref rects.Container, out horizontalAlignment, out verticalAlignment);
                }

                // final adjustment of alignment, size and margin
                switch (horizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        // Compensate when out of container bounds
                        var overLeft = rects.Container.Width - (margin.Left + rects.Element.Width);
                        margin.Right = Math.Min(overLeft, 0);
                        break;
                    case HorizontalAlignment.Center:
                        // Fall back to Stretch alignment
                        horizontalAlignment = HorizontalAlignment.Stretch;
                        break;
                    case HorizontalAlignment.Right:
                        // Compensate when out of container bounds
                        var overRight = rects.Container.Width - (margin.Right + rects.Element.Width);
                        margin.Left = Math.Min(overRight, 0);
                        break;
                }
                switch (verticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        // Compensate when out of container bounds
                        var overBottom = rects.Container.Height - (margin.Bottom + rects.Element.Height);
                        margin.Top = Math.Min(overBottom, 0);
                        break;
                    case VerticalAlignment.Center:
                        // Fall back to Stretch alignment
                        verticalAlignment = VerticalAlignment.Stretch;
                        break;
                    case VerticalAlignment.Top:
                        // Compensate when out of container bounds
                        var overTop = rects.Container.Height - (margin.Top + rects.Element.Height);
                        margin.Bottom = Math.Min(overTop, 0);
                        break;
                }
            }
            // update element properties
            element.Margin = margin;
            element.HorizontalAlignment = horizontalAlignment;
            element.VerticalAlignment = verticalAlignment;
            element.Width = MathUtil.Clamp(rects.Element.Width, element.MinimumWidth, element.MaximumWidth);
            element.Height = MathUtil.Clamp(rects.Element.Height, element.MinimumHeight, element.MaximumHeight);

            // update to the real delta that was applied
            delta = new Vector3
            {
                X = parameters.Left ? rects.Element.Left - currentElementRect.Left : rects.Element.Right - currentElementRect.Right,
                Y = parameters.Top ? rects.Element.Top - currentElementRect.Top : rects.Element.Bottom - currentElementRect.Bottom,
                Z = 0
            };
        }
    }
}

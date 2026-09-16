// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Stride.Core.Annotations;
using TreeViewItem = Stride.Core.Presentation.Controls.TreeViewItem;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// An adorner that draws the line at which dragged items would be inserted.
    /// </summary>
    /// <remarks>
    /// If the adorned element is a <see cref="TreeViewItem"/>, the line starts at the indentation of that item and a
    /// marker precedes it. This shows the nesting level at which the items land. The line follows the header of the
    /// item, because the height of an expanded item includes its child items.
    /// </remarks>
    public class InsertAdorner : Adorner
    {
        public InsertAdorner([NotNull] UIElement adornedElement)
            : base(adornedElement)
        {
        }

        public InsertPosition Position { get; set; }

        /// <summary>
        /// Gets or sets the horizontal position at which the line starts, or <see cref="double.NaN"/> to use the
        /// indentation of the adorned item.
        /// </summary>
        /// <remarks>
        /// The gap below a subtree is also the gap below each of its ancestors. Therefore the line can point at an
        /// ancestor of the adorned item, and it then starts at the indentation of that ancestor.
        /// </remarks>
        public double Indent { get; set; } = double.NaN;

        protected override void OnRender(DrawingContext drawingContext)
        {
            var item = AdornedElement as TreeViewItem;
            var width = AdornedElement.RenderSize.Width;
            var height = item?.HeaderHeight ?? AdornedElement.RenderSize.Height;

            double y;
            switch (Position)
            {
                case InsertPosition.Before:
                    y = 0;
                    break;
                case InsertPosition.After:
                    y = height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var brush = DropFeedbackResources.GetInsertLineBrush(AdornedElement);
            var markerRadius = DropFeedbackResources.GetInsertMarkerRadius(AdornedElement);
            var lineStart = 0.0;

            if (item != null)
            {
                lineStart = double.IsNaN(Indent) ? item.Offset : Indent;
                drawingContext.DrawEllipse(brush, null, new Point(lineStart + markerRadius, y), markerRadius, markerRadius);
                lineStart += markerRadius * 2;
            }

            if (lineStart < width)
            {
                var pen = new Pen(brush, DropFeedbackResources.GetInsertLineThickness(AdornedElement));
                drawingContext.DrawLine(pen, new Point(lineStart, y), new Point(width, y));
            }

            base.OnRender(drawingContext);
        }
    }
}

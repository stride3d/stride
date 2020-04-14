// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class InsertAdorner : Adorner
    {
        private readonly Pen renderPen;

        public InsertAdorner([NotNull] UIElement adornedElement)
            : base(adornedElement)
        {
            renderPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 173, 173, 173)), 3);
        }

        public InsertPosition Position { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var adornedElementRect = new Rect(AdornedElement.RenderSize);

            switch (Position)
            {
                case InsertPosition.Before:
                    drawingContext.DrawLine(renderPen, adornedElementRect.TopLeft, adornedElementRect.TopRight);
                    break;
                case InsertPosition.After:
                    drawingContext.DrawLine(renderPen, adornedElementRect.BottomLeft, adornedElementRect.BottomRight);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.OnRender(drawingContext);
        }
    }
}

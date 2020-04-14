// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GraphX;
using Stride.Core.Presentation.Behaviors;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.Graph.Behaviors
{
    public class LinkPreviewBehavior : DeferredBehaviorBase<GraphAreaBase> //Behavior<GraphAreaBase> 
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class LinkPreviewAdorner : Adorner
        {
            private readonly Pen pen_;
            private Point start_ = new Point();
            private Point end_ = new Point();

            public LinkPreviewAdorner(UIElement parent)
                : base(parent)
            {
                // Start disabled
                IsEnabled = false;

                Brush brush = new SolidColorBrush(Colors.White);
                brush.Opacity = 0.5;

                pen_ = new Pen(brush, 1.0);
                pen_.DashStyle = new DashStyle(new double[] { 3, 2, 3, 2 }, 0);
                pen_.DashCap = PenLineCap.Flat;
                pen_.StartLineCap = PenLineCap.Round;
                pen_.EndLineCap = PenLineCap.Round;                
                pen_.Thickness = 4;               
                pen_.Freeze();
            }

            public Point Start {
                get { return start_; }
                set
                {
                    start_ = value;
                    InvalidateVisual();
                }
            }

            public Point End
            {
                get { return end_; }
                set
                {
                    end_ = value;
                    InvalidateVisual();
                }
            }



            /// <summary>
            /// Participates in rendering operations that are directed by the layout system.
            /// </summary>
            /// <param name="drawingContext">The drawing instructions.</param>
            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (IsEnabled)
                {
                    //
                    //Debug.WriteLine("drawing...");
                    drawingContext.DrawLine(pen_, start_, end_);
                }
            }
        }

        #region Static Members
        public static LinkPreviewAdorner LinkPreview;
        #endregion

        #region Members
        private GraphAreaBase graph_area_;
        private AdornerLayer adornLayer;
        #endregion

        #region
        protected override void OnAttachedAndLoaded()
        {
            graph_area_ = AssociatedObject;            

            Dispatcher.InvokeAsync(Register);
        }

        protected override void OnDetachingAndUnloaded()
        {
            Unregister();
        }

        private void Register()
        {
            // Create it!
            if (LinkPreview == null)
            {
                adornLayer = AdornerLayer.GetAdornerLayer(graph_area_);
                if (adornLayer == null)
                {
                    throw new InvalidOperationException("Could not find the adorner layer");
                }                
                LinkPreview = new LinkPreviewAdorner(graph_area_);
                LinkPreview.IsHitTestVisible = false;
                adornLayer.Add(LinkPreview);
            }
        }

        private void Unregister()
        {
            // Destroy it!
            if (LinkPreview != null)
            {
                adornLayer.Remove(LinkPreview);
                adornLayer = null;
                LinkPreview = null;
            }
        }
        #endregion
    }
}

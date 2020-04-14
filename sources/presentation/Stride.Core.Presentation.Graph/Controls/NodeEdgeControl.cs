// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using GraphX.Controls;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Point = System.Windows.Point;

namespace Stride.Core.Presentation.Graph.Controls
{
    /// <summary>
    /// 
    /// </summary>
    [TemplatePart(Name = "PART_linkItemsControl", Type = typeof(ItemsControl))]
    public class NodeEdgeControl : EdgeControl, INotifyPropertyChanged
    {
        private Path path;
        private IEdgePointer arrow;

        #region Dependency Properties
        public static readonly DependencyProperty SourceSlotProperty = DependencyProperty.Register("SourceSlot", typeof(object), typeof(NodeEdgeControl));
        public static readonly DependencyProperty TargetSlotProperty = DependencyProperty.Register("TargetSlot", typeof(object), typeof(NodeEdgeControl));
        public static readonly DependencyProperty LinkStrokeProperty = DependencyProperty.Register("LinkStroke", typeof(Brush), typeof(NodeEdgeControl), new PropertyMetadata(Brushes.LightGray));
        public static readonly DependencyProperty LinkStrokeThicknessProperty = DependencyProperty.Register("LinkStrokeThickness", typeof(double), typeof(NodeEdgeControl), new PropertyMetadata(5.0));
        public static readonly DependencyProperty MouseOverLinkStrokeProperty = DependencyProperty.Register("MouseOverLinkStroke", typeof(Brush), typeof(NodeEdgeControl), new PropertyMetadata(Brushes.Green));
        public static readonly DependencyProperty SelectedLinkStrokeProperty = DependencyProperty.Register("SelectedLinkStroke", typeof(Brush), typeof(NodeEdgeControl), new PropertyMetadata(Brushes.LightGreen));
        #endregion

        #region Members
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="edge"></param>
        /// <param name="showLabels"></param>
        /// <param name="showArrows"></param>
        public NodeEdgeControl(VertexControl source, VertexControl target, object edge, bool showArrows = true)
            : base(source, target, edge, showArrows)
        {
            this.Loaded += OnLoaded;
        }

        /// <summary>
        /// 
        /// </summary>
        public NodeEdgeControl()
            : base()
        {
            this.Loaded += OnLoaded;
        }
        #endregion

        #region Overriden Methods

        public override void Clean()
        {
            if (Source != null)
                ((NodeVertexControl)Source).Connectors.CollectionChanged -= UpdateSourceConnectors;
            if (Target != null)
                ((NodeVertexControl)Target).Connectors.CollectionChanged -= UpdateTargetConnectors;
            base.Clean();
        }

        #endregion

        #region Event Handlers

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template != null)
            {
                path = Template.FindName("PART_edgePath", this) as Path;
                arrow = Template.FindName("PART_EdgePointerForTarget", this) as IEdgePointer;

                //
                UpdateEdge();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.ApplyTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLinkMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RootArea != null && Visibility == Visibility.Visible)
            {
                ((NodeGraphArea)RootArea).OnLinkSelected(sender as FrameworkElement);
            }
            e.Handled = true;
        }
        #endregion

        #region Links & Path Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateLabel"></param>
        public override void UpdateEdge(bool updateLabel = true)
        {
            // TODO Need to let the styling come through for the paths!

            base.UpdateEdge(updateLabel);

            // Template not loaded yet?
            if (path == null)
                return;

            var bezier = new BezierSegment();
            var geometry = new PathGeometry();
            var figure = new PathFigure();

            figure.Segments.Add(bezier);
            geometry.Figures.Add(figure);

            // Find the output slot 
            DependencyObject slot = null;
            if (SourceSlot != null)
            {
                if (((NodeVertexControl)Source).Connectors.TryGetValue(SourceSlot, out slot))
                {
                    var container = (UIElement)VisualTreeHelper.GetChild(Source, 0);
                    var offset = ((UIElement)slot).TransformToAncestor(container).Transform(new Point(0, 0));
                    var location = Source.GetPosition() + (Vector)offset;
                    var halfsize = new Vector((double)slot.GetValue(FrameworkElement.WidthProperty)*0.8,
                        (double)slot.GetValue(FrameworkElement.HeightProperty)/2.0);

                    figure.SetCurrentValue(PathFigure.StartPointProperty, location + halfsize);
                    //figure.StartPoint = location + halfsize;                        
                }
                else
                {
                    // Somehow the slot is not loaded yet
                    // Let's wait for a change in the Connectors collection and trigger UpdateEdge() again
                    ((NodeVertexControl)Source).Connectors.CollectionChanged -= UpdateSourceConnectors;
                    ((NodeVertexControl)Source).Connectors.CollectionChanged += UpdateSourceConnectors;
                    Visibility = Visibility.Collapsed;
                    return;
                }
            }

            // Find input slot
            if (TargetSlot != null)
            {
                if (((NodeVertexControl)Target).Connectors.TryGetValue(TargetSlot, out slot))
                {
                    var container = (UIElement)VisualTreeHelper.GetChild(Target, 0);
                    var offset = ((UIElement)slot).TransformToAncestor(container).Transform(new Point(0, 0));
                    var location = Target.GetPosition() + (Vector)offset;

                    //
                    var halfsize = new Vector((double)slot.GetValue(FrameworkElement.WidthProperty)*0.2,
                        (double)slot.GetValue(FrameworkElement.HeightProperty)/2.0);

                    bezier.SetCurrentValue(BezierSegment.Point3Property, location + halfsize);
                    //bezier.Point3 = location + halfsize;   
                }
                else
                {
                    ((NodeVertexControl)Target).Connectors.CollectionChanged -= UpdateTargetConnectors;
                    ((NodeVertexControl)Target).Connectors.CollectionChanged += UpdateTargetConnectors;
                    Visibility = Visibility.Collapsed;
                    return;
                }
            }

            // Make sure that even if link is going backward, it still goes out of the block in the proper direction
            var length = Math.Max(Math.Abs(bezier.Point3.X - figure.StartPoint.X), 120.0f);
            var curvature = length * 0.4;

            bezier.SetCurrentValue(BezierSegment.Point1Property, new Point(figure.StartPoint.X + curvature, figure.StartPoint.Y));
            //bezier.Point1 = new Point(figure.StartPoint.X + curvature, figure.StartPoint.Y);
            bezier.SetCurrentValue(BezierSegment.Point2Property, new Point(bezier.Point3.X - curvature, bezier.Point3.Y));
            //bezier.Point2 = new Point(bezier.Point3.X - curvature, bezier.Point3.Y);

            //
            path.Data = geometry;
            var direction = bezier.Point3 - bezier.Point2;
            if (direction.Length > MathUtil.ZeroToleranceDouble)
                direction.Normalize();
            else
                direction = new Vector(0, 0);
            EdgePointerForTarget?.Update(bezier.Point3, direction, EdgePointerForTarget.NeedRotation ? (-MathHelper.GetAngleBetweenPoints(bezier.Point3, bezier.Point2).ToDegrees()) : 0);
            //arrow.Data = new PathGeometry { Figures = { GeometryHelper.GenerateOldArrow(bezier.Point2, bezier.Point3) } };

            // TODO Should I be doing this here??? should I be uing setcurrentvalue??
            Visibility = Visibility.Visible;
        }

        private void UpdateSourceConnectors(object sender, TrackingCollectionChangedEventArgs e)
        {
            // Unregister ourselves
            ((NodeVertexControl)Source).Connectors.CollectionChanged -= UpdateSourceConnectors;

            // Update edge
            UpdateEdge();
        }

        private void UpdateTargetConnectors(object sender, TrackingCollectionChangedEventArgs e)
        {
            // Unregister ourselves
            ((NodeVertexControl)Target).Connectors.CollectionChanged -= UpdateTargetConnectors;

            // Update edge
            UpdateEdge();
        }

        #endregion

        #region Properties
        public object SourceSlot { get { return (object)GetValue(SourceSlotProperty); } set { SetValue(SourceSlotProperty, value); } }
        public object TargetSlot { get { return (object)GetValue(TargetSlotProperty); } set { SetValue(TargetSlotProperty, value); } }
        public Brush LinkStroke { get { return (Brush)GetValue(LinkStrokeProperty); } set { SetValue(LinkStrokeProperty, value); } }
        public double LinkStrokeThickness { get { return (double)GetValue(LinkStrokeThicknessProperty); } set { SetValue(LinkStrokeThicknessProperty, value); } }
        public Brush MouseOverLinkStroke { get { return (Brush)GetValue(MouseOverLinkStrokeProperty); } set { SetValue(MouseOverLinkStrokeProperty, value); } }
        public Brush SelectedLinkStroke { get { return (Brush)GetValue(SelectedLinkStrokeProperty); } set { SetValue(SelectedLinkStrokeProperty, value); } }
        internal Path Path => path;
        #endregion

        #region Notify Property Change Method
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

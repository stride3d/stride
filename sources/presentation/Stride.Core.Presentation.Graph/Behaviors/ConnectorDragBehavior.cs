// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using Stride.Core.Presentation.Graph.ViewModel;
using Stride.Core.Presentation.Extensions;
using System.Diagnostics;
using GraphX;
using GraphX.Controls;
using System.Windows.Media;
using Stride.Core.Presentation.Graph.Helper;

namespace Stride.Core.Presentation.Graph.Behaviors
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ConnectorDragBehavior : Behavior<FrameworkElement>
    {
        #region Dependency Properties
        public static DependencyProperty NodeProperty = DependencyProperty.Register("Node", typeof(NodeVertex), typeof(ConnectorDragBehavior));
        public static DependencyProperty SlotProperty = DependencyProperty.Register("Slot", typeof(object), typeof(ConnectorDragBehavior));
        #endregion

        #region Members
        private bool mouse_down_before_all_ = false;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public ConnectorDragBehavior()
        {
            // nothing
        }
        #endregion

        #region Attach & Detach Methods
        /// <summary>
        /// 
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnMouseLeftDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseUp += OnMouseUp;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseUp -= OnMouseUp;
            base.OnDetaching();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseLeftDown(object sender, MouseEventArgs e)
        {
            var uiElement = sender as UIElement;
            var element = e.Source as FrameworkElement;
            var draggedItem = new Tuple<NodeVertex, object>(Node, Slot);

            if (uiElement == null || draggedItem.Item1 == null || draggedItem.Item2 == null) { return; }

            DragStartPoint = AssociatedObject.PointToScreen(e.GetPosition(AssociatedObject));
            DraggedItem = draggedItem;
            DraggedUIElement = uiElement;

            mouse_down_before_all_ = true;

            e.Handled = true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var uiElement = sender as UIElement;
            if (!mouse_down_before_all_ || DraggedItem == null || DraggedUIElement == null || DraggedUIElement != uiElement || e.LeftButton != MouseButtonState.Pressed)
            {
                DraggedUIElement = null;
                DraggedItem = null;
                DragStartPoint = new Point();
                return;
            }

            DraggedUIElement.GiveFeedback += OnGiveFeedback;
            DraggedUIElement.QueryContinueDrag += OnQueryContinueDrag;
            DragDrop.DoDragDrop(DraggedUIElement, DraggedItem, DragDropEffects.All);
            DraggedUIElement.GiveFeedback -= OnGiveFeedback;
            DraggedUIElement.QueryContinueDrag -= OnQueryContinueDrag;

            mouse_down_before_all_ = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            // TODO this is not called for some reason. must've been handled before it gets to here
            var element = sender as UIElement;
            if (element == null) { return; }
            
            DraggedUIElement = null;
            DraggedItem = null;
            DragStartPoint = new Point();

            mouse_down_before_all_ = false;
        }

        private void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (LinkPreviewBehavior.LinkPreview == null) { return; }
            
            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                LinkPreviewBehavior.LinkPreview.SetCurrentValue(UIElement.IsEnabledProperty, false); //.IsEnabled = false;
                LinkPreviewBehavior.LinkPreview.InvalidateVisual();
            }            
        }

        private void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {            
            if (LinkPreviewBehavior.LinkPreview == null) { return; }

            // TODO cached this maybe. not efficient here
            GraphAreaBase graphArea = AssociatedObject.FindVisualParentOfType<GraphAreaBase>();
            ZoomControl zoomControl = graphArea.FindVisualParentOfType<ZoomControl>();

            Vector halfsize = new Vector((double)(e.OriginalSource as UIElement).GetValue(FrameworkElement.WidthProperty) / 2.0,
                                         (double)(e.OriginalSource as UIElement).GetValue(FrameworkElement.HeightProperty) / 2.0);

            LinkPreviewBehavior.LinkPreview.SetCurrentValue(UIElement.IsEnabledProperty, true); 
            LinkPreviewBehavior.LinkPreview.Start = (e.OriginalSource as UIElement).TranslatePoint(new Point(0, 0), graphArea) + halfsize;
            LinkPreviewBehavior.LinkPreview.End = zoomControl.TranslatePoint(MouseHelper.GetMousePosition(zoomControl), graphArea);         
        }
        #endregion

        #region Static Properties
        public static Point DragStartPoint { get; private set; }
        public static object DraggedItem { get; private set; }
        public static UIElement DraggedUIElement { get; private set; }
        #endregion

        #region Properties
        public NodeVertex Node { get { return (NodeVertex)GetValue(NodeProperty); } set { SetValue(NodeProperty, value); } }
        public object Slot { get { return GetValue(SlotProperty); } set { SetValue(SlotProperty, value); } }
        #endregion
    }
}

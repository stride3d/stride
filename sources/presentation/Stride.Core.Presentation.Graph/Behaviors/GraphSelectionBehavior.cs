// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xenko.Core.Presentation.Behaviors;
using Xenko.Core.Presentation.Extensions;
using System.Linq;
using GraphX;
using System.Diagnostics;
using Microsoft.Xaml.Behaviors;
using GraphX.Controls;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using GraphX.Controls.Models;
using GraphX.PCL.Common.Models;
using QuickGraph;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Graph.ViewModel;

namespace Xenko.Core.Presentation.Graph.Behaviors
{
    /// <summary>
    /// 
    /// </summary>
    public class GraphSelectionBehavior : DeferredBehaviorBase<GraphArea<NodeVertex, NodeEdge, BidirectionalGraph<NodeVertex, NodeEdge>>> 
    {
        #region Dependency Properties
        public static readonly DependencyProperty SelectedVertexItemsProperty = DependencyProperty.Register("SelectedVertexItems", typeof(IList), typeof(GraphSelectionBehavior), new PropertyMetadata(null, OnSelectedVertexItemsChanged));
        public static readonly DependencyProperty SelectedEdgeItemsProperty = DependencyProperty.Register("SelectedEdgeItems", typeof(IList), typeof(GraphSelectionBehavior), new PropertyMetadata(null, OnSelectedEdgeItemsChanged));
        #endregion

        #region Static Dependency Property Event Handler
        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectedVertexItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (GraphSelectionBehavior)d;

            if (e.OldValue != null)
            {
                var oldList = e.OldValue as INotifyCollectionChanged;
                if (oldList != null) { oldList.CollectionChanged -= behavior.OnSelectedVertexItemsCollectionChanged; }
            }

            if (e.NewValue != null)
            {
                var notifyChanged = e.NewValue as INotifyCollectionChanged;
                var newList = e.NewValue as IEnumerable<VertexBase>;
                if ((notifyChanged != null) && (newList != null))
                {
                    notifyChanged.CollectionChanged += behavior.OnSelectedVertexItemsCollectionChanged;
                    if (behavior.graph_area_ != null)
                    {
                        // Remove any items not in new list
                        behavior.selected_vertices_.RemoveWhere(x => !newList.Contains(x));

                        // Add any items that's in the new list
                        foreach (var newlySelectedItem in newList.Where(x => !behavior.selected_vertices_.Contains(x)))
                        {
                            behavior.selected_vertices_.Add(newlySelectedItem);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectedEdgeItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (GraphSelectionBehavior)d;

            if (e.OldValue != null)
            {
                var oldList = e.OldValue as INotifyCollectionChanged;
                if (oldList != null) { oldList.CollectionChanged -= behavior.OnSelectedEdgeItemsCollectionChanged; }
            }

            if (e.NewValue != null)
            {
                var notifyChanged = e.NewValue as INotifyCollectionChanged;
                var newList = e.NewValue as IEnumerable<NodeEdge>;
                if ((notifyChanged != null) && (newList != null))
                {
                    notifyChanged.CollectionChanged += behavior.OnSelectedEdgeItemsCollectionChanged;
                    if (behavior.graph_area_ != null)
                    {
                        // Remove any items not in new list
                        behavior.selected_edges_.RemoveWhere(x => !newList.Contains(x));

                        // Add any items that's in the new list
                        foreach (var newlySelectedItem in newList.Where(x => !behavior.selected_edges_.Contains(x)))
                        {
                            behavior.selected_edges_.Add(newlySelectedItem);
                        }
                    }
                }
            }
        }
        #endregion        

        #region Members
        protected GraphArea<NodeVertex, NodeEdge, BidirectionalGraph<NodeVertex, NodeEdge>> graph_area_;
        protected ZoomControl zoom_control_;

        private ObservableCollection<VertexBase> selected_vertices_ = new ObservableCollection<VertexBase>();
        private ObservableCollection<object> selected_edges_ = new ObservableCollection<object>();
        private Dictionary<VertexBase, VertexControl> vertex_controls_ = new Dictionary<VertexBase, VertexControl>();
        private Dictionary<object, EdgeControl> edge_controls_ = new Dictionary<object, EdgeControl>();

        private bool updating_vertex_collection_;
        private bool updating_edge_collection_;

        private VertexControl last_vertex_control_;
        private Point last_vertex_position_;
        private Point last_pan_position_;

        private NodeGraphBehavior graphBehavior;

        #endregion  
                
        #region Attach & Detach Methods
        /// <summary>
        /// 
        /// </summary>
        protected override void OnAttachedAndLoaded()
        {                               
            Register();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDetachingAndUnloaded()
        {
            Unregister();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Register()
        {
            //
            graph_area_ = AssociatedObject;     

            // Find the graph behavior
            graphBehavior = Interaction.GetBehaviors(graph_area_).OfType<NodeGraphBehavior>().FirstOrDefault();
            if (graphBehavior != null)
            {
                graphBehavior.VerticesCollectionChanged += GraphBehavior_VerticesCollectionChanged;
                graphBehavior.EdgesCollectionChanged += GraphBehavior_EdgesCollectionChanged;
            }

            // Travel up the visual tree and get the zoom control
            // If the zoom control doesn't exist, then we can't use this behavior
            // TODO: Done on logical tree for now because visual tree is not valid during behavior OnAttachAndLoaded()
            zoom_control_ = graph_area_.FindLogicalParentOfType<ZoomControl>();
            if (zoom_control_ == null)
            {
                // TODO throw exception!
            }
            
            graph_area_.VertexSelected += OnVertexSelected;
            graph_area_.VertexMouseUp += OnVertexMouseLeftButtonUp;
            graph_area_.EdgeSelected += OnEdgeSelected;            
            zoom_control_.AreaSelected += OnVertexAreaSelected;
            zoom_control_.MouseLeftButtonUp += OnMouseLeftButtonUp;
            zoom_control_.MouseLeftButtonDown += OnMouseLeftButtonDown;
            selected_vertices_.CollectionChanged += OnSelectedVerticesChanged;
            selected_edges_.CollectionChanged += OnSelectedEdgesChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        private void Unregister()
        {
            if (graphBehavior != null)
            {
                graphBehavior.VerticesCollectionChanged -= GraphBehavior_VerticesCollectionChanged;
                graphBehavior.EdgesCollectionChanged -= GraphBehavior_EdgesCollectionChanged;
                graphBehavior = null;
            }

            // Remove all the event handlers so this instance can be reclaimed by the GC.
            graph_area_.VertexSelected -= OnVertexSelected;
            graph_area_.VertexMouseUp -= OnVertexMouseLeftButtonUp;
            graph_area_.EdgeSelected -= OnEdgeSelected;
            zoom_control_.AreaSelected -= OnVertexAreaSelected;
            zoom_control_.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            zoom_control_.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            selected_vertices_.CollectionChanged -= OnSelectedVerticesChanged;
            selected_edges_.CollectionChanged -= OnSelectedEdgesChanged;

            zoom_control_ = null;
            graph_area_ = null;
        }
        #endregion

        #region Collection Changed Event Handlers
        private void GraphBehavior_VerticesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Remove everything deleted from selection
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    foreach (var oldItem in e.OldItems)
                        SelectedVertices.Remove((VertexBase)oldItem);
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    var vertices = (IList)sender;
                    for (int index = 0; index < SelectedVertices.Count; index++)
                    {
                        if (!vertices.Contains(SelectedVertices[index]))
                            SelectedVertices.RemoveAt(index--);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void GraphBehavior_EdgesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Remove everything deleted from selection
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    foreach (var oldItem in e.OldItems)
                        SelectedEdges.Remove(oldItem);
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    var edges = (IList)sender;
                    for (int index = 0; index < SelectedVertices.Count; index++)
                    {
                        if (!edges.Contains(SelectedEdges[index]))
                            SelectedEdges.RemoveAt(index--);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectedVertexItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //SanityCheck();

            if (updating_vertex_collection_)
                return;

            // Change from dependency property collection -> internal collection
            if (graph_area_ != null)
            {
                updating_vertex_collection_ = true;

                VertexControl[] vertexControls = graph_area_.GetAllVertexControls();

                // TODO Many different cases for this. Might to return to this in the future.
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearVertexSelection();
                }

                if (e.NewItems != null)
                {
                    foreach (var addedItem in e.NewItems.Cast<VertexBase>().Where(x => !selected_vertices_.Contains(x)))
                    {
                        var control = vertexControls.FirstOrDefault(x => x.Vertex == addedItem);
                        SelectVertex(control, false);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (var removedItem in e.OldItems.Cast<VertexBase>().Where(x => selected_vertices_.Contains(x)))
                    {
                        var control = vertexControls.FirstOrDefault(x => x.Vertex == removedItem);
                        UnselectVertex(control);
                    }
                }

                updating_vertex_collection_ = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectedVerticesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (updating_vertex_collection_)
                return;

            // Change from internal collection -> dependency property collection 
            if (SelectedVertexItems != null)
            {
                updating_vertex_collection_ = true;

                // TODO Many different cases for this. Might to return to this in the future.
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    SelectedVertexItems.Clear();
                }

                if (e.OldItems != null)
                {
                    foreach (var removedItem in e.OldItems.Cast<object>())
                    {
                        SelectedVertexItems.Remove(removedItem);
                    }
                }

                if (e.NewItems != null)
                {
                    // TODO For now, change to observablelist later
                    //SelectedVertexItems.AddRange(e.NewItems.Cast<object>().Where(x => !SelectedVertexItems.Contains(x)));
                    foreach (var item in e.NewItems)
                    {
                        if (!SelectedVertexItems.Contains(item))
                        {
                            SelectedVertexItems.Add(item);
                        }
                    }
                }
                updating_vertex_collection_ = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectedEdgeItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //SanityCheck();

            if (updating_edge_collection_)
                return;

            // Change from dependency property collection -> internal collection
            if (graph_area_ != null)
            {
                updating_edge_collection_ = true;

                var edgeControls = graph_area_.EdgesList.Values.ToList();

                // TODO Many different cases for this. Might to return to this in the future.
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearEdgeSelection();
                }

                if (e.NewItems != null)
                {
                    foreach (var addedItem in e.NewItems.Cast<object>().Where(x => !selected_edges_.Contains(x)))
                    {
                        var control = edgeControls.FirstOrDefault(x => x.Edge == addedItem);
                        SelectEdge(control, false);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (var removedItem in e.OldItems.Cast<object>().Where(x => selected_edges_.Contains(x)))
                    {
                        var control = edgeControls.FirstOrDefault(x => x.Edge == removedItem);
                        UnselectEdge(control);
                    }
                }

                updating_edge_collection_ = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectedEdgesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (updating_edge_collection_)
                return;

            // Change from internal collection -> dependency property collection 
            if (SelectedEdgeItems != null)
            {
                updating_edge_collection_ = true;

                // TODO Many different cases for this. Might to return to this in the future.
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    SelectedEdgeItems.Clear();
                }

                if (e.OldItems != null)
                {
                    foreach (var removedItem in e.OldItems.Cast<object>())
                    {
                        SelectedEdgeItems.Remove(removedItem);
                    }
                }

                if (e.NewItems != null)
                {
                    // TODO For now, change to observablelist later
                    //SelectedEdgeItems.AddRange(e.NewItems.Cast<object>().Where(x => !SelectedEdgeItems.Contains(x)));
                    foreach (var item in e.NewItems)
                    {
                        if (!SelectedEdgeItems.Contains(item))
                        {
                            SelectedEdgeItems.Add(item);
                        }
                    }
                }
                updating_edge_collection_ = false;
            }
        }

        #endregion

        #region Selection Changed Event Handlers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void OnVertexSelected(object sender, VertexSelectedEventArgs args)
        {
            // Toggle and append selection occurs in area selection only. No need to worry about it here
            var control = args.VertexControl;
            var vertex = args.VertexControl.Vertex as VertexBase;

            if (args.MouseArgs.LeftButton == MouseButtonState.Pressed)
            {
                // Is this a new selection and/or a toggle selection?
                if (!selected_vertices_.Contains(vertex))
                {
                    ClearSelection();

                    // User is only selecting one vertex
                    SelectVertex(control, false);
                }

                last_vertex_control_ = control;
                //last_vertex_position_.X = last_vertex_control_.GetPosition().X;
                //last_vertex_position_.Y = last_vertex_control_.GetPosition().Y;
                var position = zoom_control_.TranslatePoint(args.MouseArgs.GetPosition(zoom_control_), graph_area_);
                last_vertex_position_.X = position.X;
                last_vertex_position_.Y = position.Y;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void OnEdgeSelected(object sender, EdgeSelectedEventArgs args)
        {
            // Toggle and append selection occurs in area selection only. No need to worry about it here
            var control = args.EdgeControl;
            var edge = args.EdgeControl.Edge;

            // Is this a new selection and/or a toggle selection?
            if (!selected_edges_.Contains(edge))
            {
                ClearSelection();

                // User is only selecting one vertex
                SelectEdge(control, false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            last_pan_position_.X = zoom_control_.TranslateX;
            last_pan_position_.Y = zoom_control_.TranslateY;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool append = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool toggle = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            // Clicking an empty area -> Clear selection
            // Dragging (panning) does not clear selection -> Do nothing
            if ((last_pan_position_.X != zoom_control_.TranslateX) || (last_pan_position_.Y != zoom_control_.TranslateY))
            {
                return;
            }

            var position = zoom_control_.TranslatePoint(e.GetPosition(zoom_control_), graph_area_);
            HitTestResult result = VisualTreeHelper.HitTest(graph_area_, position);
            if (result == null)
            {
                if (!append && !toggle)
                {
                    // Clear everything!
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void OnVertexMouseLeftButtonUp(object sender, VertexSelectedEventArgs args)
        {
            // nothing for now
            bool append = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool toggle = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            // Vertex group dragging -> Don't clear selection
            if (last_vertex_control_ != null)
            {
                // Compare the last position and current position
                // If they are the same then you move to just selection

                var position = zoom_control_.TranslatePoint(args.MouseArgs.GetPosition(zoom_control_), graph_area_);

                if (last_vertex_position_ == position)
                    //if (last_vertex_position_ == last_vertex_control_.GetPosition())
                {
                    ClearSelection();
                    SelectVertex(last_vertex_control_, false);
                }

                last_vertex_control_ = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void OnVertexAreaSelected(object sender, AreaSelectedEventArgs args)
        {
            bool append = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool toggle = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            var area = args.Rectangle;
            var point = new Point(args.Rectangle.X, args.Rectangle.Y);

            // Probably will never hit this
            if (!append && !toggle)
            {
                ClearVertexSelection();
            }

            foreach (VertexControl control in graph_area_.GetAllVertexControls())
            {
                var offset = control.GetPosition();
                var extent = new Rect(offset.X, offset.Y, control.ActualWidth, control.ActualHeight);

                // If the area is empty, then it is a single seletion case
                if (area.Width == 0.0 || area.Height == 0.0)
                {
                    if (extent.Contains(point))
                    {
                        SelectVertex(control, toggle);

                        // Should only be one! No need to traverse the whole thing!
                        break;
                    }
                }
                else
                {
                    if (extent.IntersectsWith(area))
                    {
                        SelectVertex(control, toggle);
                    }
                }
            }
        }

        #endregion

        #region Selection Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="toggle"></param>
        public virtual void SelectVertex(VertexControl control, bool toggle)
        {
            if (control == null)
            {
                return;
            }

            VertexBase vertex = (VertexBase)control.Vertex;
            if (selected_vertices_.Contains(vertex))
            {
                if (toggle)
                {
                    control.SetValue(Selector.IsSelectedProperty, false);
                    DragBehaviour.SetIsTagged(control, false);

                    selected_vertices_.Remove(vertex);
                    vertex_controls_.Remove(vertex);
                }
            }
            else
            {
                control.SetValue(Selector.IsSelectedProperty, true);
                DragBehaviour.SetIsTagged(control, true);

                selected_vertices_.Add(vertex);
                vertex_controls_.Add(vertex, control);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        public virtual void UnselectVertex(VertexControl control)
        {
            if (control == null)
            {
                return;
            }

            VertexBase vertex = (VertexBase)control.Vertex;
            control.SetValue(Selector.IsSelectedProperty, false);
            DragBehaviour.SetIsTagged(control, false);

            selected_vertices_.Remove(vertex);
            vertex_controls_.Remove(vertex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="toggle"></param>
        public virtual void SelectEdge(EdgeControl control, bool toggle)
        {
            if (control == null)
            {
                return;
            }

            object edge = control.Edge;
            if (selected_edges_.Contains(edge))
            {
                if (toggle)
                {
                    control.SetValue(Selector.IsSelectedProperty, false);

                    selected_edges_.Remove(edge);
                    edge_controls_.Remove(edge);
                }
            }
            else
            {
                control.SetValue(Selector.IsSelectedProperty, true);

                selected_edges_.Add(edge);
                edge_controls_.Add(edge, control);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        public virtual void UnselectEdge(EdgeControl control)
        {
            if (control == null)
            {
                return;
            }

            control.SetValue(Selector.IsSelectedProperty, false);
            selected_edges_.Remove(control.Edge);
            edge_controls_.Remove(control.Edge);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearSelection()
        {
            // Clear everything!
            // Order matters: edge -> vertex
            ClearEdgeSelection();
            ClearVertexSelection();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearVertexSelection()
        {
            if (selected_vertices_.Count > 0)
            {
                foreach (KeyValuePair<VertexBase, VertexControl> entry in vertex_controls_)
                {
                    entry.Value.SetValue(Selector.IsSelectedProperty, false);
                    DragBehaviour.SetIsTagged(entry.Value, false);
                }

                selected_vertices_.Clear();
                vertex_controls_.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearEdgeSelection()
        {
            if (selected_edges_.Count > 0)
            {
                foreach (KeyValuePair<object, EdgeControl> entry in edge_controls_)
                {
                    entry.Value.SetValue(Selector.IsSelectedProperty, false);
                }

                selected_edges_.Clear();
                edge_controls_.Clear();
            }
        }

        #endregion

        #region Properties

        public IList SelectedVertexItems
        {
            get { return (IList)GetValue(SelectedVertexItemsProperty); }
            set { SetValue(SelectedVertexItemsProperty, value); }
        }

        public IList SelectedEdgeItems
        {
            get { return (IList)GetValue(SelectedEdgeItemsProperty); }
            set { SetValue(SelectedEdgeItemsProperty, value); }
        }

        public ObservableCollection<VertexBase> SelectedVertices
        {
            get { return selected_vertices_; }
        }

        public ObservableCollection<object> SelectedEdges
        {
            get { return selected_edges_; }
        }

        #endregion
    }
}

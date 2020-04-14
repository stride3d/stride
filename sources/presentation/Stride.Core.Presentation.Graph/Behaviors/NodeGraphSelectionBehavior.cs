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
using Xenko.Core.Presentation.Graph.ViewModel;
using Xenko.Core.Presentation.Graph.Controls;
using Xenko.Core.Presentation.Graph.Helper;
using System.Windows.Shapes;

namespace Xenko.Core.Presentation.Graph.Behaviors
{
    public class NodeGraphSelectionBehavior : GraphSelectionBehavior
    {
        #region Dependency Properties
        public static readonly DependencyProperty SelectedLinkItemsProperty = DependencyProperty.Register("SelectedLinkItems", typeof(IList), typeof(NodeGraphSelectionBehavior), new PropertyMetadata(null, OnSelectedLinkItemsChanged));
        #endregion

        #region Static Dependency Property Event Handler
        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectedLinkItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (NodeGraphSelectionBehavior)d;

            if (e.OldValue != null)
            {
                var oldList = e.OldValue as INotifyCollectionChanged;
                if (oldList != null) { oldList.CollectionChanged -= behavior.OnSelectedLinkItemsCollectionChanged; }
            }

            if (e.NewValue != null)
            {
                var notifyChanged = e.NewValue as INotifyCollectionChanged;
                var newList = e.NewValue as IList<LinkInfo>;
                if ((notifyChanged != null) && (newList != null))
                {
                    notifyChanged.CollectionChanged += behavior.OnSelectedLinkItemsCollectionChanged;
                    if (behavior.AssociatedObject != null)
                    {
                        // Mirror dependency property collection to internal collection
                        LinkInfo[] currentlySelectedItems = behavior.selected_links_.Cast<LinkInfo>().ToArray();

                        // Remove any items not in new list
                        foreach (var currentlySelectedItem in currentlySelectedItems.Where(x => !newList.Contains(x)))
                        {
                            behavior.selected_links_.Remove(currentlySelectedItem);
                        }

                        // Add any items that's in the new list
                        foreach (var newlySelectedItem in newList.Where(x => !behavior.selected_links_.Contains(x)))
                        {
                            behavior.selected_links_.Add(newlySelectedItem);
                        }
                    }
                }
            }
        }        
        #endregion

        #region LinkInfo
        public class LinkInfo
        {
            public NodeEdge Edge { get; set; }
            public Tuple<object, object> Link { get; set; }
        };
        #endregion

        #region Members
        private ObservableCollection<LinkInfo> selected_links_ = new ObservableCollection<LinkInfo>();
        private Dictionary<LinkInfo, UIElement> link_controls_ = new Dictionary<LinkInfo, UIElement>();     // key:object(Tuple<object, object>), value:Path
        private bool updating_link_collection_;
        #endregion                  
        
        #region Attach & Detach Methods
        /// <summary>
        /// 
        /// </summary>
        protected override void OnAttachedAndLoaded()
        {
            base.OnAttachedAndLoaded();

            selected_links_.CollectionChanged += OnSelectedLinksChanged;
            (graph_area_ as NodeGraphArea).LinkSelected += OnLinkSelected;
            zoom_control_.AreaSelected += OnLinkAreaSelected;       
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDetachingAndUnloaded()
        {
            base.OnDetachingAndUnloaded();

            selected_links_.CollectionChanged -= OnSelectedLinksChanged;
            (graph_area_ as NodeGraphArea).LinkSelected -= OnLinkSelected;
            zoom_control_.AreaSelected -= OnLinkAreaSelected;       
        }
        #endregion

        #region Collection Changed Event Handlers
        private void OnSelectedLinkItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //SanityCheck();

            if (updating_link_collection_) { return; }

            // Change from dependency property collection -> internal collection
            if (AssociatedObject != null)
            {
                var edgeControls = graph_area_.EdgesList.Values.ToList();

                // TODO Many different cases for this. Might to return to this in the future.
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearLinkSelection();
                }

                if (e.NewItems != null)
                {
                    foreach (var addedItem in e.NewItems.Cast<LinkInfo>().Where(x => !selected_links_.Contains(x)))
                    {
                        var control = edgeControls.FirstOrDefault(x => x.Edge == addedItem.Edge) as NodeEdgeControl;
                        SelectLink(control, addedItem, false);
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (var removedItem in e.OldItems.Cast<LinkInfo>().Where(x => selected_links_.Contains(x)))
                    {
                        var control = edgeControls.FirstOrDefault(x => x.Edge == removedItem.Edge) as NodeEdgeControl;                        
                        UnselectLink(control, removedItem);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectedLinksChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Change from internal collection -> dependency property collection 
            if (SelectedLinkItems != null)
            {
                updating_link_collection_ = true;

                // TODO Many different cases for this. Might to return to this in the future.
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    SelectedLinkItems.Clear();
                }

                if (e.OldItems != null)
                {
                    foreach (var removedItem in e.OldItems.Cast<object>())
                    {
                        SelectedLinkItems.Remove(removedItem);
                    }
                }

                if (e.NewItems != null)
                {
                    // TODO For now, change to observablelist later
                    //SelectedLinkItems.AddRange(e.NewItems.Cast<object>().Where(x => !SelectedLinkItems.Contains(x)));
                    foreach (var item in e.NewItems)
                    {
                        if (!SelectedLinkItems.Contains(item))
                        {
                            SelectedLinkItems.Add(item);
                        }
                    }
                }
                updating_link_collection_ = false;
            }
        }        
        #endregion

        #region Selection Changed Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void OnLinkSelected(object sender, LinkSelectedEventArgs args)
        {
            // Travel up the visual tree to find the edge
            FrameworkElement element = args.Link;
            NodeEdgeControl control = element.FindVisualParentOfType<NodeEdgeControl>();
            NodeEdge edge = control.Edge as NodeEdge;
            Tuple<object, object> link = element.DataContext as Tuple<object, object>;

            LinkInfo info = selected_links_.FirstOrDefault(x => (x.Link == link) && (x.Edge == edge));
            if (info == null)
            {
                ClearSelection();
                info = new LinkInfo() { Edge = edge, Link = link };
                SelectLink(control, info, false);
            }
        }

        protected void OnLinkAreaSelected(object sender, AreaSelectedEventArgs args)
        {
            bool append = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool toggle = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            var area = args.Rectangle;
            var point = new Point(args.Rectangle.X, args.Rectangle.Y);

            // Probably will never hit this
            if (!append && !toggle)
            {
                ClearLinkSelection();
            }

            foreach (NodeEdgeControl control in graph_area_.EdgesList.Values)
            {
                if (VisualTreeHelper.HitTest(control.Path, point) != null)
                {
                    // TODO
                    //LinkInfo info = selected_links_.FirstOrDefault(x => (x.Link == control.Key) && (x.Edge == control.Edge));
                    //if (info == null)
                    //{
                    //    info = new LinkInfo() { Edge = control.Edge as NodeEdge, Link = linkpath.Key };                                
                    //}
                    //
                    //SelectLink(control as NodeEdgeControl, info, toggle);
                    break;
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
        public virtual void SelectLink(NodeEdgeControl control, LinkInfo info, bool toggle)
        {
            if (control == null) { return; }

            object edge = (object)control.Edge;
            if (selected_links_.Contains(info))
            {
                if (toggle)
                {
                    control.Path.SetValue(Selector.IsSelectedProperty, false);

                    selected_links_.Remove(info);
                    link_controls_.Remove(info);
                }
            }
            else
            {
                control.Path.SetValue(Selector.IsSelectedProperty, true);

                selected_links_.Add(info);
                link_controls_.Add(info, control.Path);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        public virtual void UnselectLink(NodeEdgeControl control, LinkInfo info)
        {
            if (control == null) { return; }

            object edge = (object)control.Edge;
            control.Path.SetValue(Selector.IsSelectedProperty, false);

            selected_links_.Remove(info);
            link_controls_.Remove(info);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void ClearSelection()
        {
            ClearLinkSelection();

            base.ClearSelection();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearLinkSelection()
        {
            if (selected_links_.Count > 0)
            {
                foreach (KeyValuePair<LinkInfo, UIElement> entry in link_controls_)
                {
                    entry.Value.SetValue(Selector.IsSelectedProperty, false);
                }

                selected_links_.Clear();
                link_controls_.Clear();
            }
        }
        #endregion

        #region Properties
        public IList SelectedLinkItems { get { return (IList)GetValue(SelectedLinkItemsProperty); } set { SetValue(SelectedLinkItemsProperty, value); } }
        public ObservableCollection<LinkInfo> SelectedLinks { get { return selected_links_; } }
        #endregion

    }
}

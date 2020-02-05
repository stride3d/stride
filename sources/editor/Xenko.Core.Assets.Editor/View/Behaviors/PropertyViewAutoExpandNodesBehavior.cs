// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Controls;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// This behavior manages auto-expanding of property items of a <see cref="PropertyView"/> that is bound to an <see cref="GraphViewModel"/>.
    /// An existing item will be expanded if  the associated node is modified, and a newly created will be expanded if it is the only child of its parent.
    /// </summary>
    public class PropertyViewAutoExpandNodesBehavior : Behavior<PropertyView>
    {
        private readonly List<PropertyViewItem> expandedItems = new List<PropertyViewItem>();
        // These are static so that we remember their state for the entire session.
        private static readonly HashSet<string> expandedPropertyPaths = new HashSet<string>();
        private static readonly HashSet<string> collapsedPropertyPaths = new HashSet<string>();

        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(GraphViewModel), typeof(PropertyViewAutoExpandNodesBehavior), new PropertyMetadata(null, OnViewModelChanged));

        /// <summary>
        /// Gets or sets the <see cref="GraphViewModel"/> associated to this behavior.
        /// </summary>
        public GraphViewModel ViewModel { get { return (GraphViewModel)GetValue(ViewModelProperty); } set { SetValue(ViewModelProperty, value); } }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            if (ViewModel != null)
            {
                ViewModel.NodeValueChanged += NodeValueChanged;
            }
            AssociatedObject.PrepareItem += PrepareItem;
            AssociatedObject.ClearItem += ClearItem;
            base.OnAttached();
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PrepareItem -= PrepareItem;
            AssociatedObject.ClearItem -= ClearItem;
            if (ViewModel != null)
            {
                ViewModel.NodeValueChanged -= NodeValueChanged;
            }
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (PropertyViewAutoExpandNodesBehavior)d;
            var previousViewModel = e.OldValue as GraphViewModel;
            if (previousViewModel != null)
                previousViewModel.NodeValueChanged -= behavior.NodeValueChanged;

            var newViewModel = e.NewValue as GraphViewModel;
            if (newViewModel != null)
                newViewModel.NodeValueChanged += behavior.NodeValueChanged;
        }

        private void PrepareItem(object sender, PropertyViewItemEventArgs e)
        {
            RegisterItem(e.Container);
            foreach (var propertyItem in expandedPropertyPaths.ToList().Select(GetPropertyItem).NotNull())
            {
                propertyItem.IsExpanded = true;
            }
            foreach (var propertyItem in collapsedPropertyPaths.ToList().Select(GetPropertyItem).NotNull())
            {
                propertyItem.IsExpanded = false;
            }
            Dispatcher.BeginInvoke(new Action(() => ExpandSingleProperties(e.Container)));
        }

        private PropertyViewItem GetPropertyItem(string propertyPath)
        {
            IReadOnlyCollection<PropertyViewItem> currentPropertyCollection = AssociatedObject.Properties;
            string[] members = propertyPath.Split('.');
            PropertyViewItem item = null;

            foreach (var member in members.Skip(1))
            {
                item = currentPropertyCollection.FirstOrDefault(x => { var node = x.DataContext as NodeViewModel; return node != null && node.Name == member; });
                if (item == null)
                    return null;

                currentPropertyCollection = item.Properties;
            }
            return item;
        }

        private void ClearItem(object sender, PropertyViewItemEventArgs e)
        {
            expandedItems.Clear();
            UnregisterItem(e.Container);
        }

        private void RegisterItem(PropertyViewItem item)
        {
            item.Expanded += ExpandedChanged;
            item.Collapsed += ExpandedChanged;

            foreach (var container in item.Properties)
            {
                RegisterItem(container);
            }
        }

        private void UnregisterItem(PropertyViewItem item)
        {
            item.Expanded -= ExpandedChanged;
            item.Collapsed -= ExpandedChanged;
            foreach (var container in item.Properties)
            {
                UnregisterItem(container);
            }
        }

        private void ExpandedChanged(object sender, RoutedEventArgs e)
        {
            var item = (PropertyViewItem)sender;
            var propertyPath = GetNode(item).DisplayPath;
            if (item.IsExpanded)
            {
                expandedPropertyPaths.Add(propertyPath);
                collapsedPropertyPaths.Remove(propertyPath);
            }
            else
            {
                expandedPropertyPaths.Remove(propertyPath);
                collapsedPropertyPaths.Add(propertyPath);
            }
        }

        private void ExpandSingleProperties(PropertyViewItem item)
        {
            var node = GetNode(item);
            // The data context of the item might be a "disconnected object"
            if (node == null)
                return;

            var rule = GetRule(node);

            if (node.Parent != null)
            {
                switch (rule)
                {
                    case ExpandRule.Always:
                        // Always expand nodes that have this rule (without tracking them)
                        item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                        break;
                    case ExpandRule.Never:
                        // Always collapse nodes that have this rule (without tracking them)
                        item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, false);
                        break;
                    case ExpandRule.Once:
                        {
                            // Expand nodes that have this rule only if they have never been collapsed previously
                            var propertyPath = GetNode(item).DisplayPath;
                            if (!collapsedPropertyPaths.Contains(propertyPath))
                            {
                                item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                                break;
                            }
                        }
                        goto default;
                    default:
                        {
                            // If the node was saved as expanded, persist this behavior
                            var propertyPath = GetNode(item).DisplayPath;
                            if (expandedPropertyPaths.Contains(propertyPath))
                            {
                                item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                            }
                            else if (node.Parent.Children.Count == 1)
                            {
                                // If the node is an only child, let's expand it
                                item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                                // And keep a track of it, in case it has some siblings incoming
                                expandedItems.Add(item);
                            }
                            else
                            {
                                // If one of its siblings has been expanded because it was an only child at the time it was created, let's unexpand it.
                                // This will prevent to always have the first item expanded since the property items are generated as soon as a child is added.
                                expandedItems.Where(x => GetNode(x).Parent == node.Parent).ForEach(x => x.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, false));
                            }
                        }
                        break;
                }
            }

            foreach (var container in item.Properties)
            {
                ExpandSingleProperties(container);
            }
        }

        private static NodeViewModel GetNode(PropertyViewItem item)
        {
            return item.DataContext as NodeViewModel;
        }

        private static ExpandRule GetRule(NodeViewModel node)
        {
            object value;
            if (node.AssociatedData.TryGetValue(DisplayData.AutoExpandRule, out value) && value is ExpandRule)
            {
                return (ExpandRule)value;
            }
            return ExpandRule.Auto;
        }

        private void NodeValueChanged(object sender, NodeViewModelValueChangedArgs e)
        {
            if (e.Node == null)
                return;

            var rule = GetRule(e.Node);
            if (rule == ExpandRule.Never)
                return;

            var match = AssociatedObject.Properties.Select(x => FindPropertyItemRecursively(x, e.Node)).FirstOrDefault(x => x != null);
            match?.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
        }

        private static PropertyViewItem FindPropertyItemRecursively(PropertyViewItem root, NodeViewModel target)
        {
            return root.DataContext == target ? root : root.Properties.Select(x => FindPropertyItemRecursively(x, target)).FirstOrDefault(x => x != null);
        }
    }
}

// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.Controls
{
    public class PropertyView : ItemsControl
    {
        private readonly ObservableList<PropertyViewItem> properties = new ObservableList<PropertyViewItem>();

        /// <summary>
        /// Identifies the <see cref="HighlightedItem"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey HighlightedItemPropertyKey = DependencyProperty.RegisterReadOnly(nameof(HighlightedItem), typeof(PropertyViewItem), typeof(PropertyView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HoveredItem"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey HoveredItemPropertyKey = DependencyProperty.RegisterReadOnly(nameof(HoveredItem), typeof(PropertyViewItem), typeof(PropertyView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="KeyboardActiveItem"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey KeyboardActiveItemPropertyKey = DependencyProperty.RegisterReadOnly(nameof(KeyboardActiveItem), typeof(PropertyViewItem), typeof(PropertyView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="NameColumnSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NameColumnSizeProperty = DependencyProperty.Register(nameof(NameColumnSize), typeof(GridLength), typeof(PropertyView), new FrameworkPropertyMetadata(new GridLength(150), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Identifies the PreparePropertyItem event.
        /// This attached routed event may be raised by the PropertyGrid itself or by a PropertyItemBase containing sub-items.
        /// </summary>
        public static readonly RoutedEvent PrepareItemEvent = EventManager.RegisterRoutedEvent("PrepareItem", RoutingStrategy.Bubble, typeof(EventHandler<PropertyViewItemEventArgs>), typeof(PropertyView));

        /// <summary>
        /// Identifies the ClearPropertyItem event.
        /// This attached routed event may be raised by the PropertyGrid itself or by a
        /// PropertyItemBase containing sub items.
        /// </summary>
        public static readonly RoutedEvent ClearItemEvent = EventManager.RegisterRoutedEvent("ClearItem", RoutingStrategy.Bubble, typeof(EventHandler<PropertyViewItemEventArgs>), typeof(PropertyView));

        static PropertyView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyView), new FrameworkPropertyMetadata(typeof(PropertyView)));
        }

        public PropertyView()
        {
            IsKeyboardFocusWithinChanged += OnIsKeyboardFocusWithinChanged;
        }

        public IReadOnlyCollection<PropertyViewItem> Properties => properties;

        /// <summary>
        /// Gets the <see cref="PropertyViewItem"/> that is currently highlighted by the mouse cursor.
        /// </summary>
        public PropertyViewItem HighlightedItem { get { return (PropertyViewItem)GetValue(HighlightedItemPropertyKey.DependencyProperty); } private set { SetValue(HighlightedItemPropertyKey, value); } }

        /// <summary>
        /// Gets the <see cref="PropertyViewItem"/> that is currently hovered by the mouse cursor.
        /// </summary>
        public PropertyViewItem HoveredItem { get { return (PropertyViewItem)GetValue(HoveredItemPropertyKey.DependencyProperty); } private set { SetValue(HoveredItemPropertyKey, value); } }

        /// <summary>
        /// Gets the <see cref="PropertyViewItem"/> that currently owns the control who have the keyboard focus.
        /// </summary>
        public PropertyViewItem KeyboardActiveItem { get { return (PropertyViewItem)GetValue(KeyboardActiveItemPropertyKey.DependencyProperty); } private set { SetValue(KeyboardActiveItemPropertyKey, value); } }

        /// <summary>
        /// Gets or sets the shared size of the 'Name' column.
        /// </summary>
        public GridLength NameColumnSize { get { return (GridLength)GetValue(NameColumnSizeProperty); } set { SetValue(NameColumnSizeProperty, value); } }

        /// <summary>
        /// This event is raised when a property item is about to be displayed in the PropertyGrid.
        /// This allow the user to customize the property item just before it is displayed.
        /// </summary>
        public event EventHandler<PropertyViewItemEventArgs> PrepareItem { add { AddHandler(PrepareItemEvent, value); } remove { RemoveHandler(PrepareItemEvent, value); } }

        /// <summary>
        /// This event is raised when an property item is about to be remove from the display in the PropertyGrid
        /// This allow the user to remove any attached handler in the PreparePropertyItem event.
        /// </summary>
        public event EventHandler<PropertyViewItemEventArgs> ClearItem { add { AddHandler(ClearItemEvent, value); } remove { RemoveHandler(ClearItemEvent, value); } }

        internal void ItemMouseMove(object sender, MouseEventArgs e)
        {
            var item = sender as PropertyViewItem;
            if (item != null)
            {
                if (item.Highlightable)
                    HighlightItem(item);

                HoverItem(item);
            }
        }

        internal void OnIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Equals(sender, this) && !(bool)e.NewValue)
            {
                KeyboardActivateItem(null);
                return;
            }

            // We want to find the closest PropertyViewItem to the element who got the keyboard focus.
            var focusedControl = Keyboard.FocusedElement as DependencyObject;
            if (focusedControl != null)
            {
                var propertyItem = focusedControl as PropertyViewItem ?? focusedControl.FindVisualParentOfType<PropertyViewItem>();
                if (propertyItem != null)
                {
                    KeyboardActivateItem(propertyItem);
                }
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            HoverItem(null);
            HighlightItem(null);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new PropertyViewItem(this);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PropertyViewItem;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            var container = (PropertyViewItem)element;
            properties.Add(container);
            RaiseEvent(new PropertyViewItemEventArgs(PrepareItemEvent, this, container, item));
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            var container = (PropertyViewItem)element;
            RaiseEvent(new PropertyViewItemEventArgs(ClearItemEvent, this, (PropertyViewItem)element, item));
            properties.Remove(container);
            base.ClearContainerForItemOverride(element, item);
        }

        private void KeyboardActivateItem(PropertyViewItem item)
        {
            KeyboardActiveItem?.SetValue(PropertyViewItem.IsKeyboardActivePropertyKey, false);
            KeyboardActiveItem = item;
            KeyboardActiveItem?.SetValue(PropertyViewItem.IsKeyboardActivePropertyKey, true);
        }

        private void HoverItem(PropertyViewItem item)
        {
            HoveredItem?.SetValue(PropertyViewItem.IsHoveredPropertyKey, false);
            HoveredItem = item;
            HoveredItem?.SetValue(PropertyViewItem.IsHoveredPropertyKey, true);
        }

        private void HighlightItem(PropertyViewItem item)
        {
            HighlightedItem?.SetValue(PropertyViewItem.IsHighlightedPropertyKey, false);
            HighlightedItem = item;
            HighlightedItem?.SetValue(PropertyViewItem.IsHighlightedPropertyKey, true);
        }


        //protected override AutomationPeer OnCreateAutomationPeer()
        //{
        //    return (AutomationPeer)new TreeViewAutomationPeer(this);
        //}
    }
}

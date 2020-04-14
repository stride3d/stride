// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Controls
{
    /// <summary>
    /// This class represents an item container of the <see cref="PropertyView"/> control.
    /// </summary>
    public class PropertyViewItem : ExpandableItemsControl
    {
        private readonly ObservableList<PropertyViewItem> properties = new ObservableList<PropertyViewItem>();

        /// <summary>
        /// Identifies the <see cref="Highlightable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightableProperty = DependencyProperty.Register("Highlightable", typeof(bool), typeof(PropertyViewItem), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="IsHighlighted"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey IsHighlightedPropertyKey = DependencyProperty.RegisterReadOnly("IsHighlighted", typeof(bool), typeof(PropertyViewItem), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Identifies the <see cref="IsHovered"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey IsHoveredPropertyKey = DependencyProperty.RegisterReadOnly("IsHovered", typeof(bool), typeof(PropertyViewItem), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Identifies the <see cref="IsKeyboardActive"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey IsKeyboardActivePropertyKey = DependencyProperty.RegisterReadOnly("IsKeyboardActive", typeof(bool), typeof(PropertyViewItem), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Identifies the <see cref="Offset"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey OffsetPropertyKey = DependencyProperty.RegisterReadOnly("Offset", typeof(double), typeof(PropertyViewItem), new FrameworkPropertyMetadata(0.0));

        /// <summary>
        /// Identifies the <see cref="Increment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(double), typeof(PropertyViewItem), new FrameworkPropertyMetadata(0.0, OnIncrementChanged));

        static PropertyViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyViewItem), new FrameworkPropertyMetadata(typeof(PropertyViewItem)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyViewItem"/> class.
        /// </summary>
        /// <param name="propertyView">The <see cref="PropertyView"/> instance in which this <see cref="PropertyViewItem"/> is contained.</param>
        public PropertyViewItem([NotNull] PropertyView propertyView)
        {
            if (propertyView == null) throw new ArgumentNullException(nameof(propertyView));
            PropertyView = propertyView;
            PreviewMouseMove += propertyView.ItemMouseMove;
            IsKeyboardFocusWithinChanged += propertyView.OnIsKeyboardFocusWithinChanged;
        }

        /// <summary>
        /// Gets the <see cref="PropertyView"/> control containing this instance of <see cref="PropertyViewItem"/>.
        /// </summary>
        public PropertyView PropertyView { get; }

        /// <summary>
        /// Gets the collection of <see cref="PropertyViewItem"/> instance contained in this control.
        /// </summary>
        public IReadOnlyCollection<PropertyViewItem> Properties => properties;

        /// <summary>
        /// Gets or sets whether this control can be highlighted.
        /// </summary>
        /// <seealso cref="IsHighlighted"/>
        public bool Highlightable { get { return (bool)GetValue(HighlightableProperty); } set { SetValue(HighlightableProperty, value.Box()); } }

        /// <summary>
        /// Gets whether this control is highlighted. The control is highlighted when <see cref="IsHovered"/> and <see cref="Highlightable"/> are both <c>true</c>
        /// </summary>
        /// <seealso cref="Highlightable"/>
        /// <seealso cref="IsHovered"/>
        public bool IsHighlighted => (bool)GetValue(IsHighlightedPropertyKey.DependencyProperty);

        /// <summary>
        /// Gets whether the mouse cursor is currently over this control.
        /// </summary>
        public bool IsHovered => (bool)GetValue(IsHoveredPropertyKey.DependencyProperty);

        /// <summary>
        /// Gets whether this control is the closest control to the control that has the keyboard focus.
        /// </summary>
        public bool IsKeyboardActive => (bool)GetValue(IsKeyboardActivePropertyKey.DependencyProperty);

        /// <summary>
        /// Gets the absolute offset of this <see cref="PropertyViewItem"/>.
        /// </summary>
        public double Offset { get { return (double)GetValue(OffsetPropertyKey.DependencyProperty); } private set { SetValue(OffsetPropertyKey, value); } }

        /// <summary>
        /// Gets or set the increment value used to calculate the <see cref="Offset "/>of the <see cref="PropertyViewItem"/> contained in the <see cref="Properties"/> of this control..
        /// </summary>
        public double Increment { get { return (double)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

        /// <inheritdoc/>
        protected override DependencyObject GetContainerForItemOverride()
        {
            var item = new PropertyViewItem(PropertyView) { Offset = Offset + Increment };
            return item;
        }

        /// <inheritdoc/>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PropertyViewItem;
        }

        /// <inheritdoc/>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            var container = (PropertyViewItem)element;
            properties.Add(container);
            RaiseEvent(new PropertyViewItemEventArgs(PropertyView.PrepareItemEvent, this, container, item));
        }

        /// <inheritdoc/>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            var container = (PropertyViewItem)element;
            RaiseEvent(new PropertyViewItemEventArgs(PropertyView.ClearItemEvent, this, (PropertyViewItem)element, item));
            properties.Remove(container);
            base.ClearContainerForItemOverride(element, item);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // base method can handle this event, but we still want to focus on it in this case.
            var handled = e.Handled;
            base.OnMouseLeftButtonDown(e);
            if (!handled && IsEnabled)
            {
                Focus();
                e.Handled = true;
            }
        }

        // TODO
        //protected override AutomationPeer OnCreateAutomationPeer()
        //{
        //    return (AutomationPeer)new TreeViewItemAutomationPeer(this);
        //}

        private static void OnIncrementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (PropertyViewItem)d;
            var delta = (double)e.NewValue - (double)e.OldValue;
            var subItems = item.FindVisualChildrenOfType<PropertyViewItem>();
            foreach (var subItem in subItems)
            {
                subItem.Offset += delta;
            }
        }
    }
}

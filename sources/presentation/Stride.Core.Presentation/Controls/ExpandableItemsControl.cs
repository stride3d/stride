// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Controls
{
    public class ExpandableItemsControl : HeaderedItemsControl
    {
        /// <summary>
        /// Identifies the <see cref="IsExpanded"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(ExpandableItemsControl), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, OnIsExpandedChanged));

        /// <summary>
        /// Identifies the <see cref="Expanded"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ExpandedEvent = EventManager.RegisterRoutedEvent(nameof(Expanded), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExpandableItemsControl));

        /// <summary>
        /// Identifies the <see cref="Collapsed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent CollapsedEvent = EventManager.RegisterRoutedEvent(nameof(Collapsed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExpandableItemsControl));

        /// <summary>
        /// Gets or sets whether this control is expanded.
        /// </summary>
        public bool IsExpanded { get { return (bool)GetValue(IsExpandedProperty); } set { SetValue(IsExpandedProperty, value.Box()); } }

        protected bool CanExpand => HasItems;

        /// <summary>
        /// Raised when this <see cref="ExpandableItemsControl"/> is expanded.
        /// </summary>
        public event RoutedEventHandler Expanded { add { AddHandler(ExpandedEvent, value); } remove { RemoveHandler(ExpandedEvent, value); } }

        /// <summary>
        /// Raised when this <see cref="ExpandableItemsControl"/> is collapsed.
        /// </summary>
        public event RoutedEventHandler Collapsed { add { AddHandler(CollapsedEvent, value); } remove { RemoveHandler(CollapsedEvent, value); } }

        /// <summary>
        /// Invoked when this <see cref="ExpandableItemsControl"/> is expanded. Raises the <see cref="Expanded"/> event.
        /// </summary>
        /// <param name="e">The routed event arguments.</param>
        protected virtual void OnExpanded([NotNull] RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Invoked when this <see cref="ExpandableItemsControl"/> is collapsed. Raises the <see cref="Collapsed"/> event.
        /// </summary>
        /// <param name="e">The routed event arguments.</param>
        protected virtual void OnCollapsed([NotNull] RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <inheritdoc/>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled && IsEnabled && e.ClickCount % 2 == 0)
            {
                SetCurrentValue(IsExpandedProperty, !IsExpanded);
                e.Handled = true;
            }
            base.OnMouseLeftButtonDown(e);
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (ExpandableItemsControl)d;
            var isExpanded = (bool)e.NewValue;

            if (isExpanded)
                item.OnExpanded(new RoutedEventArgs(ExpandedEvent, item));
            else
                item.OnCollapsed(new RoutedEventArgs(CollapsedEvent, item));
        }
    }
}

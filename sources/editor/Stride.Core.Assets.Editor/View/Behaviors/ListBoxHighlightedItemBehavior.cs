// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class ListBoxHighlightedItemBehavior : Behavior<ListBox>
    {
        /// <summary>
        /// Identifies the <see cref="IsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(ListBoxHighlightedItemBehavior), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="HighlightedItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightedItemProperty = DependencyProperty.Register(nameof(HighlightedItem), typeof(object), typeof(ListBoxHighlightedItemBehavior));

        /// <summary>
        /// Identifies the <see cref="UseSelectedItemIfAvailable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseSelectedItemIfAvailableProperty = DependencyProperty.Register(nameof(UseSelectedItemIfAvailable), typeof(bool), typeof(ListBoxHighlightedItemBehavior));

        /// <summary>
        /// Identifies the <see cref="SelectHighlightedWhenEnteringControl"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectHighlightedWhenEnteringControlProperty = DependencyProperty.Register(nameof(SelectHighlightedWhenEnteringControl), typeof(UIElement), typeof(ListBoxHighlightedItemBehavior), new PropertyMetadata(SelectHighlightedWhenEnteringControlChanged));

        /// <summary>
        /// Identifies the <see cref="DelayToUpdate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DelayToUpdateProperty = DependencyProperty.Register(nameof(DelayToUpdate), typeof(double), typeof(ListBoxHighlightedItemBehavior));

        private ListBoxItem lastHoveredItem;
        private int lastHoveredItemId;

        public bool IsEnabled { get { return (bool)GetValue(IsEnabledProperty); } set { SetValue(IsEnabledProperty, value); } }

        public object HighlightedItem { get { return GetValue(HighlightedItemProperty); } set { SetValue(HighlightedItemProperty, value); } }

        public bool UseSelectedItemIfAvailable { get { return (bool)GetValue(UseSelectedItemIfAvailableProperty); } set { SetValue(UseSelectedItemIfAvailableProperty, value); } }

        public UIElement SelectHighlightedWhenEnteringControl { get { return (UIElement)GetValue(SelectHighlightedWhenEnteringControlProperty); } set { SetValue(SelectHighlightedWhenEnteringControlProperty, value); } }

        public double DelayToUpdate { get { return (double)GetValue(DelayToUpdateProperty); } set { SetValue(DelayToUpdateProperty, value); } }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseMove += TimedUpdateHighlightedItem;
            AssociatedObject.SelectionChanged += UpdateHighlightedItem;
            if (SelectHighlightedWhenEnteringControl != null)
            {
                SelectHighlightedWhenEnteringControl.MouseEnter += MouseEnter;
                SelectHighlightedWhenEnteringControl.MouseLeave += MouseLeave;

            }
        }

        protected override void OnDetaching()
        {
            if (SelectHighlightedWhenEnteringControl != null)
            {
                SelectHighlightedWhenEnteringControl.MouseEnter -= MouseEnter;
                SelectHighlightedWhenEnteringControl.MouseLeave -= MouseLeave;

            }
            AssociatedObject.SelectionChanged -= UpdateHighlightedItem;
            AssociatedObject.PreviewMouseMove -= TimedUpdateHighlightedItem;
            base.OnDetaching();
        }

        private void UpdateHighlightedItem(object sender, RoutedEventArgs e)
        {
            if (!IsEnabled)
                return;

            var item = GetHoveredItem(UseSelectedItemIfAvailable, e.OriginalSource);
            if (item != null)
            {
                UpdateLastHoveredItem(item);
                SetCurrentValue(HighlightedItemProperty, item.DataContext);
            }
        }

        private async void TimedUpdateHighlightedItem(object sender, MouseEventArgs e)
        {
            if (!IsEnabled)
                return;

            ListBoxItem hoveredItem = GetHoveredItem(UseSelectedItemIfAvailable, e.OriginalSource);
            // Keep track of the last hovered item in time, for when this handler goes async
            UpdateLastHoveredItem(hoveredItem);
            int hoveredId = lastHoveredItemId;

            if (hoveredItem == null)
                return;

            // Set the highlighted item immediately if we don't have a delay to wait
            if (DelayToUpdate <= 0)
            {
                SetCurrentValue(HighlightedItemProperty, hoveredItem.DataContext);
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(DelayToUpdate));

            // Check if the same item is still hovered after the delay
            if (hoveredId == lastHoveredItemId)
            {
                SetCurrentValue(HighlightedItemProperty, hoveredItem.DataContext);
            }
        }

        private void UpdateLastHoveredItem(ListBoxItem hoveredItem)
        {
            if (!ReferenceEquals(hoveredItem, lastHoveredItem))
            {
                lastHoveredItem = hoveredItem;
                ++lastHoveredItemId;
            }
        }

        private ListBoxItem GetHoveredItem(bool useSelectedItemIfAvailable, object originalSource)
        {
            ListBoxItem item = null;
            // First get the selected item, if available
            if (useSelectedItemIfAvailable && AssociatedObject.SelectedItem != null)
            {
                item = (ListBoxItem)AssociatedObject.ItemContainerGenerator.ContainerFromItem(AssociatedObject.SelectedItem);
            }
            // If nothing is selected or if we don't want the selected item, get the hovered item
            if (item == null)
            {
                var frameworkElement = originalSource as FrameworkElement;
                var contentElement = originalSource as FrameworkContentElement;
                if (contentElement != null)
                {
                    frameworkElement = contentElement.Parent as FrameworkElement;
                }
                item = frameworkElement as ListBoxItem ?? frameworkElement.FindVisualParentOfType<ListBoxItem>();
            }
            return item;
        }

        private static void SelectHighlightedWhenEnteringControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ListBoxHighlightedItemBehavior)d;
            if (e.OldValue != null)
            {
                ((UIElement)e.OldValue).MouseEnter -= behavior.MouseEnter;
                ((UIElement)e.OldValue).MouseLeave -= behavior.MouseLeave;
            }
            if (e.NewValue != null)
            {
                ((UIElement)e.NewValue).MouseEnter += behavior.MouseEnter;
                ((UIElement)e.NewValue).MouseLeave += behavior.MouseLeave;
            }
        }

        private void MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsEnabled)
                return;

            AssociatedObject.SetCurrentValue(Selector.SelectedItemProperty, HighlightedItem);
            UpdateLastHoveredItem(null);
        }

        private void MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsEnabled)
                return;

            AssociatedObject.SetCurrentValue(Selector.SelectedItemProperty, null);
            UpdateLastHoveredItem(null);
        }
    }
}

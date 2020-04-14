// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Behaviors;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class TilePanelThumbnailPrioritizationBehavior : DeferredBehaviorBase<ItemsControl>
    {
        private VirtualizingTilePanel panel;

        private int previousFirstVisibleItemIndex;
        private int previousLastVisibleItemIndex;

        protected override void OnAttachedAndLoaded()
        {
            panel = GetItemsPanel(AssociatedObject);
            panel.ScrollOwner.ScrollChanged += ScrollChanged;
            base.OnAttachedAndLoaded();
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            int firstVisibleItemIndex, lastVisibleItemIndex;
            panel.GetVisibilityRange(new Size(panel.ScrollOwner.ViewportWidth, panel.ScrollOwner.ViewportHeight), out firstVisibleItemIndex, out lastVisibleItemIndex);

            if (previousFirstVisibleItemIndex == firstVisibleItemIndex && previousLastVisibleItemIndex == lastVisibleItemIndex)
                return;

            previousFirstVisibleItemIndex = firstVisibleItemIndex;
            previousLastVisibleItemIndex = lastVisibleItemIndex;

            var items = AssociatedObject.ItemsSource?.Cast<ISessionObjectViewModel>().ToList();
            if (items == null || firstVisibleItemIndex > lastVisibleItemIndex)
                return;

            var session = items[0].Session;
            var visibleAssets = items.Subset(firstVisibleItemIndex, lastVisibleItemIndex - firstVisibleItemIndex + 1).OfType<AssetViewModel>();
            session.Thumbnails.IncreaseThumbnailPriority(visibleAssets);
        }

        private static VirtualizingTilePanel GetItemsPanel(DependencyObject itemsControl)
        {
            var itemsPresenter = itemsControl.FindVisualChildOfType<ItemsPresenter>();

            if (itemsPresenter == null)
                throw new InvalidOperationException("Unable to reach the ItemsPresenter of the associated ItemsControl.");

            var itemsPanel = (VirtualizingTilePanel)VisualTreeHelper.GetChild(itemsPresenter, 0);

            if (itemsPanel == null)
                throw new InvalidOperationException("Unable to reach the VirtualizingTilePanel of the associated ItemsControl.");
            return itemsPanel;
        }
    }
}

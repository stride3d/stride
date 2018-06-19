// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Windows.Controls;

using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Behaviors;
using Xenko.Core.Presentation.Extensions;

using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class XceedDataGridThumbnailPrioritizationBehavior : DeferredBehaviorBase<DataGridControl>
    {
        private TableViewScrollViewer scrollViewer;

        protected override void OnAttachedAndLoaded()
        {
            scrollViewer = AssociatedObject.FindVisualChildOfType<TableViewScrollViewer>();
            scrollViewer.ScrollChanged += ScrollChanged;
            base.OnAttachedAndLoaded();
        }

        protected override void OnDetachingAndUnloaded()
        {
            scrollViewer.ScrollChanged -= ScrollChanged;
            scrollViewer = null;
            base.OnDetachingAndUnloaded();
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var session = AssociatedObject.ItemsSource?.Cast<ISessionObjectViewModel>().FirstOrDefault()?.Session;
            if (session == null)
                return;

            var visibleAssets = AssociatedObject.FindVisualChildrenOfType<DataRow>().NotNull().Select(x => x.DataContext).OfType<AssetViewModel>().Where(x => x != null);
            session.Thumbnails.IncreaseThumbnailPriority(visibleAssets);
        }
    }
}

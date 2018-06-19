// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Behaviors;

using Xceed.Wpf.DataGrid;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class XceedDataGridBindableSelectedItemsBehavior : BindableSelectedItemsBehavior<DataGridControl>
    {
        protected override void OnAttached()
        {
            SelectedItemsInAssociatedObject = AssociatedObject.SelectedItems;
            AssociatedObject.SelectionChanged += XceedDataGridSelectionChanged;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= XceedDataGridSelectionChanged;
            SelectedItemsInAssociatedObject = AssociatedObject.SelectedItems;
        }

        protected override void ScrollIntoView(object dataItem)
        {
            AssociatedObject.BringItemIntoView(dataItem);
        }

        private void XceedDataGridSelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
        {
            foreach (var selectionInfo in e.SelectionInfos)
            {
                ControlSelectionChanged(selectionInfo.AddedItems, selectionInfo.RemovedItems);
            }
        }
    }
}

// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Extensions;
using Xceed.Wpf.DataGrid;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class XceedDataGridDragDropBehavior : DragDropBehavior<DataGridControl, DataRow>
    {
        protected override IEnumerable<object> GetItemsToDrag(DataRow container)
        {
            if (container != null)
            {
                var sourceItem = container.DataContext;
                return AssociatedObject.SelectedItems.Contains(sourceItem) ? AssociatedObject.SelectedItems.Cast<object>() : sourceItem.ToEnumerable<object>();
            }
            return Enumerable.Empty<object>();
        }
    }
}

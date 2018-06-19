// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Behaviors;
using Xenko.Core.Presentation.Extensions;

using Xceed.Wpf.DataGrid;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// This behavior allows to have resizable columns in a <see cref="DataGridControl"/> while being able to define default widths using star notation.
    /// It also prevent the columns do be resized below the viewport width
    /// </summary>
    public class XceedDataGridResizableColumnsBehavior : DeferredBehaviorBase<DataGridControl>
    {
        private ScrollContentPresenter scrollContentPresenter;
        private bool updatingColumns;
        private bool itemsSourceChanged;
        private bool initialized;

        protected override void OnAttachedAndLoaded()
        {
            base.OnAttachedAndLoaded();

            AssociatedObject.Columns.ForEach(x => x.Width = x.ActualWidth);

            scrollContentPresenter = AssociatedObject.FindVisualChildOfType<ScrollContentPresenter>();

            // This occurs when closing a session - it is harmless to ignore it in this case
            if (scrollContentPresenter == null)
                return;

            AssociatedObject.ItemsSourceChangeCompleted += ItemsSourceChanged;
            AssociatedObject.LayoutUpdated += LayoutUpdated;
            AssociatedObject.SizeChanged += SizeChanged;
            itemsSourceChanged = true;
            // Prevent the selection of the first item
            AssociatedObject.SelectedItems.Clear();
            initialized = true;
        }

        protected override void OnDetachingAndUnloaded()
        {
            if (initialized)
            {
                var columnManagerRow = AssociatedObject.FindVisualChildOfType<ColumnManagerRow>();
                if (columnManagerRow != null)
                {
                    var cells = columnManagerRow.FindVisualChildrenOfType<ColumnManagerCell>();
                    cells.ForEach(x => x.SizeChanged -= ColumnSizeChanged);
                    AssociatedObject.SizeChanged -= SizeChanged;
                }
            }
            base.OnDetachingAndUnloaded();
        }

        private void ItemsSourceChanged(object sender, EventArgs e)
        {
            // cells update is defered to after the next layout update
            itemsSourceChanged = true;
            // Prevent the selection of the first item
            AssociatedObject.SelectedItems.Clear();
        }

        private void LayoutUpdated(object sender, EventArgs e)
        {
            if (itemsSourceChanged)
            {
                var columnManagerRow = AssociatedObject.FindVisualChildOfType<ColumnManagerRow>();
                if (columnManagerRow != null)
                {
                    var cells = columnManagerRow.FindVisualChildrenOfType<ColumnManagerCell>();
                    cells.ForEach(x => x.SizeChanged += ColumnSizeChanged);
                }
                itemsSourceChanged = false;
            }
        }

        private void ColumnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (updatingColumns)
                return;

            updatingColumns = true;

            double total = AssociatedObject.Columns.Sum(x => x.ActualWidth);
            if (total < scrollContentPresenter.ActualWidth)
            {
                foreach (var column in AssociatedObject.Columns)
                {
                    column.Width = column.ActualWidth * scrollContentPresenter.ActualWidth / total;
                }
            }

            updatingColumns = false;
        }

        private void SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double total = AssociatedObject.Columns.Sum(x => x.ActualWidth);
            var offset = e.NewSize.Width - e.PreviousSize.Width;
            foreach (var column in AssociatedObject.Columns)
            {
                column.Width = column.ActualWidth * (total + offset) / total;
            }
        }
    }
}

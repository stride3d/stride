// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Specialized;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Behaviors;
using TreeView = Xenko.Core.Presentation.Controls.TreeView;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class TreeViewBindableSelectedItemsBehavior : BindableSelectedItemsBehavior<TreeView>
    {
        protected override void OnAttached()
        {
            SelectedItemsInAssociatedObject = AssociatedObject.SelectedItems;
            ((INotifyCollectionChanged)AssociatedObject.SelectedItems).CollectionChanged += TreeViewSelectionChanged;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            ((INotifyCollectionChanged)AssociatedObject.SelectedItems).CollectionChanged -= TreeViewSelectionChanged;
            SelectedItemsInAssociatedObject = AssociatedObject.SelectedItems;
        }

        protected override void ScrollIntoView(object dataItem)
        {
            AssociatedObject.BringItemToView(dataItem, x => { var child = x as IChildViewModel; return child?.GetParent(); });
        }

        private void TreeViewSelectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ControlSelectionCleared();
                ControlSelectionChanged(SelectedItemsInAssociatedObject, null);
            }
            else
            {
                ControlSelectionChanged(e.NewItems, e.OldItems);
            }
        }
    }
}

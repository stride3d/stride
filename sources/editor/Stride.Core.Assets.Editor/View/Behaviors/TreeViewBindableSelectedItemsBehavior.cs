// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Specialized;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Behaviors;
using TreeView = Stride.Core.Presentation.Controls.TreeView;

namespace Stride.Core.Assets.Editor.View.Behaviors
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

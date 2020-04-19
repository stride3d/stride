// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// An implementation of the <see cref="BindableSelectedItemsBehavior{T}"/> for <see cref="ListBox"/>.
    /// </summary>
    public class ListBoxBindableSelectedItemsBehavior : BindableSelectedItemsBehavior<ListBox>
    {
        // We need this because when a listbox is being removed from the visual tree, it might have this as data context
        // while its selection is cleared, and in this case we don't want to modify the bound collection (because it's cleanup, not user action)
        private static readonly object InternalDisconnectedObject;

        static ListBoxBindableSelectedItemsBehavior()
        {
            var field = typeof(BindingExpressionBase).GetField("DisconnectedItem", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null) throw new InvalidOperationException("An incompatible version of Windows Presentation Framework has been used to build this application");
            InternalDisconnectedObject = field.GetValue(null);
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            SelectedItemsInAssociatedObject = AssociatedObject.SelectedItems;
            AssociatedObject.SelectionChanged += ListBoxSelectionChanged;
            base.OnAttached();
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= ListBoxSelectionChanged;
            SelectedItemsInAssociatedObject = null;
        }

        /// <summary>
        /// Handles the <see cref="ListBox.SelectionChanged"/> event and invoke the <see cref="BindableSelectedItemsBehavior{T}.ControlSelectionChanged"/> method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxSelectionChanged([NotNull] object sender, SelectionChangedEventArgs e)
        {
            // Don't process events happening inside the EditableContentListBoxItem (happening when we have a ComboBox inside)
            if (!Equals(e.OriginalSource, AssociatedObject))
                return;

            var listBox = (ListBox)sender;
            if (listBox.DataContext != null && listBox.DataContext != InternalDisconnectedObject)
                ControlSelectionChanged(e.AddedItems, e.RemovedItems);
        }

        /// <summary>
        /// Scrolls the list box to the given item.
        /// </summary>
        /// <param name="dataItem">The item to scroll to.</param>
        protected override void ScrollIntoView(object dataItem)
        {
            AssociatedObject.ScrollIntoView(dataItem);
        }
    }
}

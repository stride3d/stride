// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Assets.Editor.View.Controls;
using Xenko.Core.Presentation.Controls;
using Xenko.Core.Presentation.Extensions;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class BringSelectionToViewBehavior : Behavior<EditableContentListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += SelectionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= SelectionChanged;
            base.OnDetaching();
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssociatedObject.SelectedIndex >= 0)
            {
                var panel = AssociatedObject.FindVisualChildOfType<VirtualizingTilePanel>();
                if (panel != null)
                {
                    panel.ScrollToIndexedItem(AssociatedObject.SelectedIndex);
                }
            }
        }
    }
}

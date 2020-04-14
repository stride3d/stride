// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Presentation.Behaviors;
using TreeViewItem = Xenko.Core.Presentation.Controls.TreeViewItem;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class TreeViewStopEditOnLostFocusBehavior : OnEventBehavior
    {
        public TreeViewStopEditOnLostFocusBehavior()
        {
            EventName = "LostFocus";
        }

        protected override void OnAttached()
        {
            if (!(AssociatedObject is TreeViewItem))
                throw new InvalidOperationException("This behavior must be attached to an instance of TreeViewItem.");
            base.OnAttached();
        }

        protected override void OnEvent()
        {
            var treeViewItem = (TreeViewItem)AssociatedObject;
            treeViewItem.SetCurrentValue(TreeViewItem.IsEditingProperty, false);
        }
    }
}

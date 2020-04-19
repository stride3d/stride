// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Controls;

using Stride.Core.Presentation.Behaviors;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class OnComboBoxClosedWithSelectionBehavior : OnEventCommandBehavior
    {
        public OnComboBoxClosedWithSelectionBehavior()
        {
            EventName = "DropDownClosed";
        }

        protected override void OnAttached()
        {
            if (!(AssociatedObject is ComboBox))
                throw new InvalidOperationException("The OnComboBoxClosedWithSelectionBehavior must be attached to a ComboBox.");

            base.OnAttached();
        }

        protected override void OnEvent()
        {
            var comboBox = (ComboBox)AssociatedObject;
            if (comboBox.SelectedItem != null)
                base.OnEvent();
        }
    }
}

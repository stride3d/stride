// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// This behavior will ensure that the associated toggle button can be toggled only when both mouse down and mouse up
    /// events are received, preventing to toggle it when a popup window is open. This behavior is useful when binding the
    /// <see cref="Popup.IsOpen"/> property of a popup to the <see cref="ToggleButton.IsChecked"/> of a toggle button.
    /// </summary>
    public class ToggleButtonPopupBehavior : Behavior<ToggleButton>
    {
        private bool mouseDownOccurred;

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += MouseDown;
            AssociatedObject.PreviewMouseLeftButtonUp += MouseUp;
        }

        private void MouseUp(object sender, [NotNull] MouseButtonEventArgs e)
        {
            if (!mouseDownOccurred)
            {
                // Stop capturing mouse so that a click somewhere else doesn't reopen the popup
                AssociatedObject.ReleaseMouseCapture();
            }
            mouseDownOccurred = false;
        }

        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDownOccurred = true;
        }
    }
}

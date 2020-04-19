// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Documents;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// A behavior that can be attached to a <see cref="Hyperlink"/> and will close the window it is contained in when clicked. Note that if a command is attached to the button, it will be executed after the window is closed.
    /// If you need to execute a command before closing the window, you can use the <see cref="CloseWindowBehavior{T}.Command"/> and <see cref="CloseWindowBehavior{T}.CommandParameter"/> property of this behavior.
    /// </summary>
    public class HyperlinkCloseWindowBehavior : CloseWindowBehavior<Hyperlink>
    {
        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += HyperlinkClicked;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            AssociatedObject.Click -= HyperlinkClicked;
            base.OnDetaching();
        }

        /// <summary>
        /// Raised when the associated button is clicked. Close the containing window
        /// </summary>
        private void HyperlinkClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

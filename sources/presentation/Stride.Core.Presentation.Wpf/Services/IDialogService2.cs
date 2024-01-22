// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Services
{
    /// <summary>
    /// An interface to invoke dialogs from commands implemented in view models
    /// </summary>
    public interface IDialogService2 : IDialogService
    {
        /// <summary>
        /// Displays a modal message box and blocks until the user closed the message box.
        /// </summary>
        /// <param name="message">The text to display as message in the message box.</param>
        /// <param name="buttons">The buttons to display in the message box.</param>
        /// <param name="image">The image to display in the message box.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
        MessageBoxResult BlockingMessageBox(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None);

        /// <summary>
        /// Displays a modal message box and blocks until the user closed the message box.
        /// </summary>
        /// <param name="message">The text to display as message in the message box.</param>
        /// <param name="buttons">The buttons to display in the message box.</param>
        /// <param name="image">The image to display in the message box.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
        int BlockingMessageBox(string message, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None);

        /// <summary>
        /// Displays a modal message box with an additional checkbox between the message and the buttons,
        /// and blocks until the user closed the message box.
        /// The message displayed in the checkbox is the localized string <see cref="Resources.Strings.DontAskMeAgain"/>.
        /// </summary>
        /// <param name="message">The text to display as message in the message box.</param>
        /// <param name="isChecked">The initial status of the check box.</param>
        /// <param name="buttons">The buttons to display in the message box.</param>
        /// <param name="image">The image to display in the message box.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
        CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None);

        /// <summary>
        /// Displays a modal message box with an additional checkbox between the message and the buttons,
        /// and blocks until the user closed the message box.
        /// </summary>
        /// <param name="message">The text to display as message in the message box.</param>
        /// <param name="isChecked">The initial status of the check box.</param>
        /// <param name="checkboxMessage">The message to display in the check box.</param>
        /// <param name="buttons">The buttons to display in the message box.</param>
        /// <param name="image">The image to display in the message box.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
        CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None);

        /// <summary>
        /// Displays a modal message box with an additional checkbox between the message and the buttons,
        /// and blocks until the user closed the message box.
        /// </summary>
        /// <param name="message">The text to display as message in the message box.</param>
        /// <param name="isChecked">The initial status of the check box.</param>
        /// <param name="checkboxMessage">The message to display in the check box.</param>
        /// <param name="buttons">The buttons to display in the message box.</param>
        /// <param name="image">The image to display in the message box.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
        CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, string checkboxMessage, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None);

        /// <summary>
        /// Attempts to close the main window of the application.
        /// </summary>
        /// <param name="onClosed">An action to execute if the main window is successfully closed.</param>
        [NotNull]
        Task CloseMainWindow(Action onClosed);
    }
}

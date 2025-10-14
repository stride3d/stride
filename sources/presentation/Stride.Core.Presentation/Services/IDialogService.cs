// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Core.Presentation.Services;

/// <summary>
/// An interface to invoke dialogs from commands implemented in view models
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Indicates whether the current application has a main window.
    /// </summary>
    /// <remarks>>
    /// Modal windows usually require to be owned by the main window.
    /// </remarks>
    /// <returns><c>true if a main window exists (which can be hidden); otherwise, <c>false</c>.</returns>
    bool HasMainWindow { get; }

    /// <summary>
    /// Gracefully exits the current application.
    /// </summary>
    /// <remarks>
    /// This usualy terminates the process.
    /// </remarks>
    void Exit(int exitCode = 0);

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'Cancel' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.IsCancel"/> is set to <see langword="true"/>.
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.Cancel"/>.</remarks>
    static DialogButtonInfo ButtonCancel => new()
    {
        IsCancel = true,
        Result = (int)MessageBoxResult.Cancel,
        Content = "Cancel",
    };

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'OK' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.IsDefault"/> is set to <see langword="true"/>.
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.OK"/>.</remarks>
    static DialogButtonInfo ButtonOK => new()
    {
        IsDefault = true,
        Result = (int)MessageBoxResult.OK,
        Content = "OK",
    };

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'No' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.No"/>.</remarks>
    static DialogButtonInfo ButtonNo => new()
    {
        Result = (int)MessageBoxResult.No,
        Content = "No",
        Key = Tr._p("KeyGesture", "N"),
    };

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'Yes' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.IsDefault"/> is set to <see langword="true"/>.
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.Yes"/>.</remarks>
    static DialogButtonInfo ButtonYes => new()
    {
        IsDefault = true,
        Result = (int)MessageBoxResult.Yes,
        Content = "Yes",
        Key = Tr._p("KeyGesture", "Y"),
    };

    /// <summary>
    /// Generates a collection of <see cref="DialogButtonInfo"/> based on the specified <paramref name="buttons"/>.
    /// </summary>
    /// <param name="buttons">The buttons.</param>
    /// <returns>A new collection of <see cref="DialogButtonInfo"/>.</returns>
    static IReadOnlyCollection<DialogButtonInfo> GetButtons(MessageBoxButton buttons)
    {
        switch (buttons)
        {
            case MessageBoxButton.OK:
                var buttonOk = ButtonOK;
                buttonOk.IsCancel = true;
                return [buttonOk];

            case MessageBoxButton.OKCancel:
                return [ButtonOK, ButtonCancel];

            case MessageBoxButton.YesNoCancel:
                return [ButtonYes, ButtonNo, ButtonCancel];

            case MessageBoxButton.YesNo:
                var buttonNo = ButtonNo;
                buttonNo.IsCancel = true;
                return [ButtonYes, buttonNo];

            default:
                throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
        }
    }

    /// <summary>
    /// Displays a modal message box with an additional checkbox between the message and the buttons,
    /// and returns a task that completes when the message box is closed.
    /// </summary>
    /// <param name="message">The text to display as message in the message box.</param>
    /// <param name="isChecked">The initial status of the check box.</param>
    /// <param name="checkboxMessage">The message to display in the check box.</param>
    /// <param name="buttons">The buttons to display in the message box.</param>
    /// <param name="image">The image to display in the message box.</param>
    /// <returns>A <see cref="CheckedMessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
    Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None);

    /// <summary>
    /// Displays a modal message box with an additional checkbox between the message and the buttons,
    /// and returns a task that completes when the message box is closed.
    /// </summary>
    /// <param name="message">The text to display as message in the message box.</param>
    /// <param name="isChecked">The initial status of the check box.</param>
    /// <param name="checkboxMessage">The message to display in the check box.</param>
    /// <param name="buttons">The buttons to display in the message box.</param>
    /// <param name="image">The image to display in the message box.</param>
    /// <returns>A <see cref="CheckedMessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
    Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None);

    /// <summary>
    /// Displays a modal message box and returns a task that completes when the message box is closed.
    /// </summary>
    /// <param name="message">The text to display as message in the message box.</param>
    /// <param name="buttons">The buttons to display in the message box.</param>
    /// <param name="image">The image to display in the message box.</param>
    /// <returns>A <see cref="MessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>
    Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None);

    /// <summary>
    /// Displays a modal message box and returns a task that completes when the message box is closed.
    /// </summary>
    /// <param name="message">The text to display as message in the message box.</param>
    /// <param name="buttons">The buttons to display in the message box.</param>
    /// <param name="image">The image to display in the message box.</param>
    /// <returns>A <see cref="int"/> value indicating which button the user pressed to close the window.</returns>
    Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None);

    /// <summary>
    /// Creates a modal file picker dialog.
    /// </summary>
    /// <returns>
    /// A file; or <c>null</c> if user canceled the dialog.
    /// </returns>
    Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null);

    /// <summary>
    /// Creates a modal files picker dialog.
    /// </summary>
    /// <returns>
    /// A list of files; or an empty collection if user canceled the dialog.
    /// </returns>
    Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null);

    /// <summary>
    /// Creates a modal folder picker dialog.
    /// </summary>
    /// <returns>
    /// A folder; or <c>null</c> if user canceled the dialog.
    /// </returns>
    Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null);

    /// <summary>
    /// Creates a modal file picker dialog for saving.
    /// </summary>
    /// <returns>
    /// A file; or <c>null</c> if user canceled the dialog.
    /// </returns>
    Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null);
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Presentation.Services;

/// <summary>
/// An interface to invoke dialogs from commands implemented in view models
/// </summary>
public interface IDialogService
{
    bool HasMainWindow { get; }

    void Exit(int exitCode = 0);

    /// <summary>
    /// Displays a modal message box and returns a task that completes when the message box is closed.
    /// </summary>
    /// <param name="message">The text to display as message in the message box.</param>
    /// <param name="buttons">The buttons to display in the message box.</param>
    /// <param name="image">The image to display in the message box.</param>
    /// <returns>A <see cref="MessageBoxResult"/> value indicating which button the user pressed to close the window.</returns>        
    Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None);

    /// <summary>
    /// Creates a modal file picker dialog.
    /// </summary>
    Task<UFile?> OpenFilePickerAsync(UPath? initialPath = null);

    /// <summary>
    /// Create a modal folder picker dialog.
    /// </summary>
    Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null);
}

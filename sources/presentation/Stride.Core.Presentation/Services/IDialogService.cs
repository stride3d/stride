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
    /// Creates a modal file picker dialog.
    /// </summary>
    Task<UFile?> OpenFilePickerAsync(UPath? initialPath = null);

    /// <summary>
    /// Create a modal folder picker dialog.
    /// </summary>
    Task<UDirectory?> OpenFolderPickerAsync(UPath? initialPath = null);
}

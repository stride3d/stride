// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Shows files and folders in the file manager of the system, and opens files with other programs: the Explore items of the
/// asset view and the solution explorer, and the "show in explorer" items of the editors.
/// </summary>
/// <remarks>
/// The methods throw when the program can't be started; the callers tell the user.
/// </remarks>
public interface IFileExplorerService
{
    /// <summary>
    /// Shows a file in the file manager: its folder opens with the file selected where the file manager can select it.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    Task ShowFileAsync(string filePath);

    /// <summary>
    /// Opens a folder in the file manager.
    /// </summary>
    /// <param name="folderPath">The full path of the folder.</param>
    Task OpenFolderAsync(string folderPath);

    /// <summary>
    /// Opens a file with the program the system associates with it.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <param name="tryEdit">Uses the program that edits the file rather than the one that opens it, where the system tells them apart.</param>
    Task OpenFileAsync(string filePath, bool tryEdit);

    /// <summary>
    /// Opens a file with the given program.
    /// </summary>
    /// <param name="programPath">The full path of the program.</param>
    /// <param name="filePath">The full path of the file, passed as the only argument of the program.</param>
    Task OpenFileWithAsync(string programPath, string filePath);
}

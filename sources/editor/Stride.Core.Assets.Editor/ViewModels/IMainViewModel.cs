// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The main view model of an editor, as the session sees it: each editor (WPF, cross-platform) implements it.
/// </summary>
public interface IMainViewModel
{
    /// <summary>
    /// Gets the status bar of the editor.
    /// </summary>
    StatusViewModel Status { get; }

    /// <summary>
    /// Gets the UI toolkit of the editor (see <see cref="EditorToolkits"/>): the session loads only the Editor companion packages for it.
    /// </summary>
    string EditorToolkitName { get; }

    /// <summary>
    /// Gets the names of the asset types that the session opens with the external shader editor instead of an asset editor.
    /// </summary>
    IEnumerable<string> TextAssetTypes { get; }

    /// <summary>
    /// Puts the given session file at the top of the recent files; the session calls it after each successful save.
    /// </summary>
    /// <param name="filePath">The solution or package file of the session.</param>
    void AddRecentFile(UFile filePath);
}

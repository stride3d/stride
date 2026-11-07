// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.EditorGame;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// An asset editor that shows a game (the scene, prefab and UI editors), for the code that handles their games alike:
/// hiding the game of a hidden editor, reloading assemblies, the recovery service and the UI tests.
/// </summary>
public interface IGameEditorViewModel : IAssetEditorViewModel
{
    /// <summary>
    /// Gets the game of the editor.
    /// </summary>
    EditorGameHost GameHost { get; }
}

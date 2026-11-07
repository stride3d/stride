// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.EditorGame;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// Creates the view options of the scene and prefab editors. Each user interface registers its own in the services of the
/// session, from its default assets plugin.
/// </summary>
public interface IEditorViewOptionsFactory
{
    /// <summary>
    /// Creates the view options of the given editor, whose game has created its scene (UI thread).
    /// </summary>
    /// <param name="editor">The scene or prefab editor.</param>
    /// <param name="controller">The controller of its game.</param>
    /// <returns>The view options, or <c>null</c> when this user interface shows none for this game.</returns>
    IEditorViewOptions? CreateViewOptions(EntityHierarchyEditorViewModel editor, EntityHierarchyEditorController controller);
}

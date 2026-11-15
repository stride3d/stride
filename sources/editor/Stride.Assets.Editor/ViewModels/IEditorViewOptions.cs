// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System.Threading.Tasks;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The view options of the game of a scene or prefab editor (camera, grid, gizmos, render mode, overlays...), which each
/// user interface shows its own way. The editor creates them through the <see cref="IEditorViewOptionsFactory"/> of the
/// session once its game has created its scene, saves them in the user settings of the package and destroys them.
/// </summary>
public interface IEditorViewOptions : IDestroyable
{
    /// <summary>
    /// Completes the options once the game services exist (UI thread).
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Shows the given saved options, and moves the camera where it was.
    /// </summary>
    void LoadSettings(SceneSettingsData settings);

    /// <summary>
    /// Writes the shown options, and where the camera is, into the given settings.
    /// </summary>
    void SaveSettings(SceneSettingsData settings);

    /// <summary>
    /// Snaps the position of an entity the user creates in the view (the entity menu, assets dropped on the view) to the
    /// translation snap of the view, when it is on.
    /// </summary>
    Vector3 SnapPosition(Vector3 position);

    /// <summary>
    /// Called once an entity is in the game and its game-side clone is initialized.
    /// </summary>
    void OnEntityLinked(EntityViewModel entity);

    /// <summary>
    /// Called when an entity leaves the game.
    /// </summary>
    void OnEntityUnlinked(EntityViewModel entity);
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The camera options a user interface shows for the game of a scene or prefab editor. When the view options set them on
/// the camera service, the keys of the game that change the speed or the projection go through them, so that the options
/// show the change and the scene settings save it.
/// </summary>
public interface ICameraViewOptions
{
    /// <summary>
    /// Steps the movement speed of the camera up or down (any thread).
    /// </summary>
    /// <param name="step">+1 for a faster speed, -1 for a slower one.</param>
    void ChangeMovementSpeed(int step);

    /// <summary>
    /// Switches the camera between the perspective and the orthographic projections (any thread).
    /// </summary>
    void SwitchProjection();
}

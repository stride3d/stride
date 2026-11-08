// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// The camera of the game of an editor, as the view models of the editor see it (UI thread): the setters post their
    /// change to the game thread.
    /// </summary>
    public interface IEditorGameCameraViewModelService : IEditorGameViewModelService
    {
        void SetOrthographicProjection(bool value);

        void SetOrthographicSize(float value);

        void SetNearPlane(float value);

        void SetFarPlane(float value);

        void SetFieldOfView(float value);

        float SceneUnit { get; set; }

        float MoveSpeed { get; set; }

        void ResetCamera();

        void ResetCameraOrientation(CameraOrientation orientation);

        /// <summary>
        /// Moves the camera to the position and the orientation saved with the scene (the <c>CamPosition</c> and
        /// <c>CamPitchYaw</c> members of its settings).
        /// </summary>
        /// <param name="position">The position of the camera.</param>
        /// <param name="pitchYaw">The pitch (<see cref="Vector2.X"/>) and the yaw (<see cref="Vector2.Y"/>) of the camera, in radians.</param>
        void LoadSettings(Vector3 position, Vector2 pitchYaw);

        /// <summary>
        /// Gets the position and the orientation of the camera, to save them with the scene.
        /// </summary>
        /// <returns>The position, and the pitch (<see cref="Vector2.X"/>) and yaw (<see cref="Vector2.Y"/>) in radians.</returns>
        (Vector3 Position, Vector2 PitchYaw) SaveSettings();
    }
}

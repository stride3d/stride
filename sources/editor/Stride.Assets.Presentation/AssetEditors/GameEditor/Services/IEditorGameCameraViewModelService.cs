// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// Interface allowing a <see cref="ViewModels.GameEditorViewModel"/> to safely access the camera of the game instance in which the editor is running.
    /// </summary>
    public interface IEditorGameCameraViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Sets whether the camera is currently using an orthographic projection.
        /// </summary>
        /// <param name="value">The value to set.</param>
        void SetOrthographicProjection(bool value);

        /// <summary>
        /// Sets the height of the orthographic projection
        /// </summary>
        /// <param name="value">The value to set.</param>
        void SetOrthographicSize(float value);

        /// <summary>
        /// Sets the distance to the near plane.
        /// </summary>
        /// <param name="value">The value to set.</param>
        void SetNearPlane(float value);

        /// <summary>
        /// Sets the distance to the far plane.
        /// </summary>
        /// <param name="value">The value to set.</param>
        void SetFarPlane(float value);

        /// <summary>
        /// Sets the vertical field of view in degrees.
        /// </summary>
        /// <param name="value">The value to set.</param>
        void SetFieldOfView(float value);

        /// <summary>
        /// The scale used for grid spacing and camera speed.
        /// </summary>
        float SceneUnit { get; set; }

        /// <summary>
        /// Gets or sets the moving speed of the camera (in units/second).
        /// </summary>
        float MoveSpeed { get; set; }

        /// <summary>
        /// Resets the camera to its default.
        /// </summary>
        void ResetCamera();

        /// <summary>
        /// Resets the camera orientation.
        /// </summary>
        /// <param name="orientation">The new direction facing the camera.</param>
        void ResetCameraOrientation(CameraOrientation orientation);

        /// <summary>
        /// Loads settings from the given object into the camera.
        /// </summary>
        /// <param name="sceneSettings">The object from which to read settings.</param>
        void LoadSettings(SceneSettingsData sceneSettings);

        /// <summary>
        /// Saves settings from the camera into the given object.
        /// </summary>
        /// <param name="sceneSettings">The object into which to write settings.</param>
        void SaveSettings(SceneSettingsData sceneSettings);
    }
}

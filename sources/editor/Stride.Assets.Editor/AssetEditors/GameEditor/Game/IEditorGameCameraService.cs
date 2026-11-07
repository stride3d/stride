// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Game
{
    public interface IEditorGameCameraService : IEditorGameService
    {
        CameraComponent Component { get; }

        Matrix ViewMatrix { get; }

        Matrix ProjectionMatrix { get; }

        Vector3 Position { get; }

        bool IsMoving { get; }

        bool IsOrthographic { get; set; }

        float AspectRatio { get; }

        float VerticalFieldOfView { get; }

        float NearPlane { get; }

        float FarPlane { get; }

        float SceneUnit { get; }

        float MoveSpeed { get; }

        void ResetCamera(Vector3 viewDirection);

        void ResetCamera(CameraOrientation orientation);

        /// <summary>
        /// Switches between the perspective and the orthographic projections (game thread).
        /// </summary>
        /// <remarks>
        /// An editor whose view models show the projection switches it through them, so that they show and save the new one.
        /// </remarks>
        void SwitchProjection();
    }
}

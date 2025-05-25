// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.
using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Threading.Tasks;
using MySurvivalGame.Game; // Ensures PlayerInput is resolved from this namespace
using Stride.Engine.Events;

namespace MySurvivalGame.Game
{
    /// <summary>
    /// Script to manage FPS camera behavior.
    /// </summary>
    public class MyFpsCamera : AsyncScript
    {
        /// <summary>
        /// Gets the camera component used to visualized the scene.
        /// </summary>
        private CameraComponent? Component => Entity?.Get<CameraComponent>();

        /// <summary>
        /// Gets or sets the rotation speed of the camera (in radian/screen units)
        /// </summary>
        public float RotationSpeed { get; set; } = 2.355f;

        /// <summary>
        /// Gets or sets the Yaw rotation of the camera.
        /// </summary>
        private float Yaw { get; set; }

        /// <summary>
        /// Gets or sets the Pitch rotation of the camera.
        /// </summary>
        private float Pitch { get; set; }

        /// <summary>
        /// Check to invert the horizontal camera movement
        /// </summary>
        public bool InvertX { get; set; } = false;

        /// <summary>
        /// Check to invert the vertical camera movement
        /// </summary>
        public bool InvertY { get; set; } = false;

        private readonly EventReceiver<Vector2> cameraDirectionEvent = new EventReceiver<Vector2>(PlayerInput.CameraDirectionEventKey);

        public override async Task Execute()
        {
            ResetRotation(); // Renamed to avoid conflict if a base class has Reset

            while (Game.IsRunning) // Loop while the game is running
            {
                UpdateCamera();
                await Script.NextFrame();
            }
        }

        public void ResetRotation()
        {
            // Initialize Yaw and Pitch from the entity's current rotation
            var initialRotation = Entity.Transform.Rotation;
            initialRotation.ToYawPitchRoll(out Yaw, out Pitch, out _); // Extract Yaw and Pitch
        }

        /// <summary>
        /// Update the camera parameters.
        /// </summary>
        protected virtual void UpdateCamera()
        {
            // Camera movement from player input
            if (cameraDirectionEvent.TryReceive(out Vector2 cameraMovement))
            {
                if (InvertY) cameraMovement.Y *= -1;
                if (InvertX) cameraMovement.X *= -1;
            
                Yaw -= cameraMovement.X * RotationSpeed;
                Pitch = MathUtil.Clamp(Pitch + cameraMovement.Y * RotationSpeed, -MathUtil.PiOverTwo + 0.01f, MathUtil.PiOverTwo - 0.01f); // Added small epsilon to prevent gimbal lock issues at poles
            }

            // Update the camera view matrix
            UpdateViewMatrix();
        }

        private void UpdateViewMatrix()
        {
            var camera = Component;
            if (camera == null) return;
            
            // Apply rotation to the entity's transform
            Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0);
        }
    }
}

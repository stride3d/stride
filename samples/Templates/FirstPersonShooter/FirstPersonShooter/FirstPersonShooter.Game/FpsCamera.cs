// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Threading.Tasks;
using FirstPersonShooter.Player;
using Stride.Engine.Events;

namespace FirstPersonShooter
{
    /// <summary>
    /// The default script for the scene editor camera.
    /// </summary>
    public class FpsCamera : AsyncScript
    {
        /// <summary>
        /// Gets the camera component used to visualized the scene.
        /// </summary>
        private CameraComponent Component => Entity?.Get<CameraComponent>();

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
            Reset();

            while (true)
            {
                UpdateCamera();

                await Script.NextFrame();
            }
        }

        public void Reset()
        {
            Yaw =
                (float)
                    Math.Asin(2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.Y +
                              2 * Entity.Transform.Rotation.Z * Entity.Transform.Rotation.W);
        
            Pitch =
                (float)
                    Math.Atan2(
                        2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.W -
                        2 * Entity.Transform.Rotation.Y * Entity.Transform.Rotation.Z,
                        1 - 2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.X -
                        2 * Entity.Transform.Rotation.Z * Entity.Transform.Rotation.Z);
        }

        /// <summary>
        /// Update the camera parameters.
        /// </summary>
        protected virtual void UpdateCamera()
        {
            // Camera movement from player input
            Vector2 cameraMovement;
            cameraDirectionEvent.TryReceive(out cameraMovement);

            if (InvertY) cameraMovement.Y *= -1;
            if (InvertX) cameraMovement.X *= -1;
            
            Yaw -= cameraMovement.X * RotationSpeed;
            Pitch = MathUtil.Clamp(Pitch + cameraMovement.Y * RotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);

            // Update the camera view matrix
            UpdateViewMatrix();
        }

        private void UpdateViewMatrix()
        {
            var camera = Component;
            if (camera == null) return;
            var rotation = Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0);

            Entity.Transform.Rotation = rotation;
        }
    }
}

// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        private float desiredYaw;
        private float desiredPitch;

        /// <summary>
        /// Gets the camera component used to visualized the scene.
        /// </summary>
        private CameraComponent Component => Entity?.Get<CameraComponent>();

        /// <summary>
        /// Gets or sets the rotation speed of the camera (in radian/screen units)
        /// </summary>
        public float RotationSpeed { get; set; } = 2.355f;

        /// <summary>
        /// Gets or sets the rate at which orientation is adapted to a target value.
        /// </summary>
        /// <value>
        /// The adaptation rate.
        /// </value>
        public float RotationAdaptationSpeed { get; set; } = 5.0f;

        /// <summary>
        /// Gets or sets the Yaw rotation of the camera.
        /// </summary>
        private float Yaw { get; set; }

        /// <summary>
        /// Gets or sets the Pitch rotation of the camera.
        /// </summary>
        private float Pitch { get; set; }

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
            desiredYaw =
                Yaw =
                    (float)
                        Math.Asin(2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.Y +
                                  2 * Entity.Transform.Rotation.Z * Entity.Transform.Rotation.W);

            desiredPitch =
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
            // TODO: InvertX
            // TODO: InvertY

            // Take shortest path
            var deltaPitch = desiredPitch - Pitch;
            var deltaYaw = (desiredYaw - Yaw) % MathUtil.TwoPi;
            if (deltaYaw < 0)
                deltaYaw += MathUtil.TwoPi;
            if (deltaYaw > MathUtil.Pi)
                deltaYaw -= MathUtil.TwoPi;
            desiredYaw = Yaw + deltaYaw;

            // Perform orientation transition
            var rotationAdaptation = (float)Game.UpdateTime.Elapsed.TotalSeconds * RotationAdaptationSpeed;
            Yaw = Math.Abs(deltaYaw) < rotationAdaptation ? desiredYaw : Yaw + rotationAdaptation * Math.Sign(deltaYaw);
            Pitch = Math.Abs(deltaPitch) < rotationAdaptation ? desiredPitch : Pitch + rotationAdaptation * Math.Sign(deltaPitch);

            desiredYaw = Yaw -= 1.333f * cameraMovement.X * RotationSpeed;
            desiredPitch = Pitch = MathUtil.Clamp(Pitch + cameraMovement.Y * RotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);

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

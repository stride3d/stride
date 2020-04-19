// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
using ThirdPersonPlatformer.Player;

namespace ThirdPersonPlatformer.Camera
{
    public class ThirdPersonCamera : SyncScript
    {
        /// <summary>
        /// Starting camera distance from the target
        /// </summary>
        public float DefaultDistance { get; set; } = 6f;

        /// <summary>
        /// Minimum camera distance from the target
        /// </summary>
        public float MinimumDistance { get; set; } = 0.4f;

        /// <summary>
        /// Cone radius for the collision cone used to hold the camera
        /// </summary>
        public float ConeRadius { get; set; } = 1.25f;

        /// <summary>
        /// Check to invert the horizontal camera movement
        /// </summary>
        public bool InvertX { get; set; } = false;

        /// <summary>
        /// Minimum camera distance from the target
        /// </summary>
        public float MinVerticalAngle { get; set; } = -20f;

        /// <summary>
        /// Maximum camera distance from the target
        /// </summary>
        public float MaxVerticalAngle { get; set; } = 70f;

        /// <summary>
        /// Check to invert the vertical camera movement
        /// </summary>
        public bool InvertY { get; set; } = false;

        /// <summary>
        /// Maximum rotation speed for the camera around the target in degrees per second
        /// </summary>
        public float RotationSpeed { get; set; } = 360f;

        /// <summary>
        /// Maximum rotation speed for the camera around the target in degrees per second
        /// </summary>
        public float VerticalSpeed { get; set; } = 65f;

        private Vector3 cameraRotationXYZ = new Vector3(-20, 45, 0);
        private Vector3 targetRotationXYZ = new Vector3(-20, 45, 0);
        private readonly EventReceiver<Vector2> cameraDirectionEvent = new EventReceiver<Vector2>(PlayerInput.CameraDirectionEventKey);
        private List<HitResult> resultsOutput;
        private ConeColliderShape coneShape;

        /// <summary>
        /// Raycast between the camera and its target. The script assumes the camera is a child entity of its target.
        /// </summary>
        private void UpdateCameraRaycast()
        {
            var maxLength = DefaultDistance;
            var cameraVector = new Vector3(0, 0, DefaultDistance);
            Entity.GetParent().Transform.Rotation.Rotate(ref cameraVector);

            if (ConeRadius <= 0)
            {
                // If the cone radius
                var raycastStart = Entity.GetParent().Transform.WorldMatrix.TranslationVector;
                var hitResult = this.GetSimulation().Raycast(raycastStart, raycastStart + cameraVector);
                if (hitResult.Succeeded)
                {
                    maxLength = Math.Min(DefaultDistance, (raycastStart - hitResult.Point).Length());
                }
            }
            else
            {
                // If the cone radius is > 0 we will sweep an actual cone and see where it collides
                var fromMatrix = Matrix.Translation(0, 0, -DefaultDistance * 0.5f) *
                                 Entity.GetParent().Transform.WorldMatrix;
                var toMatrix   = Matrix.Translation(0, 0, DefaultDistance * 0.5f) *
                                 Entity.GetParent().Transform.WorldMatrix;

                resultsOutput.Clear();
                var cfg = CollisionFilterGroups.DefaultFilter;
                var cfgf = CollisionFilterGroupFlags.DefaultFilter; // Intentionally ignoring the CollisionFilterGroupFlags.StaticFilter; to avoid collision with poles

                this.GetSimulation().ShapeSweepPenetrating(coneShape, fromMatrix, toMatrix, resultsOutput, cfg, cfgf);

                foreach (var result in resultsOutput)
                {
                    if (result.Succeeded)
                    {
                        var signedVector = result.Point - Entity.GetParent().Transform.WorldMatrix.TranslationVector;
                        var signedDistance = Vector3.Dot(cameraVector, signedVector);

                        var currentLength = DefaultDistance * result.HitFraction;
                        if (signedDistance > 0 && currentLength < maxLength)
                            maxLength = currentLength;
                    }
                }
            }

            if (maxLength < MinimumDistance)
                maxLength = MinimumDistance;

            Entity.Transform.Position.Z = maxLength;
        }

        /// <summary>
        /// Raycast between the camera and its target. The script assumes the camera is a child entity of its target.
        /// </summary>
        private void UpdateCameraOrientation()
        {
            var dt = this.GetSimulation().FixedTimeStep;

            // Camera movement from player input
            Vector2 cameraMovement;
            cameraDirectionEvent.TryReceive(out cameraMovement);

            if (InvertY) cameraMovement.Y *= -1;
            targetRotationXYZ.X += cameraMovement.Y * dt * VerticalSpeed;
            targetRotationXYZ.X = Math.Max(targetRotationXYZ.X, -MaxVerticalAngle);
            targetRotationXYZ.X = Math.Min(targetRotationXYZ.X, -MinVerticalAngle);

            if (InvertX) cameraMovement.X *= -1;
            targetRotationXYZ.Y -= cameraMovement.X * dt * RotationSpeed;

            // Very simple lerp to allow smoother transition of the camera towards its desired destination. You can change this behavior with a different one, better suited for your game.
            cameraRotationXYZ = Vector3.Lerp(cameraRotationXYZ, targetRotationXYZ, 0.15f);
            Entity.GetParent().Transform.RotationEulerXYZ = new Vector3(MathUtil.DegreesToRadians(cameraRotationXYZ.X), MathUtil.DegreesToRadians(cameraRotationXYZ.Y), 0);
        }

        public override void Update()
        {
            UpdateCameraRaycast();

            UpdateCameraOrientation();
        }

        public override void Start()
        {
            base.Start();

            coneShape = new ConeColliderShape(DefaultDistance, ConeRadius, ShapeOrientation.UpZ);
            resultsOutput = new List<HitResult>();

            if (Entity.GetParent() == null) throw new ArgumentException("ThirdPersonCamera should be placed as a child entity of its target entity!");
        }
    }
}

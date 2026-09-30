// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.BepuPhysics;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using ThirdPersonPlatformer.Player;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;

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
        /// Sphere radius for the collision sphere used to hold the camera
        /// </summary>
        public float SphereRadius { get; set; } = 0.5f;

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
        private readonly List<HitInfo> resultsOutput = [];

        /// <summary>
        /// Raycast between the camera and its target. The script assumes the camera is a child entity of its target.
        /// </summary>
        private void UpdateCameraRaycast()
        {
            var maxLength = DefaultDistance;
            var cameraVector = Vector3.UnitZ;
            {
                var current = Entity.GetParent();
                while (current != null)
                {
                    current.Transform.Rotation.Rotate(ref cameraVector);
                    current = current.GetParent();
                }
            }

            // The samples use the following collision layers:
            // Layer 0: default
            // Layer 1: player
            // Layer 2: ground
            // Layer 3: wall
            // Layer 4: pillar
            // layer 5: custom

            // Intentionally ignoring CollisionMask.Layer4; to avoid collision with poles
            var collisionMask = ~(CollisionMask.Layer1 | CollisionMask.Layer4);
            if (SphereRadius <= 0)
            {
                // If the sphere radius is non-positive we will just raycast and see where it collides
                var raycastStart = Entity.GetParent().Transform.WorldMatrix.TranslationVector;
                if (Entity.GetSimulation().RayCast(raycastStart, cameraVector, DefaultDistance, out var hitResult, collisionMask))
                {
                    maxLength = Math.Min(DefaultDistance, (raycastStart - hitResult.Point).Length());
                }
            }
            else
            {
                // If the sphere radius is > 0 we will sweep an actual sphere and see where it collides
                var sweepcastStart = Entity.GetParent().Transform.WorldMatrix.TranslationVector;

                var pose = new RigidPose(sweepcastStart, Quaternion.Identity);
                var velocity = new BodyVelocity { Linear = cameraVector };

                resultsOutput.Clear();

                Entity.GetSimulation().SweepCastPenetrating(new Sphere(SphereRadius), pose, velocity, DefaultDistance, resultsOutput, collisionMask);

                foreach (var result in resultsOutput)
                {
                    var signedVector = result.Point - sweepcastStart;
                    var signedDistance = Vector3.Dot(cameraVector, signedVector);

                    if (signedDistance > 0 && result.Distance < maxLength)
                        maxLength = result.Distance;
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
            // Camera movement from player input
            Vector2 cameraMovement;
            cameraDirectionEvent.TryReceive(out cameraMovement);

            if (InvertY) cameraMovement.Y *= -1;
            targetRotationXYZ.X += cameraMovement.Y * VerticalSpeed;
            targetRotationXYZ.X = Math.Max(targetRotationXYZ.X, -MaxVerticalAngle);
            targetRotationXYZ.X = Math.Min(targetRotationXYZ.X, -MinVerticalAngle);

            if (InvertX) cameraMovement.X *= -1;
            targetRotationXYZ.Y -= cameraMovement.X * RotationSpeed;

            // Very simple lerp to allow smoother transition of the camera towards its desired destination. You can change this behavior with a different one, better suited for your game.
            cameraRotationXYZ = Vector3.Lerp(cameraRotationXYZ, targetRotationXYZ, 0.25f);
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

            if (Entity.GetParent() == null) throw new ArgumentException("ThirdPersonCamera should be placed as a child entity of its target entity!");
        }
    }
}

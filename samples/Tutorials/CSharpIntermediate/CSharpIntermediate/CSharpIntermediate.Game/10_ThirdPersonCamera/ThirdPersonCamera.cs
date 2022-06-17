// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class ThirdPersonCamera : SyncScript
    {
        public bool InvertMouseY = false;
        public Vector2 MouseSpeed = new Vector2(0.6f, 0.5f);
        public float MaxLookUpAngle = -40;
        public float MaxLookDownAngle = 40;
        public float MinimumCameraDistance = 1.5f;
        public Vector3 CameraOffset = new Vector3(0, 0, -3);

        private Entity firstPersonPivot;
        private Entity thirdPersonPivot;

        private Vector2 maxCameraAnglesRadians;
        private Vector3 camRotation;
        private bool isActive = false;
        private Simulation simulation;
        private CharacterComponent character;

        public override void Start()
        {
            Game.IsMouseVisible = false;
            isActive = true;

            firstPersonPivot = Entity.FindChild("FirstPersonPivot");
            thirdPersonPivot = Entity.FindChild("ThirdPersonPivot");

            maxCameraAnglesRadians = new Vector2(MathUtil.DegreesToRadians(MaxLookUpAngle), MathUtil.DegreesToRadians(MaxLookDownAngle));
            camRotation = Entity.Transform.RotationEulerXYZ;
            Input.MousePosition = new Vector2(0.5f, 0.5f);
            simulation = this.GetSimulation();
            character = Entity.Get<CharacterComponent>();
        }

        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.Escape))
            {
                isActive = !isActive;
                Game.IsMouseVisible = !isActive;
                Input.UnlockMousePosition();
            }

            if (isActive)
            {
                Input.LockMousePosition();
                var mouseMovement = -Input.MouseDelta * MouseSpeed;

                // Update rotation values with the mouse movement
                camRotation.Y += mouseMovement.X;
                camRotation.X += InvertMouseY ? mouseMovement.Y : -mouseMovement.Y;
                camRotation.X = MathUtil.Clamp(camRotation.X, maxCameraAnglesRadians.X, maxCameraAnglesRadians.Y);

                // Apply Y rotation to character entity
                character.Orientation = Quaternion.RotationY(camRotation.Y);

                // Apply X rotation the existing first person pivot
                firstPersonPivot.Transform.Rotation = Quaternion.RotationX(camRotation.X);

                // The third person pivot gets the same position and rotation as the first person pivot + the camera offset
                thirdPersonPivot.Transform.Position = new Vector3(0);
                thirdPersonPivot.Transform.Position += CameraOffset;

                // Make sure that the WorldMatrix of the thirdperson pivot is up to date
                thirdPersonPivot.Transform.UpdateWorldMatrix();

                // Raycast from first person pivot to third person pivot
                var raycastStart = firstPersonPivot.Transform.WorldMatrix.TranslationVector;
                var raycastEnd = thirdPersonPivot.Transform.WorldMatrix.TranslationVector;

                if (simulation.Raycast(raycastStart, raycastEnd, out HitResult hitResult))
                {
                    // If we hit something along the way, calculate the distance
                    var hitDistance = Vector3.Distance(raycastStart, hitResult.Point);

                    if (hitDistance >= MinimumCameraDistance)
                    {
                        // If the distance is larger than the minimum distance, place the camera at the hitpoint
                        thirdPersonPivot.Transform.Position.Z = -(hitDistance-0.1f);
                    }
                    else
                    {
                        // If the distance is lower than the minimum distance, place the camera at first person pivot
                        thirdPersonPivot.Transform.Position = new Vector3(0);
                    }
                }
            }
        }
    }
}

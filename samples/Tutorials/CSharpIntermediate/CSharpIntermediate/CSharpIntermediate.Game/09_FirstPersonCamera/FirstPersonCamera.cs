// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class FirstPersonCamera : SyncScript
    {
        public float MouseSpeed = 0.6f;
        public float MaxLookUpAngle = -50;
        public float MaxLookDownAngle = 50;
        public bool InvertMouseY = false;

        private Entity firstPersonCameraPivot;
        private Vector3 camRotation;
        private bool isActive = false;
        private Vector2 maxCameraAnglesRadians;

        public override void Start()
        {
            firstPersonCameraPivot = Entity.FindChild("CameraPivot");

            // Convert the Max camera angles from Degress to Radions
            maxCameraAnglesRadians = new Vector2(MathUtil.DegreesToRadians(MaxLookUpAngle), MathUtil.DegreesToRadians(MaxLookDownAngle));
            
            // Store the initial camera rotation
            camRotation = Entity.Transform.RotationEulerXYZ;

            // Set the mouse to the middle of the screen
            Input.MousePosition = new Vector2(0.5f, 0.5f);

            isActive = true;
            Game.IsMouseVisible = false;
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

                // Update camera rotation values
                camRotation.Y += mouseMovement.X;
                camRotation.X += InvertMouseY ? mouseMovement.Y : -mouseMovement.Y;
                camRotation.X = MathUtil.Clamp(camRotation.X, maxCameraAnglesRadians.X, maxCameraAnglesRadians.Y);

                // Apply Y rotation to character entity
                Entity.Transform.Rotation = Quaternion.RotationY(camRotation.Y);

                // Apply X rptatopmnew camera rotation to the existing camera rotations
                firstPersonCameraPivot.Transform.Rotation = Quaternion.RotationX(camRotation.X);
            }
        }
    }
}

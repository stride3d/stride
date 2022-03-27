using System;
using System.Collections.Generic;
using CSharpIntermediate.Code.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class FirstPersonCamera : SyncScript
    {
        public float MouseSpeed = 1.5f;
        public float MaxLookUpAngle = -50;
        public float MaxLookDownAngle = 50;
        public bool InvertMouseY = false;

        private Entity firstPersonCameraPivot;
        private Vector2 mouseDif;
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
            }

            if (isActive)
            {
                var mouseMovement = new Vector2(0, 0);
                var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                var mousePos = Input.MousePosition;
                mouseDif = new Vector2(0.5f - mousePos.X, 0.5f - mousePos.Y);

                // Adjust and set the camera rotation
                mouseMovement.X += mouseDif.X * MouseSpeed * deltaTime;
                mouseMovement.Y += mouseDif.Y * MouseSpeed * deltaTime;

                // Update camera rotation values
                camRotation.Y += mouseMovement.X;
                camRotation.X += InvertMouseY ? mouseMovement.Y : -mouseMovement.Y;
                camRotation.X = MathUtil.Clamp(camRotation.X, maxCameraAnglesRadians.X, maxCameraAnglesRadians.Y);

                // Apply Y rotation to character entity
                Entity.Transform.Rotation = Quaternion.RotationY(camRotation.Y);

                // Apply X rptatopmnew camera rotation to the existing camera rotations
                firstPersonCameraPivot.Transform.Rotation = Quaternion.RotationX(camRotation.X);
                

                Input.MousePosition = new Vector2(0.5f);
            }
        }
    }
}

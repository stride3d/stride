using System;
using System.Collections.Generic;
using CSharpIntermediate.Code.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class ThirdPersonCamera : SyncScript
    {
        public bool InvertMouseY = false;
        public Vector2 MouseSpeed = new Vector2(20, 14);
        public float MaxLookUpAngle = -50;
        public float MaxLookDownAngle = 50;
        public float MinimumCameraDistance = 0.5f;
        public float MaxCameraDistance = 2;

        private Entity firstPersonPivot;
        private Entity thirdPersonPivot;
        private Entity cameraEntity;

        private Vector2 mouseDif;
        private Vector3 camRotation;
        private bool isActive = false;
        private Vector2 maxCameraAnglesRadians;
        private Simulation simulation;

        public override void Start()
        {
            firstPersonPivot = Entity.FindChild("FirstPersonPivot");
            thirdPersonPivot = Entity.FindChild("ThirdPersonPivot");
            cameraEntity = Entity.FindChild("Camera");

            maxCameraAnglesRadians = new Vector2(MathUtil.DegreesToRadians(MaxLookUpAngle), MathUtil.DegreesToRadians(MaxLookDownAngle));
            camRotation = Entity.Transform.RotationEulerXYZ;
            Input.MousePosition = new Vector2(0.5f, 0.5f);
            isActive = true;
            Game.IsMouseVisible = false;


            simulation = this.GetSimulation();
        }


        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.Escape))
            {
                isActive = !isActive;
                Game.IsMouseVisible = !isActive;
            }

            if (Input.IsKeyPressed(Keys.Up))
            {
                MaxCameraDistance += 0.1f;
            }
            if (Input.IsKeyPressed(Keys.Down))
            {
                MaxCameraDistance -= 0.1f;

            }


            if (isActive)
            {
                var mouseMovement = new Vector2(0, 0);
                var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                var mousePos = Input.MousePosition;
                mouseDif = new Vector2(0.5f - mousePos.X, 0.5f - mousePos.Y);

                // Adjust and set the camera rotation
                mouseMovement.X += mouseDif.X * MouseSpeed.X * deltaTime;
                mouseMovement.Y += mouseDif.Y * MouseSpeed.Y * deltaTime;

                // Update camera rotation values
                camRotation.Y += mouseMovement.X;
                camRotation.X += InvertMouseY ? mouseMovement.Y : -mouseMovement.Y;
                camRotation.X = MathUtil.Clamp(camRotation.X, maxCameraAnglesRadians.X, maxCameraAnglesRadians.Y);

                // Apply Y rotation to character entity
                Entity.Transform.Rotation = Quaternion.RotationY(camRotation.Y);

                // Apply X rotation the existing first person pivot
                firstPersonPivot.Transform.Rotation = Quaternion.RotationX(camRotation.X);


                thirdPersonPivot.Transform.Rotation = firstPersonPivot.Transform.Rotation;
                thirdPersonPivot.Transform.Position = firstPersonPivot.Transform.Position;
                thirdPersonPivot.Transform.Position.Z -= MaxCameraDistance;
                
                //Temp
                //cameraEntity.Transform.Position = thirdPersonPivot.Transform.Position;


                //var raycastStart = FirstPersonPivot.Transform.WorldMatrix.TranslationVector;
                //var raycastEnd = raycastStart;
                //raycastEnd.Z -= MaxCameraDistance;

                //if (simulation.Raycast(raycastStart, raycastEnd, out HitResult hitResult))
                //{
                //    var distance = Vector3.Distance(raycastStart, hitResult.Point);
                //    if (distance >= MinimumCameraDistance)
                //    {
                //        cameraEntity.Transform.WorldMatrix.TranslationVector = hitResult.Point;
                //    }
                //    else
                //    {
                //        cameraEntity.Transform.Position = new Vector3(0);
                //    }
                //}
                //else
                //{
                //   cameraEntity.Transform.Position = ThirdPersonPivot.Transform.Position;
                //}

                Input.MousePosition = new Vector2(0.5f);
            }
        }
    }
}

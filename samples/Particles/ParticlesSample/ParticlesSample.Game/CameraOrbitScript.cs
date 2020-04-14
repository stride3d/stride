// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Input;
using Stride.Core.Mathematics;

namespace ParticlesSample
{
    /// <summary>
    /// Script that update the position of the camera.
    /// </summary>
    public class CameraOrbitScript : AsyncScript
    {
        private readonly Vector3 lookAtPosition = new Vector3(0, 0, 0);
        private float lookAtAngle = 90f;
        private float lookAtAngleY = 10f;
        private const float lookFromDistance = 8f;
        private const float MinimumCameraHeight = 0f;
        private const float MaximumCameraHeight = 50f;
        private const float AbsoluteMaxSpeedX = 5f;
        private const float AbsoluteMaxSpeedY = 2f;
        private const float Friction = 0.9f;
        private const float Frametime = 1 / 60.0f;
        private float timeToProcess;
        private float movingSpeedX;
        private float movingSpeedY;
        private bool userIsTouchingScreen;

        public override async Task Execute()
        {
            while (Game.IsRunning)
            {
                var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                timeToProcess = Math.Max(timeToProcess + elapsedTime, 1.0f);

                // determine if the user is currently touching the screen.
                if (Input.PointerEvents.Count > 0)
                    userIsTouchingScreen = Input.Pointer.DownPointers.Any(x=>x.IsDown);

                // calculate the current speed of the camera
                if (userIsTouchingScreen)
                {
                    movingSpeedX += -AbsoluteMaxSpeedX * Input.PointerEvents.Sum(x => x.DeltaPosition.X);
                    if (Math.Abs(movingSpeedX) > AbsoluteMaxSpeedX)
                    {
                        movingSpeedX = AbsoluteMaxSpeedX * Math.Sign(movingSpeedX);
                    }

                    movingSpeedY += AbsoluteMaxSpeedY * Input.PointerEvents.Sum(y => y.DeltaPosition.Y);
                    if (Math.Abs(movingSpeedY) > AbsoluteMaxSpeedY)
                    {
                        movingSpeedY = AbsoluteMaxSpeedY * Math.Sign(movingSpeedY);
                    }


                    timeToProcess = timeToProcess % Frametime;
                    UpdatePosition(movingSpeedX * 2, movingSpeedY * 2);
                }
                else
                {
                    while (timeToProcess >= Frametime)
                    {
                        timeToProcess -= Frametime;
                        var previousSpeedX = movingSpeedX;
                        movingSpeedX = (float)(movingSpeedX * Math.Pow(Friction, Frametime));
                        var previousSpeedY = movingSpeedY;
                        movingSpeedY = (float)(movingSpeedY * Math.Pow(Friction, Frametime));
                        UpdatePosition(previousSpeedX + movingSpeedX, previousSpeedY + movingSpeedY);
                    }
                }

                // wait until next frame
                await Script.NextFrame();
            }
        }

        private void UpdatePosition(float speedX, float speedY)
        {
            lookAtAngle = lookAtAngle + (speedX) * 7.5f * Frametime;
            lookAtAngleY = lookAtAngleY + (speedY) * 1.5f * Frametime;
            lookAtAngleY = MathUtil.Clamp(lookAtAngleY, MinimumCameraHeight, MaximumCameraHeight);

            var maxDistance = lookFromDistance*(1 + (float) Math.Sin(MathUtil.DegreesToRadians(lookAtAngleY)));

            var distance = maxDistance * (float)Math.Cos(MathUtil.DegreesToRadians(lookAtAngleY));
            Entity.Transform.Position.X = (float)Math.Sin(MathUtil.DegreesToRadians(lookAtAngle)) * distance;
            Entity.Transform.Position.Z = (float)Math.Cos(MathUtil.DegreesToRadians(lookAtAngle)) * distance;
            Entity.Transform.Position.Y = maxDistance*(float) Math.Sin(MathUtil.DegreesToRadians(lookAtAngleY));
            Entity.Transform.Position += lookAtPosition;

            Entity.Transform.Rotation = Quaternion.RotationAxis(new Vector3(1, 0, 0), MathUtil.DegreesToRadians(-lookAtAngleY)) * 
                                        Quaternion.RotationAxis(new Vector3(0, 1, 0), MathUtil.DegreesToRadians(lookAtAngle));
        }
    }
}

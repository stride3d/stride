using System;
using BepuPhysicIntegrationTest.Integration.Components.Constraints;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class CarControllerComponent : SyncScript
    {
        private const float steeringSpeed = 0.5f;
        private float previousSteeringAngle = 0;
        private float steeringAngle = 0;
        private float MaximumSteeringAngle = 30;

        public AngularAxisMotorConstraintComponent? LeftMotor { get; set; }
        public AngularAxisMotorConstraintComponent? RightMotor { get; set; }
        public AngularAxisMotorConstraintComponent? LeftBMotor { get; set; }
        public AngularAxisMotorConstraintComponent? RightBMotor { get; set; }

        public AngularHingeConstraintComponent? LeftWheel { get; set; }
        public AngularHingeConstraintComponent? RightWheel { get; set; }



        public override void Start()
        {
        }

        public override void Update()
        {
            if (LeftMotor == null || RightMotor == null || LeftBMotor == null || RightBMotor == null || LeftWheel == null || RightWheel == null)
                return;

            if (Input.IsKeyDown(Keys.Up))
            {
                LeftMotor.TargetVelocity = 300;
                LeftBMotor.TargetVelocity = 300;
                RightMotor.TargetVelocity = 300;
                RightBMotor.TargetVelocity = 300;
            }
            else if (Input.IsKeyDown(Keys.Down))
            {
                LeftMotor.TargetVelocity = -100;
                LeftBMotor.TargetVelocity = -100;
                RightMotor.TargetVelocity = -100;
                RightBMotor.TargetVelocity = -100;
            }
            else
            {
                if (LeftMotor.TargetVelocity != 0)
                    LeftMotor.TargetVelocity = 0;

                if (LeftBMotor.TargetVelocity != 0)
                    LeftBMotor.TargetVelocity = 0;

                if (RightMotor.TargetVelocity != 0)
                    RightMotor.TargetVelocity = 0;

                if (RightBMotor.TargetVelocity != 0)
                    RightBMotor.TargetVelocity = 0;
            }

            if (Input.IsKeyDown(Keys.Right))
            {
                steeringAngle -= steeringSpeed;
            }
            else if (Input.IsKeyDown(Keys.Left))
            {
                steeringAngle += steeringSpeed;
            }
            else if (steeringAngle >= steeringSpeed)
            {
                steeringAngle -= steeringSpeed;
            }
            else if (steeringAngle <= -steeringSpeed)
            {
                steeringAngle += steeringSpeed;
            }
            else
            {
                steeringAngle = 0f;
            }

            steeringAngle = MathF.Max(MathF.Min(MaximumSteeringAngle, steeringAngle), -MaximumSteeringAngle);
            if (steeringAngle != previousSteeringAngle)
            {
                var result = Vector3.RotateAround(Vector3.UnitZ, Vector3.Zero, Vector3.UnitY, steeringAngle * (MathF.PI / 180));
                LeftWheel.LocalHingeAxisA = result;
                RightWheel.LocalHingeAxisA = result;
                previousSteeringAngle = steeringAngle;
            }
        }
    }
}

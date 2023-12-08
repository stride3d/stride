using System;
using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Components.Containers;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class CarControllerComponent : SyncScript
    {
        private float _accelerationForce = 1000;
        private float _breakingForce = 4000f;


        private float _acceleration = 0.02f;
        private float _engineBrakeCoef = 0.99f;
        private float _speed = 0;
        private float _previousSpeed = 0;
        private float _maximumSpeed = 100;
        private float _minimumSpeed = -40;

        private float steeringSpeed = 0.35f;
        private float steeringAngle = 0;
        private float previousSteeringAngle = 0;
        private float MaximumSteeringAngle = 30; //maybe reduce steerringSpeed/maximum with speed



        public BodyContainerComponent? CarBody { get; set; }
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

            DebugText.Print($"steeringAngle : {steeringAngle} | _speed : {_speed} <==> real : {LeftMotor.Bodies[0].GetPhysicBody()?.Velocity.Angular.Length()} | linear speed {CarBody?.GetPhysicBody()?.Velocity.Linear.Length()}", new(100, 100));

            //Motor
            if (Input.IsKeyDown(Keys.Up))
            {
                _speed += _acceleration;
            }
            else if (Input.IsKeyDown(Keys.Down))
            {
                _speed -= _acceleration;
            }
            else if (Input.IsKeyDown(Keys.Space))
            {
                _speed = 0;
            }
            else
            {
                _speed *= _engineBrakeCoef;
            }

            _speed = MathF.Max(MathF.Min(_maximumSpeed, _speed), _minimumSpeed);
            if (_speed != _previousSpeed)
            {
                if (MathF.Abs(_speed) > MathF.Abs(_previousSpeed) && LeftMotor.MotorMaximumForce != _accelerationForce)
                {
                    LeftMotor.MotorMaximumForce = _accelerationForce;
                    RightMotor.MotorMaximumForce = _accelerationForce;
                }
                else if (LeftMotor.MotorMaximumForce != _breakingForce)
                {
                    LeftMotor.MotorMaximumForce = _breakingForce;
                    RightMotor.MotorMaximumForce = _breakingForce;
                }
                LeftMotor.TargetVelocity = _speed;
                //LeftBMotor.TargetVelocity = _speed;
                RightMotor.TargetVelocity = _speed;
                //RightBMotor.TargetVelocity = _speed;
                _previousSpeed = _speed;
            }

            //Steering
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

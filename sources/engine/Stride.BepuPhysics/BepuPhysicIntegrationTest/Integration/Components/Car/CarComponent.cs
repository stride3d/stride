using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysicIntegrationTest.Integration.Components.Constraints;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using Silk.NET.OpenGL;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{

    public class Engine
    {
        public int MinRPM { get; set; } = 1000;
        public int MaxRPM { get; set; } = 10000;
        public float EngineInertia { get; set; } = 1f;

        public List<EngineGear> Gears { get; set; } = new List<EngineGear>() { new() { AccelerationForce = 500, GearRatio = -0.001f }, new() { AccelerationForce = 1000, GearRatio = 0.001f } }; //0 => reverse, 1 => first, ..

    }
    public class EngineGear
    {
        public float AccelerationForce { get; set; }
        public float GearRatio { get; set; }

    }
    public class WheelComponent : StartupScript
    {
        public float Friction { get; set; } = 1.5f;
        public float DamperLen { get; set; } = 0.5f;
        public float DamperRatio { get; set; } = 0.01f;
        public float DamperForce { get; set; } = 1000f;
    }

    [ComponentCategory("Bepu - Car")]
    public class CarComponent : SimulationUpdateComponent
    {

        public const float GEAR_UP_VALUE = 0.9f;
        public const float GEAR_DOWN_VALUE = 0.1f;

        //--Refs--
        //Every Entities must contains a BodyContainerComponent & their collider.
        public Entity CarBody { get; set; } = new();
        public List<Entity> Wheels { get; set; } = new(); //all the car wheel
        public List<Entity> SteerWheels { get; set; } = new(); //wheel that can turn
        public List<Entity> DriveWheels { get; set; } = new(); //engine wheel
        public List<Entity> BreakWheels { get; set; } = new(); //breaking wheel


        //--Parameters--
        //keybind
        public List<Keys> AccelerateKeys { get; set; } = new() { Keys.Up };
        public List<Keys> ReverseKeys { get; set; } = new() { Keys.Down };
        public List<Keys> BreakKeys { get; set; } = new() { Keys.Space };

        public List<Keys> SteerLeftKeys { get; set; } = new() { Keys.Left };
        public List<Keys> SteerRightKeys { get; set; } = new() { Keys.Right };

        public List<Keys> GearUpKeys { get; set; } = new() { Keys.NumPad0 };
        public List<Keys> GearDownKeys { get; set; } = new() { Keys.RightCtrl };
        public List<Keys> ClutchKeys { get; set; } = new() { Keys.Decimal };
        public List<Keys> StarterKeys { get; set; } = new() { Keys.Enter };

        //car
        public Engine CarEngine { get; set; } = new();
        public float BreakingForce { get; set; } = 4000f;
        public float MaximumSteeringAngle { get; set; } = 30;
        public float SteeringSpeed { get; set; } = 0.5f;


        //--Actions--
        public bool Clutch { get; set; } = true;
        public bool Starter { get; set; } = false; //set to true while clutch is true to start the engine to MinRPM.

        //--Control Helper--
        public bool AutomaticGearing { get; set; } = true; //Wil handle "Clutch" & gears automatically
        public bool AutomaticStart { get; set; } = true; //Wil handle "Starter" automatically (car always running)

        //Engine internal parameters
        public float PreviousSteeringAngle { get; private set; } = 0;
        public float SteeringAngle { get; private set; } = 0;
        public int CurrentGear { get; private set; } = -1;
        public int CurrentRPM { get; private set; } = 0;
        public bool EngineRunning => CurrentRPM > 0;

        public override void Start()
        {
            if (CarBody == null || Wheels.Count == 0)
                throw new Exception("CarComponent : must contains at least one body & one wheel");

            var bodyContainer = CarBody.Get<BodyContainerComponent>();

            foreach (var wheel in Wheels)
            {
                var wheelContainer = wheel.Get<BodyContainerComponent>();
                var WheelComponent = wheel.Get<WheelComponent>() ?? new();

                wheelContainer.FrictionCoefficient = WheelComponent.Friction;


                var polscc = new PointOnLineServoConstraintComponent();
                polscc.Bodies.Add(bodyContainer);
                polscc.Bodies.Add(wheelContainer);

                polscc.LocalOffsetA = wheel.Transform.GetWorldPos() - CarBody.Transform.GetWorldPos();
                polscc.LocalOffsetB = new();
                polscc.LocalDirection = Vector3.UnitY;

                wheel.Add(polscc);

                var lascc = new LinearAxisServoConstraintComponent();
                lascc.Bodies.Add(bodyContainer);
                lascc.Bodies.Add(wheelContainer);

                lascc.LocalOffsetA = wheel.Transform.GetWorldPos() - CarBody.Transform.GetWorldPos();
                lascc.LocalOffsetB = new();
                lascc.LocalPlaneNormal = Vector3.UnitY;

                lascc.TargetOffset = WheelComponent.DamperLen;
                lascc.SpringDampingRatio = WheelComponent.DamperRatio;
                lascc.ServoMaximumForce = WheelComponent.DamperForce;
                wheel.Add(lascc);

                var ahcc = new AngularHingeConstraintComponent();
                ahcc.Bodies.Add(bodyContainer);
                ahcc.Bodies.Add(wheelContainer);

                ahcc.LocalHingeAxisA = Vector3.UnitZ;
                ahcc.LocalHingeAxisB = Vector3.UnitY;
                wheel.Add(ahcc);
            }

            //foreach (var wheel in DriveWheels.Union(BreakWheels))
            //{
            //    var wheelContainer = wheel.Get<BodyContainerComponent>();

            //    var aamcc = new AngularAxisMotorConstraintComponent();
            //    aamcc.Bodies.Add(wheelContainer);
            //    aamcc.Bodies.Add(bodyContainer);

            //    aamcc.LocalAxisA = Vector3.UnitY;
            //    aamcc.MotorDamping = 50;
            //    aamcc.Enabled = false;
            //    wheel.Add(aamcc);
            //}
            base.Start();
        }

        public override void Update()
        {
            var bodies = DriveWheels.Select(e => (BepuPhysics.BodyReference)e.Get<BodyContainerComponent>().GetPhysicBody());
            var wheelMeanRPM = bodies.Select(e => e.Velocity.Angular.Length()).Aggregate((a, b) => a + b) / bodies.Count();
            var engineMeanRPM = CurrentGear == -1 ? 0 : wheelMeanRPM / CarEngine.Gears[CurrentGear].GearRatio;

            DebugText.Print($"Clutch:{Clutch}" + " | " +
                $"Starter:{Starter}" + " | " +
                $"SteeringAngle:{SteeringAngle}" + " | " +
                $"CurrentGear:{CurrentGear}" + " | " +
                $"CurrentRPM:{CurrentRPM}" + " | " +
                $"EngineRunning:{EngineRunning}" + " | " +
                $"wheelMeanRPM:{wheelMeanRPM}" + " | " +
                $"engineMeanRPM:{engineMeanRPM}" + " | " +
                $"", new(100, 100));
        }

        private void HandleGearing()
        {
            if (AutomaticGearing)
            {
                var deltaRPM = CarEngine.MaxRPM - CarEngine.MinRPM;

                if (CurrentRPM > CarEngine.MinRPM + GEAR_UP_VALUE * deltaRPM)
                {
                    var notInMaxGear = CurrentGear < CarEngine.Gears.Count() - 1;
                    if (CurrentGear > 0 && notInMaxGear)
                        CurrentGear++;
                    else if (CurrentGear == -1)
                    {
                        CurrentGear = 1;
                        Clutch = false;
                    }
                }
                else if (CurrentRPM < CarEngine.MinRPM + GEAR_DOWN_VALUE * deltaRPM)
                {
                    if (CurrentGear > 1)
                        CurrentGear--;
                    //else
                    //{
                    //    CurrentGear = -1;
                    //    Clutch = true;
                    //}
                }
            }
            else
            {
                if (GearUpKeys.Any(Input.IsKeyPressed) && Clutch)
                    CurrentGear++;
                if (GearDownKeys.Any(Input.IsKeyPressed) && Clutch)
                    CurrentGear--;
            }
        }
        private void HandleEngineStartingAndUpdate()
        {
            if (!EngineRunning)
            {
                Starter = StarterKeys.Any(Input.IsKeyPressed);

                if (Starter || AutomaticStart)
                {
                    if (AutomaticGearing)
                        Clutch = true;
                    CurrentRPM = CarEngine.MinRPM;
                    Starter = false;
                }
            }

            SetEngineParametersFromPhysic();
        }
        private void HandleEngine()
        {
            if (EngineRunning)
            {
                var engineAcceleration = false;


                if (AccelerateKeys.Any(Input.IsKeyDown))
                {
                    engineAcceleration = true;
                    CurrentRPM = (int)(CurrentRPM * 1.02f);
                }
                //TODO elseif


                if (!engineAcceleration)
                    CurrentRPM = (int)(CurrentRPM * 0.95f);


                CurrentRPM = Math.Min(CarEngine.MaxRPM, CurrentRPM);
                CurrentRPM = Math.Max(CarEngine.MinRPM, CurrentRPM);

                //DriveWheels.ForEach(e =>
                //{
                //    var aamcc = e.Get<AngularAxisMotorConstraintComponent>();
                //    aamcc.TargetVelocity = CurrentGear == -1 ? 0 : CurrentRPM * CarEngine.Gears[CurrentGear].GearRatio;
                //    aamcc.MotorMaximumForce = CurrentGear == -1 ? 0 : CarEngine.Gears[CurrentGear].AccelerationForce;
                //    aamcc.Enabled = !Clutch;
                //});
            }
        }
        private void HandleSteering()
        {
            var idle = true;
            if (SteerRightKeys.Any(Input.IsKeyDown))
            {
                SteeringAngle -= SteeringSpeed;
                idle = false;
            }
            if (SteerLeftKeys.Any(Input.IsKeyDown))
            {
                SteeringAngle += SteeringSpeed;
                idle = false;
            }

            if (idle & SteeringAngle >= SteeringSpeed)
            {
                SteeringAngle -= SteeringSpeed;
            }
            else if (idle & SteeringAngle <= -SteeringSpeed)
            {
                SteeringAngle += SteeringSpeed;
            }
            else if (idle)
            {
                SteeringAngle = 0f;
            }


            SteeringAngle = MathF.Max(MathF.Min(MaximumSteeringAngle, SteeringAngle), -MaximumSteeringAngle);
            if (SteeringAngle != PreviousSteeringAngle)
            {
                var result = Vector3.RotateAround(Vector3.UnitZ, Vector3.Zero, Vector3.UnitY, SteeringAngle * (MathF.PI / 180));

                SteerWheels.ForEach(e => e.Get<AngularHingeConstraintComponent>().LocalHingeAxisA = result);

                PreviousSteeringAngle = SteeringAngle;
            }
        }


        private void SetEngineParametersFromPhysic()
        {
            if (Clutch || CurrentGear == -1)
                return;

            var bodies = DriveWheels.Select(e => (BepuPhysics.BodyReference)e.Get<BodyContainerComponent>().GetPhysicBody());
            var wheelMeanRPM = bodies.Select(e => e.Velocity.Angular.Length()).Aggregate((a, b) => a + b) / bodies.Count();
            var engineMeanRPM = wheelMeanRPM / CarEngine.Gears[CurrentGear].GearRatio;
            CurrentRPM = (int)((CurrentRPM * CarEngine.EngineInertia + engineMeanRPM) / (CarEngine.EngineInertia + 1));

            //TODO : set velocity in the right direction    
            bodies.ForEach(e => e.Velocity.Angular = new System.Numerics.Vector3(0, 0, CurrentRPM * CarEngine.Gears[CurrentGear].GearRatio)); //deltaRPM * CarEngine.EngineInertia * CarEngine.Gears[CurrentGear].GearRatio
        }

        public override void SimulationUpdate(float simTimeStep)
        {
            HandleGearing();
            HandleEngineStartingAndUpdate();
            HandleEngine();
            HandleSteering();
        }
    }
}

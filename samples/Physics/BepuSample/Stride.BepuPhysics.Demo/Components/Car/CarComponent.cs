// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Demo.Car;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Demo.Components.Car
{

#warning This need rework/Rename and could be part of the API
    [ComponentCategory("BepuDemo - Car")]
    public class CarComponent : SyncScript, ISimulationUpdate
    {
        public const float GEAR_UP_VALUE = 0.9f;
        public const float GEAR_DOWN_VALUE = 0.1f;

        //--Refs--
        //Every Entities must contains a BodyComponent & their collider.
        public Entity CarBody { get; set; } = new();
        public List<Entity> Wheels { get; set; } = new(); //all the car wheel
        public List<Entity> SteerWheels { get; set; } = new(); //wheel that can turn
        public List<Entity> DriveWheels { get; set; } = new(); //engine wheel
        public List<Entity> BreakWheels { get; set; } = new(); //breaking wheel


        //--Parameters--
        //keybind
        public List<Keys> AccelerateKeys { get; set; } = new();// = new() { Keys.Up };
        public List<Keys> ReverseKeys { get; set; } = new();// = new() { Keys.Down };
        public List<Keys> BreakKeys { get; set; } = new();// = new() { Keys.Space };

        public List<Keys> SteerLeftKeys { get; set; } = new();// = new() { Keys.Left };
        public List<Keys> SteerRightKeys { get; set; } = new();// = new() { Keys.Right };

        public List<Keys> GearUpKeys { get; set; } = new();// = new() { Keys.NumPad0 };
        public List<Keys> GearDownKeys { get; set; } = new();// = new() { Keys.RightCtrl };
        public List<Keys> ClutchKeys { get; set; } = new();// = new() { Keys.Decimal };
        public List<Keys> StarterKeys { get; set; } = new();// = new() { Keys.Enter };

        //car
        public CarEngine CarEngine { get; set; } = new();
        public float BreakingForce { get; set; } = 0.1f;
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

            var bodyCollidable = CarBody.Get<BodyComponent>();

            foreach (var wheel in Wheels)
            {
                var wheelCollidable = wheel.Get<BodyComponent>();
                var WheelComponent = wheel.Get<WheelComponent>() ?? new();

                var polscc = new PointOnLineServoConstraintComponent();
                polscc.A = bodyCollidable;
                polscc.B = wheelCollidable;

                polscc.LocalOffsetA = wheel.Transform.GetWorldPos() - CarBody.Transform.GetWorldPos();
                polscc.LocalOffsetB = new();
                polscc.LocalDirection = Vector3.UnitY;

                wheel.Add(polscc);

                var lascc = new LinearAxisServoConstraintComponent();
                lascc.A = bodyCollidable;
                lascc.B = wheelCollidable;

                lascc.LocalOffsetA = wheel.Transform.GetWorldPos() - CarBody.Transform.GetWorldPos();
                lascc.LocalOffsetB = new();
                lascc.LocalPlaneNormal = Vector3.UnitY;

                lascc.TargetOffset = WheelComponent.DamperLen;
                lascc.SpringDampingRatio = WheelComponent.DamperRatio;
                lascc.ServoMaximumForce = WheelComponent.DamperForce;
                wheel.Add(lascc);

                var ahcc = new AngularHingeConstraintComponent();
                ahcc.A = bodyCollidable;
                ahcc.B = wheelCollidable;

                ahcc.LocalHingeAxisA = Vector3.UnitZ;
                ahcc.LocalHingeAxisB = Vector3.UnitY;
                wheel.Add(ahcc);
            }

            //foreach (var wheel in DriveWheels.Union(BreakWheels))
            //{
            //    var wheelCollidable = wheel.Get<BodyComponent>();

            //    var aamcc = new AngularAxisMotorConstraintComponent();
            //    aamcc.Bodies.Add(wheelCollidable);
            //    aamcc.Bodies.Add(bodyCollidable);

            //    aamcc.LocalAxisA = Vector3.UnitY;
            //    aamcc.MotorDamping = 50;
            //    aamcc.Enabled = false;
            //    wheel.Add(aamcc);
            //}
            base.Start();
        }

        private readonly List<int> LastsRPMList = new() { 0 };
        public override void Update()
        {
            var WheelAverageRPM = GetWheelsAverageRPM();

            DebugText.Print($"Clutch:{Clutch}" + " | " +
                $"Starter:{Starter}" + " | " +
                $"SteeringAngle:{SteeringAngle}" + " | " +
                $"CurrentGear:{CurrentGear}" + " | " +
                $"EngineRunning:{EngineRunning}" + " | " +
                $"WheelAverageRPM:{WheelAverageRPM}" + " | " +
                $"AverageRPM:{(int)LastsRPMList.Average()}" + " | " +
                $"CurrentRPM:{CurrentRPM}" + " | " +
                $"", new(100, 1000));
        }

        public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {
            HandleGearing();
            HandleEngineStartingAndUpdate();
            HandleEngine();
            HandleSteering();

            LastsRPMList.Add(CurrentRPM);
            if (LastsRPMList.Count > 30)
                LastsRPMList.RemoveAt(0);
        }

        public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {

        }

        private void HandleGearing()
        {
            if (AutomaticGearing)
            {
                var deltaRPM = CarEngine.MaxRPM - CarEngine.MinRPM;

                var breaking = BreakKeys.Any(Input.IsKeyDown);
                var reversing = ReverseKeys.Any(Input.IsKeyDown);
                var accelerating = AccelerateKeys.Any(Input.IsKeyDown);

                if (CurrentRPM > CarEngine.MinRPM + GEAR_UP_VALUE * deltaRPM)
                {
                    var notInMaxGear = CurrentGear < CarEngine.Gears.Count() - 1;
                    if (CurrentGear > 0 && notInMaxGear)
                        CurrentGear++;
                    else if (CurrentGear == -1 && accelerating && !reversing)
                    {
                        CurrentGear = 1;
                        Clutch = false;
                        UnclutchFromStop();
                    }
                    else if (CurrentGear == -1 && reversing && !accelerating && GetWheelsAverageRPM() < 0.1f)
                    {
                        CurrentGear = 0;
                        Clutch = false;
                        UnclutchFromStop();
                    }
                }
                else if (CurrentRPM < CarEngine.MinRPM + GEAR_DOWN_VALUE * deltaRPM)
                {
                    if (CurrentGear > 1)
                        CurrentGear--;
                    else if (reversing && CurrentGear != 0 || breaking || accelerating && CurrentGear == 0)
                    {
                        CurrentGear = -1;
                        Clutch = true;
                    }
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
                float engineForce = (GetWheelsAverageRPM() > 0 ? 1 : -1) * (CurrentRPM > CarEngine.MinRPM ? -1 : 1) * CarEngine.EngineBreakForce;
                float brakeForce = 0f;

                var acc = AccelerateKeys.Any(Input.IsKeyDown);
                var dec = ReverseKeys.Any(Input.IsKeyDown);

                if (acc && !dec)
                {
                    if (CurrentGear == 0)
                    {
                        brakeForce = BreakingForce;
                    }
                    else if (CurrentGear != -1 && CurrentRPM < CarEngine.MaxRPM)
                    {
                        engineForce = CarEngine.Gears[CurrentGear].AccelerationForce;
                    }
                }
                if (dec && !acc)
                {
                    if (CurrentGear == 0 && CurrentRPM < CarEngine.MaxRPM)
                    {
                        engineForce = -CarEngine.Gears[CurrentGear].AccelerationForce;
                    }
                    else
                    {
                        brakeForce = BreakingForce;
                    }
                }

                if (BreakKeys.Any(Input.IsKeyDown))
                {
                    brakeForce = BreakingForce;
                }

                if (!Clutch)
                    DriveWheels.ForEach(e =>
                    {
                        var wheelBody = e.Get<BodyComponent>();
                        // do we want to get rid of the GetPhysicBody() method?
                        var rotationNormal = GetWheelRotationNormal(wheelBody);
                        wheelBody.ApplyAngularImpulse(rotationNormal * engineForce);
                        wheelBody.Awake = true;
                    });

                if (brakeForce != 0f)
                    BreakWheels.ForEach(e =>
                    {
                        var wheelBody = e.Get<BodyComponent>();

                        var rotationNormal = GetWheelRotationNormal(wheelBody);
                        var averageWheelRPM = GetWheelAverageRPM(wheelBody);

                        // Determine the direction of rotation
                        float rotationDirection = averageWheelRPM > 0 ? -1f : 1f;

                        // Calculate the braking force vector
                        var brakeVector = rotationDirection * rotationNormal * brakeForce;

                        // Adjust the braking force to avoid over-braking
                        var brakeVectorLen = brakeVector.Length();
                        if (brakeVectorLen > Math.Abs(averageWheelRPM) * 0.01f)
                        {
                            //brakeVector = brakeVector / brakeVectorLen * Math.Abs(averageWheelRPM) * 0.01f;
                            wheelBody.AngularVelocity = Vector3.Zero;
                        }
                        else
                        {
                            // Apply the braking force
                            wheelBody.ApplyAngularImpulse(brakeVector);
                        }
                        wheelBody.Awake = true;
                    });
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
            {
                var acc = AccelerateKeys.Any(Input.IsKeyDown);
                var rev = ReverseKeys.Any(Input.IsKeyDown);
                if ((acc || rev && CurrentGear == -1) && CurrentRPM < CarEngine.MaxRPM || CurrentRPM < CarEngine.MinRPM)
                {
                    if (CurrentRPM < CarEngine.MinRPM / 2)
                    {
                        CurrentRPM = 0;
                    }
                    else
                    {
                        CurrentRPM = (int)(CurrentRPM * 1.04f);
                        CurrentRPM = Math.Min(CurrentRPM, CarEngine.MaxRPM);
                    }
                }
                else if (CurrentRPM > CarEngine.MinRPM)
                {
                    CurrentRPM = (int)(CurrentRPM * 0.97f);
                    CurrentRPM = Math.Max(CurrentRPM, CarEngine.MinRPM);
                }
            }
            else
            {
                var engineMeanRPM = Math.Abs(GetWheelsAverageRPM() / CarEngine.Gears[CurrentGear].GearRatio);
                CurrentRPM = (int)engineMeanRPM;

                if (CurrentRPM < CarEngine.MinRPM / 2)
                {
                    CurrentRPM = 0;
                    if (AutomaticGearing)
                        CurrentGear = -1;
                }
            }
        }

        private void UnclutchFromStop()
        {
            DriveWheels.ForEach(e =>
            {
                var wheelBody = e.Get<BodyComponent>();

                var rotationNormal = GetWheelRotationNormal(wheelBody);
                wheelBody.AngularVelocity = CurrentRPM * CarEngine.Gears[CurrentGear].GearRatio * rotationNormal;
            });
        }

        private float GetWheelsAverageRPM() => DriveWheels.Select(e =>
        {
            var wheelBody = e.Get<BodyComponent>();

            return GetWheelAverageRPM(wheelBody);
        }).Average();
        private float GetWheelAverageRPM(BodyComponent e)
        {
            var rotationNormal = GetWheelRotationNormal(e);
            var angularVelocity = e.AngularVelocity;

            var dotProduct = Vector3.Dot(angularVelocity, rotationNormal);
            var result = dotProduct;

            return result;
        }
        private Vector3 GetWheelRotationNormal(BodyComponent e)
        {
            var unitVec = new Vector3(0, 1, 0);
            e.Orientation.Rotate(ref unitVec);
            return unitVec;
        }
    }
}

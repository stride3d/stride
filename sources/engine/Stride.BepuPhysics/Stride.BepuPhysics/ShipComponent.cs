// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

/// <summary>
/// Component for controlling a ship with atmospheric and space flight modes.
/// Supports 2D flight with vertical thrust, horizontal acceleration, and mode-dependent physics.
/// </summary>
[ComponentCategory("Physics - Bepu")]
public class ShipComponent : BodyComponent, ISimulationUpdate
{
    private float _currentSpeed;
    private bool _isHovering;

    /// <summary>
    /// Flight mode of the ship affecting gravity and drag behavior.
    /// </summary>
    public FlightMode Mode { get; set; } = FlightMode.Atmospheric;

    /// <summary>
    /// Maximum horizontal speed the ship can achieve in units per second.
    /// </summary>
    public float MaxSpeed { get; set; } = 50f;

    /// <summary>
    /// Rate at which the ship accelerates horizontally in units per second squared.
    /// </summary>
    public float Acceleration { get; set; } = 20f;

    /// <summary>
    /// Rate at which the ship decelerates when braking in units per second squared.
    /// </summary>
    public float BrakeDeceleration { get; set; } = 40f;

    /// <summary>
    /// Force applied for vertical thrust (W/S keys) in Newtons.
    /// </summary>
    public float VerticalThrustForce { get; set; } = 100f;

    /// <summary>
    /// Linear drag coefficient for atmospheric flight. Higher values = more drag.
    /// Drag force = LinearDragCoefficient * velocity.
    /// Only applied in Atmospheric mode.
    /// </summary>
    public float AtmosphericLinearDrag { get; set; } = 0.5f;

    /// <summary>
    /// Angular drag coefficient for atmospheric flight. Higher values = more rotational drag.
    /// Drag torque = AngularDragCoefficient * angularVelocity.
    /// Only applied in Atmospheric mode.
    /// </summary>
    public float AtmosphericAngularDrag { get; set; } = 0.3f;

    /// <summary>
    /// Linear drag coefficient for space flight. Lower values = less friction.
    /// Only applied in Space mode.
    /// </summary>
    public float SpaceLinearDrag { get; set; } = 0.05f;

    /// <summary>
    /// Angular drag coefficient for space flight. Lower values = less rotational friction.
    /// Only applied in Space mode.
    /// </summary>
    public float SpaceAngularDrag { get; set; } = 0.02f;

    /// <summary>
    /// When hovering in atmospheric mode, the ship will counteract gravity to maintain altitude.
    /// </summary>
    [DataMemberIgnore]
    public bool IsHovering
    {
        get => _isHovering;
        set
        {
            if (_isHovering == value)
                return;
            _isHovering = value;
            UpdateHoverState();
        }
    }

    /// <summary>
    /// Current horizontal speed of the ship (can be negative for reverse).
    /// </summary>
    [DataMemberIgnore]
    public float CurrentSpeed
    {
        get => _currentSpeed;
        private set => _currentSpeed = MathUtil.Clamp(value, -MaxSpeed, MaxSpeed);
    }

    /// <summary>
    /// Intended thrust direction: 1 = forward (D), -1 = reverse (A), 0 = no thrust.
    /// </summary>
    [DataMemberIgnore]
    public float ThrustDirection { get; set; }

    /// <summary>
    /// Whether braking (Space) is currently active.
    /// </summary>
    [DataMemberIgnore]
    public bool IsBraking { get; set; }

    /// <summary>
    /// Vertical thrust input: 1 = up (W), -1 = down (S), 0 = none.
    /// </summary>
    [DataMemberIgnore]
    public float VerticalThrust { get; set; }

    public ShipComponent()
    {
        InterpolationMode = InterpolationMode.Interpolated;
    }

    /// <inheritdoc cref="BodyComponent.AttachInner"/>
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        base.AttachInner(pose, shapeInertia, shapeIndex);
        UpdateFlightMode();
    }

    /// <summary>
    /// Applies forward thrust (D key).
    /// </summary>
    public void ThrustForward()
    {
        ThrustDirection = 1f;
        IsBraking = false;
    }

    /// <summary>
    /// Applies reverse thrust (A key).
    /// </summary>
    public void ThrustReverse()
    {
        ThrustDirection = -1f;
        IsBraking = false;
    }

    /// <summary>
    /// Stops horizontal thrust input.
    /// </summary>
    public void StopThrust()
    {
        ThrustDirection = 0f;
    }

    /// <summary>
    /// Activates braking (Space key).
    /// </summary>
    public void Brake()
    {
        IsBraking = true;
        ThrustDirection = 0f;
    }

    /// <summary>
    /// Releases brake.
    /// </summary>
    public void ReleaseBrake()
    {
        IsBraking = false;
    }

    /// <summary>
    /// Applies upward thrust (W key).
    /// </summary>
    public void ThrustUp()
    {
        VerticalThrust = 1f;
    }

    /// <summary>
    /// Applies downward thrust (S key).
    /// </summary>
    public void ThrustDown()
    {
        VerticalThrust = -1f;
    }

    /// <summary>
    /// Stops vertical thrust input.
    /// </summary>
    public void StopVerticalThrust()
    {
        VerticalThrust = 0f;
    }

    /// <summary>
    /// Switches between Atmospheric and Space flight modes.
    /// </summary>
    public void ToggleFlightMode()
    {
        Mode = Mode == FlightMode.Atmospheric ? FlightMode.Space : FlightMode.Atmospheric;
        UpdateFlightMode();
    }

    /// <summary>
    /// Called before physics simulation tick.
    /// </summary>
    public virtual void SimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        Awake = true; // Keep ship active

        // Handle horizontal acceleration/deceleration
        if (IsBraking)
        {
            ApplyBraking(simTimeStep);
        }
        else if (ThrustDirection != 0f)
        {
            CurrentSpeed += ThrustDirection * Acceleration * simTimeStep;
        }
        else
        {
            // Natural deceleration when no input
            ApplyNaturalDeceleration(simTimeStep);
        }

        // Apply horizontal velocity (assuming X is horizontal in 2D)
        var velocity = LinearVelocity;
        velocity.X = CurrentSpeed;

        // Handle vertical thrust
        if (Mode == FlightMode.Atmospheric)
        {
            if (VerticalThrust != 0f)
            {
                // Active vertical thrust
                ApplyLinearImpulse(new Vector3(0, VerticalThrust * VerticalThrustForce * simTimeStep, 0));
                IsHovering = false;
            }
            else if (IsHovering)
            {
                // Hover mode: counteract gravity
                var gravity = Simulation!.PoseGravity;
                var gravityMagnitude = gravity.Length();
                if (gravityMagnitude > 0f)
                {
                    // Apply upward force equal to gravity
                    var mass = 1f / BodyInertia.InverseMass;
                    var counterForce = -Vector3.Normalize(gravity) * gravityMagnitude * mass;
                    ApplyLinearImpulse(counterForce * simTimeStep);
                }
            }
        }
        else // Space mode
        {
            if (VerticalThrust != 0f)
            {
                ApplyLinearImpulse(new Vector3(0, VerticalThrust * VerticalThrustForce * simTimeStep, 0));
            }
        }

        // Apply drag to both linear and angular velocities
        ApplyDrag(simTimeStep);

        LinearVelocity = velocity;
    }

    /// <summary>
    /// Called after physics simulation tick.
    /// </summary>
    public virtual void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        // Update current speed from actual velocity
        _currentSpeed = LinearVelocity.X;
    }

    private void ApplyBraking(float deltaTime)
    {
        if (MathF.Abs(CurrentSpeed) < 0.01f)
        {
            CurrentSpeed = 0f;
            return;
        }

        var brakeAmount = BrakeDeceleration * deltaTime;
        if (CurrentSpeed > 0f)
            CurrentSpeed = MathF.Max(0f, CurrentSpeed - brakeAmount);
        else
            CurrentSpeed = MathF.Min(0f, CurrentSpeed + brakeAmount);
    }

    private void ApplyNaturalDeceleration(float deltaTime)
    {
        // Slower natural deceleration compared to active braking
        var linearDrag = Mode == FlightMode.Atmospheric ? AtmosphericLinearDrag : SpaceLinearDrag;
        var deceleration = CurrentSpeed * linearDrag * deltaTime;

        if (MathF.Abs(CurrentSpeed) < 0.01f)
        {
            CurrentSpeed = 0f;
            return;
        }

        if (CurrentSpeed > 0f)
            CurrentSpeed = MathF.Max(0f, CurrentSpeed - deceleration);
        else
            CurrentSpeed = MathF.Min(0f, CurrentSpeed + deceleration);
    }

    private void ApplyDrag(float deltaTime)
    {
        // Get drag coefficients based on flight mode
        var linearDrag = Mode == FlightMode.Atmospheric ? AtmosphericLinearDrag : SpaceLinearDrag;
        var angularDrag = Mode == FlightMode.Atmospheric ? AtmosphericAngularDrag : SpaceAngularDrag;

        // Apply linear drag: F_drag = -coefficient * velocity
        // Using exponential decay: v_new = v_old * (1 - drag * dt)
        // This is more stable than force-based approach
        var linearVelocity = LinearVelocity;
        var dragFactor = 1f - MathUtil.Clamp(linearDrag * deltaTime, 0f, 1f);
        
        // Apply drag to Y (vertical) velocity only, X is controlled separately
        linearVelocity.Y *= dragFactor;
        linearVelocity.Z *= dragFactor; // Apply to Z for consistency
        
        LinearVelocity = linearVelocity;

        // Apply angular drag: T_drag = -coefficient * angularVelocity
        var angularVelocity = AngularVelocity;
        var angularDragFactor = 1f - MathUtil.Clamp(angularDrag * deltaTime, 0f, 1f);
        angularVelocity *= angularDragFactor;
        
        AngularVelocity = angularVelocity;
    }

    private void UpdateFlightMode()
    {
        if (Simulation == null)
            return;

        switch (Mode)
        {
            case FlightMode.Atmospheric:
                Gravity = true;
                // Drag is now applied manually in ApplyDrag() using AtmosphericLinearDrag and AtmosphericAngularDrag
                break;

            case FlightMode.Space:
                Gravity = false;
                // Drag is now applied manually in ApplyDrag() using SpaceLinearDrag and SpaceAngularDrag
                IsHovering = false; // No hovering in space
                break;
        }
    }

    private void UpdateHoverState()
    {
        if (Mode == FlightMode.Space)
        {
            _isHovering = false; // Cannot hover in space
        }
    }
}

/// <summary>
/// Flight mode affecting physics behavior.
/// </summary>
public enum FlightMode
{
    /// <summary>
    /// Atmospheric flight with gravity and higher drag.
    /// Supports hovering to maintain altitude.
    /// </summary>
    Atmospheric,

    /// <summary>
    /// Space flight with no gravity and minimal drag.
    /// True Newtonian physics.
    /// </summary>
    Space
}

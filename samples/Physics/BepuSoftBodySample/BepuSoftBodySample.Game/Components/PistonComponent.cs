// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuSoftBodySample.Game.Components;

/// <summary>
/// Moves a kinematic <see cref="BodyComponent"/> on the same entity down and back up again, forever.
/// </summary>
/// <remarks>
/// The body is driven by its velocity rather than by its position, so it pushes on whatever it meets.
/// </remarks>
[ComponentCategory("BepuSoftBodyDemo")]
public class PistonComponent : StartupScript, ISimulationUpdate
{
    private BodyComponent? _body;
    private Vector3 _top;
    private float _time;

    /// <summary> How far down the piston goes, in meters </summary>
    public float Stroke { get; set; } = 2f;

    /// <summary> Seconds before the first stroke </summary>
    public float StartDelay { get; set; } = 1f;

    /// <summary> Seconds to go down, and as many to come back up </summary>
    public float TravelTime { get; set; } = 2f;

    /// <summary> Seconds spent at the bottom </summary>
    public float HoldTime { get; set; } = 1f;

    /// <summary> Seconds spent at the top between two strokes </summary>
    public float RestTime { get; set; } = 1.5f;

    public override void Start()
    {
        _body = Entity.Get<BodyComponent>();
        _top = Entity.Transform.Position;
    }

    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        if (_body is null || _body.Simulation is null)
            return;

        // Driven once per simulation step, so that it reaches where it should be whatever the frame rate
        _time += simTimeStep;
        var target = _top - new Vector3(0f, Depth(_time), 0f);
        _body.LinearVelocity = (target - _body.Position) / simTimeStep;
        _body.Awake = true;
    }

    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
    }

    private float Depth(float time)
    {
        if (time < StartDelay)
            return 0f;

        var t = (time - StartDelay) % (2f * TravelTime + HoldTime + RestTime);
        if (t < TravelTime)
            return Stroke * MathUtil.SmootherStep(t / TravelTime);
        if (t < TravelTime + HoldTime)
            return Stroke;
        if (t < 2f * TravelTime + HoldTime)
            return Stroke * (1f - MathUtil.SmootherStep((t - TravelTime - HoldTime) / TravelTime));
        return 0f;
    }
}

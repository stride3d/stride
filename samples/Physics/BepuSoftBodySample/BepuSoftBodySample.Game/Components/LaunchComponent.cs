// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics;
using Stride.BepuPhysics.Soft;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuSoftBodySample.Game.Components;

/// <summary>
/// Gives the rigid or soft body on the same entity a velocity once, after a delay; the body can be kept in place until then.
/// </summary>
[ComponentCategory("BepuSoftBodyDemo")]
public class LaunchComponent : SyncScript
{
    private float _time;
    private bool _launched;

    /// <summary> Seconds before the launch </summary>
    public float Delay { get; set; }

    /// <summary> The velocity given, in world space </summary>
    public Vector3 Velocity { get; set; }

    /// <summary> Whether the body floats in place until it is launched </summary>
    public bool HoldUntilLaunch { get; set; } = true;

    public override void Start()
    {
        SetGravity(HoldUntilLaunch == false);
    }

    public override void Update()
    {
        if (_launched)
            return;

        _time += (float)Game.UpdateTime.Elapsed.TotalSeconds;
        if (_time < Delay)
            return;

        _launched = true;
        SetGravity(true);
        if (Entity.Get<SoftBodyComponent>() is { } soft)
            soft.LinearVelocity = Velocity;
        else if (Entity.Get<BodyComponent>() is { } body)
            body.LinearVelocity = Velocity;
    }

    private void SetGravity(bool gravity)
    {
        if (Entity.Get<SoftBodyComponent>() is { } soft)
            soft.Gravity = gravity;
        else if (Entity.Get<BodyComponent>() is { } body)
            body.Gravity = gravity;
    }
}

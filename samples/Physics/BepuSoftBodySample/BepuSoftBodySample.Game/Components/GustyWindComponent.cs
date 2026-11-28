// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.BepuPhysics.Soft;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuSoftBodySample.Game.Components;

/// <summary>
/// Changes the <see cref="ClothComponent.Wind"/> of the cloth on the same entity over time, gusting and swinging around a main direction.
/// </summary>
[ComponentCategory("BepuSoftBodyDemo")]
public class GustyWindComponent : SyncScript
{
    private ClothComponent? _cloth;
    private float _time;

    /// <summary> The direction the wind mostly blows towards </summary>
    public Vector3 Direction { get; set; } = Vector3.UnitX;

    /// <summary> The average wind speed, in meters per second </summary>
    public float Speed { get; set; } = 7f;

    /// <summary> How much the speed varies around <see cref="Speed"/>, in meters per second </summary>
    public float Gusts { get; set; } = 3f;

    /// <summary> How far the direction swings sideways, as a fraction of <see cref="Direction"/> </summary>
    public float Swing { get; set; } = 0.35f;

    public override void Start()
    {
        _cloth = Entity.Get<ClothComponent>();
    }

    public override void Update()
    {
        if (_cloth is null)
            return;

        _time += (float)Game.UpdateTime.Elapsed.TotalSeconds;
        var side = Vector3.Normalize(Vector3.Cross(Direction, Vector3.UnitY));
        var direction = Vector3.Normalize(Vector3.Normalize(Direction) + side * Swing * MathF.Sin(_time * 0.7f));
        _cloth.Wind = direction * (Speed + Gusts * (MathF.Sin(_time * 1.3f) + 0.5f * MathF.Sin(_time * 4.1f)) / 1.5f);
    }
}

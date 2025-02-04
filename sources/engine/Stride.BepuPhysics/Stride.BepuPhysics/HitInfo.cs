// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NVector3 = System.Numerics.Vector3;
using SVector3 = Stride.Core.Mathematics.Vector3;

namespace Stride.BepuPhysics;

/// <summary>
/// Information returned by the different simulation test methods in <see cref="Stride.BepuPhysics.BepuSimulation"/>
/// </summary>
public readonly record struct HitInfo(SVector3 Point, SVector3 Normal, float Distance, CollidableComponent Collidable, int ChildIndex) : IComparable<HitInfo>
{
    /// <summary> The position where the intersection occured </summary>
    public SVector3 Point { get; init; } = Point;

    /// <summary> The direction of the surface hit </summary>
    public SVector3 Normal { get; init; } = Normal;

    /// <summary> The distance along the ray where the hit occured </summary>
    public float Distance { get; init; } = Distance;

    /// <summary> The collidable hit </summary>
    public CollidableComponent Collidable { get; init; } = Collidable;

    /// <summary> The specific child hit if the <see cref="Collidable"/>'s <see cref="CollidableComponent.Collider"/> is a <see cref="Definitions.Colliders.CompoundCollider"/> </summary>
    public int ChildIndex { get; init; } = ChildIndex;

    public HitInfo(NVector3 point, NVector3 normal, float distance, CollidableComponent collidable, int childIndex) : this(point.ToStride(), normal.ToStride(), distance, collidable, childIndex) { }

    public int CompareTo(HitInfo other)
    {
        return Distance.CompareTo(other.Distance);
    }
}

/// <summary>
/// Structure used through the different simulation test methods in <see cref="Stride.BepuPhysics.BepuSimulation"/>
/// </summary>
public readonly record struct HitInfoStack(CollidableStack Collidable, SVector3 Point, SVector3 Normal, float Distance, int ChildIndex);

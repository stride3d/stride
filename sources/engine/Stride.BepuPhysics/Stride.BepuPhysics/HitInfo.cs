using NVector3 = System.Numerics.Vector3;
using SVector3 = Stride.Core.Mathematics.Vector3;

namespace Stride.BepuPhysics;

/// <summary>
/// Information returned by the different simulation test methods in <see cref="Stride.BepuPhysics.BepuSimulation"/>
/// </summary>
public readonly record struct HitInfo(SVector3 Point, SVector3 Normal, float Distance, ContainerComponent Container) : IComparable<HitInfo>
{
    /// <summary> The position where the intersection occured </summary>
    public SVector3 Point { get; init; } = Point;

    /// <summary> The direction of the surface hit </summary>
    public SVector3 Normal { get; init; } = Normal;

    /// <summary> The distance along the ray where the hit occured </summary>
    public float Distance { get; init; } = Distance;

    /// <summary> The container hit </summary>
    public ContainerComponent Container { get; init; } = Container;

    public HitInfo(NVector3 point, NVector3 normal, float distance, ContainerComponent container) : this(point.ToStrideVector(), normal.ToStrideVector(), distance, container) { }

    public int CompareTo(HitInfo other)
    {
        return Distance.CompareTo(other.Distance);
    }
}

/// <summary>
/// Structure used through the different simulation test methods in <see cref="Stride.BepuPhysics.BepuSimulation"/>
/// </summary>
public readonly record struct HitInfoStack(CollidableStack Collidable, SVector3 Point, SVector3 Normal, float Distance);

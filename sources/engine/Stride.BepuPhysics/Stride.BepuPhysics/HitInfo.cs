using Stride.BepuPhysics.Definitions;
using NVector3 = System.Numerics.Vector3;
using SVector3 = Stride.Core.Mathematics.Vector3;

namespace Stride.BepuPhysics;

/// <summary>
/// Information returned by the different simulation test methods in <see cref="Stride.BepuPhysics.Configurations.BepuSimulation"/>
/// </summary>
/// <param name="Point">The position where the intersection occured</param>
/// <param name="Normal">The direction of the surface hit</param>
/// <param name="Distance">The distance along the ray where the hit occured</param>
/// <param name="Container">The container hit</param>
public readonly record struct HitInfo : IComparable<HitInfo>
{
    public SVector3 Point { get; init; }
    public SVector3 Normal { get; init; }
    public float Distance { get; init; }
    public ContainerComponent Container { get; init; }

    public HitInfo(NVector3 point, NVector3 normal, float distance, ContainerComponent container)
    {
        Point = point.ToStrideVector();
        Normal = normal.ToStrideVector();
        Distance = distance;
        Container = container;
    }

    public int CompareTo(HitInfo other)
    {
        return Distance.CompareTo(other.Distance);
    }
}
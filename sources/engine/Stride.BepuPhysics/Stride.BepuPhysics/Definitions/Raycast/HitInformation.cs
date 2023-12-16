using System.Numerics;
using Stride.BepuPhysics.Components.Containers;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    /// <summary>
    /// Information returned by the different simulation test methods in <see cref="Stride.BepuPhysics.Configurations.BepuSimulation"/>
    /// </summary>
    /// <param name="Point">The position where the intersection occured</param>
    /// <param name="Normal">The direction of the surface hit</param>
    /// <param name="Distance">The distance along the ray where the hit occured</param>
    /// <param name="Container">The container hit</param>
    public readonly record struct HitInfo(Vector3 Point, Vector3 Normal, float Distance, ContainerComponent Container) : IComparable<HitInfo>
    {
        public int CompareTo(HitInfo other)
        {
            return Distance.CompareTo(other.Distance);
        }
    }
}

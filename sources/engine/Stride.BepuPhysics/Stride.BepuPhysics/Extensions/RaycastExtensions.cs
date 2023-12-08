using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Raycast;
using Stride.Engine;

namespace Stride.BepuPhysics.Extensions;
public static class RaycastExtensions
{

    /// <summary>
    /// A simple (untested) way to get the entity from a raycast result.
    /// </summary>
    /// <param name="rayCastResult"></param>
    /// <param name="simulation"></param>
    /// <returns></returns>
    public static Entity? GetEntityComponents(this HitInformation rayCastResult, BepuSimulation simulation)
    {
        if (rayCastResult.Collidable == null)
            return null;

        if (simulation.BodiesContainers.TryGetValue(rayCastResult.Collidable.Value.BodyHandle, out var bodyContainer))
            return bodyContainer.Entity;

        if (simulation.StaticsContainers.TryGetValue(rayCastResult.Collidable.Value.StaticHandle, out var staticContainer))
            return staticContainer.Entity;

        return null;
    }
}

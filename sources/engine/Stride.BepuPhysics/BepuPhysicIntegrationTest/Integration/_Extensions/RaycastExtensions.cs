using BepuPhysicIntegrationTest.Integration.Configurations;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration._Extensions;
public static class RaycastExtensions
{

	/// <summary>
	/// A simple (untested) way to get the entity from a raycast result.
	/// </summary>
	/// <param name="rayCastResult"></param>
	/// <param name="simulation"></param>
	/// <returns></returns>
	public static Entity? GetEntityComponents(this HitResult rayCastResult, BepuSimulation simulation)
	{
		if (simulation.BodiesContainers.TryGetValue(rayCastResult.Collidable.Value.BodyHandle, out var bodyContainer))
			return bodyContainer.Entity;

		if (simulation.StaticsContainers.TryGetValue(rayCastResult.Collidable.Value.StaticHandle, out var staticContainer))
			return staticContainer.Entity;

		return null;
	}
}

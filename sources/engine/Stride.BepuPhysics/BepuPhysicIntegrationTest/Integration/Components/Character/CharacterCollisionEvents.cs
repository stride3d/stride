using BepuPhysicIntegrationTest.Integration.Components.Collisions;
using BepuPhysics.Collidables;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using Stride.Particles;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BepuPhysicIntegrationTest.Integration.Components.Character;
public class CharacterCollisionEvents : IContactEventHandler
{
	public Simulation Simulation { get; set; }

	public bool Contact { get; private set; } = false;

	public Dictionary<CollidablePair, Vector3> ContactPoints { get; } = new();

	void IContactEventHandler.OnPairCreated<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int workerIndex)
	{
		Console.WriteLine("pc");
	}

	void IContactEventHandler.OnPairEnded(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair)
	{
		Console.WriteLine("pe");
		// this will be slow for use in a dictionary but shouldnt happen often soooooo
		if (ContactPoints.ContainsKey(pair))
			ContactPoints.Remove(pair);
	}

	void IContactEventHandler.OnStartedTouching<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int workerIndex)
	{
		Contact = true;
		Console.WriteLine("stot");
	}

	void IContactEventHandler.OnStoppedTouching<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int workerIndex)
	{
		Contact = false;
		Console.WriteLine("stat");
		// this will be slow for use in a dictionary but shouldnt happen often soooooo
		if (ContactPoints.ContainsKey(pair))
			ContactPoints.Remove(pair);
	}

	void IContactEventHandler.OnContactAdded<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, System.Numerics.Vector3 contactOffset, System.Numerics.Vector3 contactNormal, float depth, int featureId, int contactIndex, int workerIndex)
	{
		Console.WriteLine("ca");
		var contactpos = contactOffset + (pair.A.Mobility == CollidableMobility.Static ?
			new StaticReference(pair.A.StaticHandle, Simulation.Statics).Pose.Position :
			new BodyReference(pair.A.BodyHandle, Simulation.Bodies).Pose.Position);

		if (ContactPoints.ContainsKey(pair))
		{
			ContactPoints[pair] = contactpos;
			return;
		}

		ContactPoints.Add(pair, contactpos);
	}

	void IContactEventHandler.OnContactRemoved<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int removedFeatureId, int workerIndex)
	{
		Console.WriteLine("cr");
		// this will be slow for use in a dictionary but shouldnt happen often soooooo
		if(ContactPoints.ContainsKey(pair))
			ContactPoints.Remove(pair);
	}

	void IContactEventHandler.OnTouching<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, System.Numerics.Vector3 contactOffset, ref TManifold contactManifold, int workerIndex)
	{
		var contactpos = contactOffset + (pair.A.Mobility == CollidableMobility.Static ?
			new StaticReference(pair.A.StaticHandle, Simulation.Statics).Pose.Position :
			new BodyReference(pair.A.BodyHandle, Simulation.Bodies).Pose.Position);

		if (ContactPoints.ContainsKey(pair))
		{
			ContactPoints[pair] = contactpos;
			return;
		}

		ContactPoints.Add(pair, contactpos);
	}
}

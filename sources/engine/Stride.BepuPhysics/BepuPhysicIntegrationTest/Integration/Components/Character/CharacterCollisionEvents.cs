using BepuPhysicIntegrationTest.Integration.Components.Collisions;
using BepuPhysics.Collidables;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using Stride.Particles;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace BepuPhysicIntegrationTest.Integration.Components.Character;
public class CharacterCollisionEvents : IContactEventHandler
{
    public Simulation Simulation { get; set; }
    public List<Vector3> ContactPoints { get; } = new();

    void IContactEventHandler.OnStoppedTouching<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex)
    {
        ContactPoints.Clear();
    }
    void IContactEventHandler.OnTouching<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex)
    {
        if (contactIndex == 0)
            ContactPoints.Clear();

        var worldPoint = contactManifold.GetOffset(contactIndex) + (pair.A.Mobility == CollidableMobility.Static ? new StaticReference(pair.A.StaticHandle, Simulation.Statics).Pose.Position : new BodyReference(pair.A.BodyHandle, Simulation.Bodies).Pose.Position);
        ContactPoints.Add(worldPoint);
    }
}

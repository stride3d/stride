using BepuPhysics.Collidables;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using System.Numerics;
using Stride.BepuPhysics.Definitions.Collisions;

namespace Stride.BepuPhysics.Definitions.Character;
public class CharacterContactEventHandler : IContactEventHandler
{
    private Simulation _simulation;
    public List<Vector3> ContactPoints { get; } = new();

    public CharacterContactEventHandler(Simulation Simulation)
    {
        _simulation = Simulation;
    }

    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex)
    {
        ContactPoints.Clear();
    }
    void IContactEventHandler.OnTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex)
    {
        if (contactIndex == 0)
            ContactPoints.Clear();

        //var worldPoint = contactManifold.GetOffset(contactIndex) + (pair.A.Mobility == CollidableMobility.Static ? _simulation.Statics[pair.A.StaticHandle].Pose.Position : _simulation.Bodies[pair.A.BodyHandle].Pose.Position);
        ContactPoints.Add(contactManifold.GetOffset(contactIndex));
    }
}

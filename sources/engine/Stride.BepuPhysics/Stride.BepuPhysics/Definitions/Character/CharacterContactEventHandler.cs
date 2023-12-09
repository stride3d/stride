using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components.Character;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions.Collisions;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Definitions.Character;
public class CharacterContactEventHandler : IContactEventHandler
{
    private CharacterComponent _characterComponent;
    public List<Vector3> ContactPoints { get; } = new();

    public CharacterContactEventHandler(CharacterComponent characterComponent)
    {
        _characterComponent = characterComponent;
    }

    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex)
    {
        ContactPoints.Clear();
    }
    void IContactEventHandler.OnTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex)
    {
        if (contactIndex == 0)
            ContactPoints.Clear();

        var container = GetContainerFromCollidable(pair.A);
        if (container == null)
            ContactPoints.Add(contactManifold.GetOffset(contactIndex));
        else
            ContactPoints.Add(contactManifold.GetOffset(contactIndex) + container.Entity.Transform.GetWorldPos().ToNumericVector() + container.CenterOfMass.ToNumericVector());
    }
    private ContainerComponent? GetContainerFromCollidable(CollidableReference collidable)
    {
        ContainerComponent? container = null;
        var _sim = _characterComponent.BepuSimulation;
        if (_sim != null)
        {
            if (collidable.Mobility == CollidableMobility.Static && _sim.StaticsContainers.ContainsKey(collidable.StaticHandle))
            {
                container = _sim.StaticsContainers[collidable.StaticHandle];
            }
            else if (collidable.Mobility != CollidableMobility.Static && _sim.BodiesContainers.ContainsKey(collidable.BodyHandle))
            {
                container = _sim.BodiesContainers[collidable.BodyHandle];
            }
        }
        return container;
    }

}

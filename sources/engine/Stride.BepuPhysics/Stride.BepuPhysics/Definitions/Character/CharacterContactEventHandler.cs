using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components.Character;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Collisions;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Extensions;

namespace Stride.BepuPhysics.Definitions.Character;
public class CharacterContactEventHandler : IContactEventHandler
{
    private readonly CharacterComponent _characterComponent;

    /// <summary> Order is not guaranteed and may change at any moment </summary>
    public List<(ContainerComponent Source, Contact Contact)> Contacts { get; } = new();

    public CharacterContactEventHandler(CharacterComponent characterComponent)
    {
        _characterComponent = characterComponent;
    }

    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
    {
        var sim = _characterComponent.BepuSimulation;
        if (sim == null)
        {
            Contacts.Clear();
            return;
        }

        var containerA = GetContainerFromCollidable(pair.A, sim);
        var containerB = GetContainerFromCollidable(pair.B, sim);
        var otherContainer = _characterComponent.CharacterBody == containerA ? containerB : containerA;
        for (int i = Contacts.Count - 1; i >= 0; i--)
        {
            if (Contacts[i].Source == otherContainer)
                Contacts.SwapRemoveAt(i);
        }
    }

    void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
    {
        var sim = _characterComponent.BepuSimulation;
        if (sim == null)
            throw new InvalidOperationException("Received new contacts in a container that's not part of the simulation");

        var containerA = GetContainerFromCollidable(pair.A, sim);
        var containerB = GetContainerFromCollidable(pair.B, sim);
        var otherContainer = _characterComponent.CharacterBody == containerA ? containerB : containerA;

        contactManifold.GetContact(contactIndex, out var contact);
#warning likely need to transform contact from local to world instead of this 
        //Nicogo : It is a world pos, Offset is a "worldPosOffset" from the world pos of Pair.A
        contact.Offset = contact.Offset + containerA.Entity.Transform.GetWorldPos().ToNumericVector() + containerA.CenterOfMass.ToNumericVector();
        Contacts.Add((otherContainer, contact));
    }

    private ContainerComponent GetContainerFromCollidable(CollidableReference collidable, BepuSimulation sim)
    {
        if (collidable.Mobility == CollidableMobility.Static && sim.StaticsContainers.TryGetValue(collidable.StaticHandle, out StaticContainerComponent? staticsContainer))
        {
            return staticsContainer;
        }
        else if (collidable.Mobility != CollidableMobility.Static && sim.BodiesContainers.TryGetValue(collidable.BodyHandle, out BodyContainerComponent? bodiesContainer))
        {
            return bodiesContainer;
        }
        throw new InvalidOperationException("Received new contacts with a container that's not part of the simulation");
    }

}

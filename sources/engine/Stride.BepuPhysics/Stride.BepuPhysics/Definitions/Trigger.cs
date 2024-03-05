using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions;

public delegate void TriggerDelegate(CollidableComponent @this, CollidableComponent other);

/// <summary>
/// A contact event handler without collision response, which runs delegates on enter and exit
/// </summary>
[DataContract]
public class Trigger : IContactEventHandler
{
    public bool NoContactResponse => true;
    public event TriggerDelegate? OnEnter, OnLeave;

    void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation)
    {
        OnEnter?.Invoke(bepuSimulation.GetComponent(eventSource), bepuSimulation.GetComponent(other));
    }
    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation)
    {
        OnLeave?.Invoke(bepuSimulation.GetComponent(eventSource), bepuSimulation.GetComponent(other));
    }
}
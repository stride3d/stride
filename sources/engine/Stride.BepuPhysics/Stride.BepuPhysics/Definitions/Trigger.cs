// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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

    void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation) => OnStartedTouching(eventSource, other, ref contactManifold, contactIndex, bepuSimulation);
    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation) => OnStoppedTouching(eventSource, other, ref contactManifold, contactIndex, bepuSimulation);

    protected void OnStartedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation)
    {
        OnEnter?.Invoke(bepuSimulation.GetComponent(eventSource), bepuSimulation.GetComponent(other));
    }

    protected void OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation)
    {
        OnLeave?.Invoke(bepuSimulation.GetComponent(eventSource), bepuSimulation.GetComponent(other));
    }
}

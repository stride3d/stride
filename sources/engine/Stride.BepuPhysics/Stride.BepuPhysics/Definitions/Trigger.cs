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

    void IContactEventHandler.OnStartedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int contactIndex, BepuSimulation bepuSimulation) => OnStartedTouching(eventSource, other, ref contactManifold, flippedManifold, contactIndex, bepuSimulation);
    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int contactIndex, BepuSimulation bepuSimulation) => OnStoppedTouching(eventSource, other, ref contactManifold, flippedManifold, contactIndex, bepuSimulation);

    /// <inheritdoc cref="IContactEventHandler.OnStartedTouching{TManifold}"/>
    protected void OnStartedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int contactIndex, BepuSimulation bepuSimulation)
    {
        OnEnter?.Invoke(eventSource, other);
    }

    /// <inheritdoc cref="IContactEventHandler.OnStoppedTouching{TManifold}"/>
    protected void OnStoppedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int contactIndex, BepuSimulation bepuSimulation)
    {
        OnLeave?.Invoke(eventSource, other);
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using BepuPhysics.CollisionDetection;

namespace Stride.BepuPhysics.Definitions.Contacts;

/// <summary>
/// Implements handlers for various collision events.
/// </summary>
[Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}, update your contact methods when migrating to this new class")]
public interface IContactEventHandler : IContactHandler
{
    /// <summary>
    /// Whether the object this is attached to should let colliders pass through it
    /// </summary>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}, this property will never be called")]
    public new bool NoContactResponse { get; }

    /// <summary>
    /// Fires when a contact is added.
    /// </summary>
    /// <remarks>
    /// This may be called before <see cref="OnStartedTouching{TManifold}"/>,
    /// contacts are registered when two collidables are close enough, not necessarily when actually touching.
    /// </remarks>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="flippedManifold">Whether the manifold's normals and offset is flipped from the source's point of view.</param>
    /// <param name="contactIndex">Index of the new contact in the contact manifold.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}, this method will never be called", true)]
    void OnContactAdded<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int contactIndex, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires when a contact is removed.
    /// </summary>
    /// <remarks>
    /// This may be called without a corresponding call to <see cref="OnStoppedTouching{TManifold}"/>,
    /// contacts are registered when two collidables are close enough, not necessarily when actually touching.
    /// If the two collidables grazed each other, none of the touching methods will be called.
    /// </remarks>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="flippedManifold">Whether the manifold's normals and offset is flipped from the source's point of view.</param>
    /// <param name="removedFeatureId">Feature id of the contact that was removed and is no longer present in the contact manifold.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}, this method will never be called", true)]
    void OnContactRemoved<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int removedFeatureId, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires the first time a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="flippedManifold">Whether the manifold's normals and offset is flipped from the source's point of view.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}")]
    void OnStartedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires whenever a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold. Will not fire for sleeping pairs.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="flippedManifold">Whether the manifold's normals and offset is flipped from the source's point of view.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}")]
    void OnTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }


    /// <summary>
    /// Fires when a pair stops touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="flippedManifold">Whether the manifold's normals and offset is flipped from the source's point of view.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}")]
    void OnStoppedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }


    /// <summary>
    /// Fires when a pair is observed for the first time.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="flippedManifold">Whether the manifold's normals and offset is flipped from the source's point of view.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}, this method will never be called", true)]
    void OnPairCreated<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires whenever a pair is updated. Will not fire for sleeping pairs.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="flippedManifold">Whether the manifold's normals and offset is flipped from the source's point of view.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}, this method will never be called", true)]
    void OnPairUpdated<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires when a pair ends.
    /// </summary>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    [Obsolete($"{nameof(IContactEventHandler)} as been superseded by {nameof(IContactHandler)}, this method will never be called", true)]
    void OnPairEnded(CollidableComponent eventSource, CollidableComponent other, BepuSimulation bepuSimulation)
    {
    }

    bool IContactHandler.NoContactResponse => NoContactResponse;

    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        foreach (var contact in contacts)
        {
            var manifold = contact.ContactGroup.Manifold;
            OnStartedTouching(contacts.EventSource, contacts.Other, ref manifold, contacts.IsSourceOriginalA == false, 0, contacts.Simulation);
        }
    }

    void IContactHandler.OnTouching<TManifold>(Contacts<TManifold> contacts)
    {
        foreach (var contact in contacts)
        {
            var manifold = contact.ContactGroup.Manifold;
            OnTouching(contacts.EventSource, contacts.Other, ref manifold, contacts.IsSourceOriginalA == false, 0, contacts.Simulation);
        }
    }

    void IContactHandler.OnStoppedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        foreach (var contact in contacts)
        {
            var manifold = contact.ContactGroup.Manifold;
            OnStoppedTouching(contacts.EventSource, contacts.Other, ref manifold, contacts.IsSourceOriginalA == false, 0, contacts.Simulation);
        }
    }
}

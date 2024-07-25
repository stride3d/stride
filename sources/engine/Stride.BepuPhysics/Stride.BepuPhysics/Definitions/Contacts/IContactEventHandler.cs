// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;

namespace Stride.BepuPhysics.Definitions.Contacts;

/// <summary>
/// Implements handlers for various collision events.
/// </summary>
public interface IContactEventHandler
{
    /// <summary>
    /// Whether the object this is attached to should let colliders pass through it
    /// </summary>
    public bool NoContactResponse { get; }

    /// <summary>
    /// Fires when a contact is added.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="contactIndex">Index of the new contact in the contact manifold.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnContactAdded<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires when a contact is removed.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="removedFeatureId">Feature id of the contact that was removed and is no longer present in the contact manifold.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnContactRemoved<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int removedFeatureId, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires the first time a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnStartedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires whenever a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold. Will not fire for sleeping pairs.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }


    /// <summary>
    /// Fires when a pair stops touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }


    /// <summary>
    /// Fires when a pair is observed for the first time.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnPairCreated<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires whenever a pair is updated. Will not fire for sleeping pairs.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnPairUpdated<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires when a pair ends.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    void OnPairEnded(CollidableReference eventSource, CollidableReference other, BepuSimulation bepuSimulation)
    {
    }
}
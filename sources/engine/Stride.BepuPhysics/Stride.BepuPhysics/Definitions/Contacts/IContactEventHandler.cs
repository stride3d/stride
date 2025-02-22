// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
    void OnPairUpdated<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires when a pair ends.
    /// </summary>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="other">Other collider <paramref name="eventSource"/> collided with.</param>
    /// <param name="bepuSimulation">The simulation where the contact occured.</param>
    void OnPairEnded(CollidableComponent eventSource, CollidableComponent other, BepuSimulation bepuSimulation)
    {
    }
}

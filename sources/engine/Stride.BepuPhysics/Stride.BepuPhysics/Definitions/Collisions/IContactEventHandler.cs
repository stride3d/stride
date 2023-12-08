using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Stride.BepuPhysics.Definitions.Collisions;
/// <summary>
/// Implements handlers for various collision events.
/// </summary>
public interface IContactEventHandler
{
    /// <summary>
    /// Fires when a contact is added.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="contactOffset">Offset from the pair's local origin to the new contact.</param>
    /// <param name="contactNormal">Normal of the new contact.</param>
    /// <param name="depth">Depth of the new contact.</param>
    /// <param name="featureId">Feature id of the new contact.</param>
    /// <param name="contactIndex">Index of the new contact in the contact manifold.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnContactAdded<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires when a contact is removed.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="removedFeatureId">Feature id of the contact that was removed and is no longer present in the contact manifold.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnContactRemoved<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int removedFeatureId, int contactIndex, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires the first time a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires whenever a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold. Will not fire for sleeping pairs.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }


    /// <summary>
    /// Fires when a pair stops touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }


    /// <summary>
    /// Fires when a pair is observed for the first time.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnPairCreated<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires whenever a pair is updated. Will not fire for sleeping pairs.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    /// <param name="contactManifold">Set of remaining contacts in the collision.</param>
    /// <param name="workerIndex">Index of the worker thread that fired this event.</param>
    void OnPairUpdated<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires when a pair ends.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="eventSource">Collidable that the event was attached to.</param>
    /// <param name="pair">Collidable pair triggering the event.</param>
    void OnPairEnded(CollidableReference eventSource, CollidablePair pair)
    {
    }
}
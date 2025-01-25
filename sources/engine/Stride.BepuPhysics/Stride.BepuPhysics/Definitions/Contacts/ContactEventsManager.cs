// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;

namespace Stride.BepuPhysics.Definitions.Contacts;

/// <summary>
/// Watches a set of bodies and statics for contact changes and reports events.
/// </summary>
internal class ContactEventsManager : IDisposable
{
    private readonly Dictionary<OrderedPair, LastCollisionState> _trackedCollisions = new();
    private readonly BufferPool _pool;
    private readonly BepuSimulation _simulation;
    private IndexSet _staticListenerFlags;
    private IndexSet _bodyListenerFlags;
    private int _flushes;

    public ContactEventsManager(BufferPool pool, BepuSimulation simulation)
    {
        _pool = pool;
        _simulation = simulation;
    }

    public void Initialize()
    {
        //simulation.Simulation.Timestepper.BeforeCollisionDetection += SetFreshnessForCurrentActivityStatus;
    }

    public void Dispose()
    {
        //_simulation.Simulation.Timestepper.BeforeCollisionDetection -= SetFreshnessForCurrentActivityStatus;
        if (_bodyListenerFlags.Flags.Allocated)
            _bodyListenerFlags.Dispose(_pool);
        if (_staticListenerFlags.Flags.Allocated)
            _staticListenerFlags.Dispose(_pool);
    }

    /// <summary>
    /// Begins listening for events related to the given body.
    /// </summary>
    /// <param name="body">Body to monitor for events.</param>
    public void Register(BodyHandle body)
    {
        Register(_simulation.Simulation.Bodies[body].CollidableReference);
    }

    /// <summary>
    /// Begins listening for events related to the given static.
    /// </summary>
    /// <param name="staticHandle">Static to monitor for events.</param>
    public void Register(StaticHandle staticHandle)
    {
        Register(new CollidableReference(staticHandle));
    }

    /// <summary>
    /// Begins listening for events related to the given collidable.
    /// </summary>
    public void Register(CollidableReference collidable)
    {
        if (collidable.Mobility == CollidableMobility.Static)
            _staticListenerFlags.Add(collidable.RawHandleValue, _pool);
        else
            _bodyListenerFlags.Add(collidable.RawHandleValue, _pool);
    }

    /// <summary>
    /// Stops listening for events related to the given body.
    /// </summary>
    /// <param name="body">Body to stop listening for.</param>
    public void Unregister(BodyHandle body)
    {
        Unregister(_simulation.Simulation.Bodies[body].CollidableReference);
    }

    /// <summary>
    /// Stops listening for events related to the given static.
    /// </summary>
    /// <param name="staticHandle">Static to stop listening for.</param>
    public void Unregister(StaticHandle staticHandle)
    {
        Unregister(new CollidableReference(staticHandle));
    }

    /// <summary>
    /// Stops listening for events related to the given collidable.
    /// </summary>
    public void Unregister(CollidableReference collidable)
    {
        if (collidable.Mobility == CollidableMobility.Static)
            _staticListenerFlags.Remove(collidable.RawHandleValue);
        else
            _bodyListenerFlags.Remove(collidable.RawHandleValue);

        ClearCollisionsOf(_simulation.GetComponent(collidable));
    }

    /// <summary>
    /// Checks if a collidable is registered as a listener.
    /// </summary>
    public bool IsRegistered(BodyHandle body)
    {
        return IsRegistered(_simulation.Simulation.Bodies[body].CollidableReference);
    }
    /// <summary>
    /// Checks if a collidable is registered as a listener.
    /// </summary>
    public bool IsRegistered(StaticHandle staticHandle)
    {
        return IsRegistered(_simulation.Simulation.Statics[staticHandle].CollidableReference);
    }

    /// <summary>
    /// Checks if a collidable is registered as a listener.
    /// </summary>
    public bool IsRegistered(CollidableReference collidable)
    {
        if (collidable.Mobility == CollidableMobility.Static)
            return _staticListenerFlags.Contains(collidable.RawHandleValue);
        else
            return _bodyListenerFlags.Contains(collidable.RawHandleValue);
    }

    public unsafe void ClearCollisionsOf(CollidableComponent collidable)
    {
#error Handle handler exceptions
        // Really slow, but improving performance has a huge amount of gotchas since user code
        // may cause this method to be re-entrant through handler calls.
        // Something to investigate later

        int workerIndex = 0;
        var manifold = new EmptyManifold();
        bool flippedManifold = false; // The flipped manifold argument does not make sense in this context given that we pass an empty one

        foreach (var (pair, state) in _trackedCollisions)
        {
            if (!ReferenceEquals(pair.A, collidable) && !ReferenceEquals(pair.B, collidable))
                continue;

#if DEBUG
            ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, pair, out _);
            _trackedCollisions.Remove(pair);
            System.Diagnostics.Debug.Assert(stateRef.Alive == false); // Notify HandleManifoldInner higher up the call stack that the manifold they are processing is dead
#else
            _trackedCollisions.Remove(pair);
#endif

            for (int i = 0; i < state.ContactCount; i++)
            {
                state.HandlerA?.OnContactRemoved(pair.A, pair.B, ref manifold, flippedManifold, state.FeatureId[i], workerIndex, _simulation);
                state.HandlerB?.OnContactRemoved(pair.B, pair.A, ref manifold, flippedManifold, state.FeatureId[i], workerIndex, _simulation);
            }

            if (state.Touching)
            {
                state.HandlerA?.OnStoppedTouching(pair.A, pair.B, ref manifold, flippedManifold, workerIndex, _simulation);
                state.HandlerB?.OnStoppedTouching(pair.B, pair.A, ref manifold, flippedManifold, workerIndex, _simulation);
            }

            state.HandlerA?.OnPairEnded(pair.A, pair.B, _simulation);
            state.HandlerB?.OnPairEnded(pair.B, pair.A, _simulation);
        }
    }

    public void HandleManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        bool aListener = IsRegistered(pair.A);
        bool bListener = IsRegistered(pair.B);
        if (aListener == false && bListener == false)
            return;

        HandleManifoldInner(workerIndex, _simulation.GetComponent(pair.A), _simulation.GetComponent(pair.A), ref manifold);
    }

    private unsafe void HandleManifoldInner<TManifold>(int workerIndex, CollidableComponent a, CollidableComponent b, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
#error Handle handler exceptions
        ref var collisionState = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, new OrderedPair(a, b), out bool alreadyExisted);

        var handlerA = a.ContactEventHandler;
        var handlerB = b.ContactEventHandler;

        if (alreadyExisted)
        {
            bool previouslyTouching = collisionState.Touching;
            for (int contactIndex = 0; contactIndex < manifold.Count; ++contactIndex)
            {
                if (manifold.GetDepth(contactIndex) < 0)
                    continue;

                if (collisionState.Touching == false)
                {
                    collisionState.Touching = true;
                    handlerA?.OnStartedTouching(a, b, ref manifold, false, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                    handlerB?.OnStartedTouching(b, a, ref manifold, true, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }

                handlerA?.OnTouching(a, b, ref manifold, false, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                handlerB?.OnTouching(b, a, ref manifold, true, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                break;
            }

            int previousContactsStillExist = 0; // Bitmask to mark contacts that are both inside the previous and current manifold
            for (int contactIndex = 0; contactIndex < manifold.Count; ++contactIndex)
            {
                //We can check if each contact was already present in the previous frame by looking at contact feature ids. See the 'PreviousCollision' type for a little more info on FeatureIds.
                var featureId = manifold.GetFeatureId(contactIndex);
                var featureIdWasInPreviousCollision = false;
                for (int previousContactIndex = 0; previousContactIndex < collisionState.ContactCount; ++previousContactIndex)
                {
                    if (featureId == collisionState.FeatureId[previousContactIndex])
                    {
                        featureIdWasInPreviousCollision = true;
                        previousContactsStillExist |= 1 << previousContactIndex;
                        break;
                    }
                }

                if (!featureIdWasInPreviousCollision)
                {
                    handlerA?.OnContactAdded(a, b, ref manifold, false, contactIndex, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                    handlerB?.OnContactAdded(b, a, ref manifold, true, contactIndex, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }
            }

            if (previousContactsStillExist != (1 << collisionState.ContactCount) - 1) //At least one contact that used to exist no longer does.
            {
                for (int previousContactIndex = 0; previousContactIndex < collisionState.ContactCount; ++previousContactIndex)
                {
                    if ((previousContactsStillExist & (1 << previousContactIndex)) != 0)
                        continue;

                    int id = collisionState.FeatureId[previousContactIndex];
                    handlerA?.OnContactRemoved(a, b, ref manifold, false, id, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                    handlerB?.OnContactRemoved(b, a, ref manifold, true, id, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }
            }

            if (previouslyTouching && collisionState.Touching == false)
            {
                handlerA?.OnStoppedTouching(a, b, ref manifold, false, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;

                handlerB?.OnStoppedTouching(b, a, ref manifold, true, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }
        }
        else
        {
            collisionState.Alive = true; // This is set as a flag to check for removal events
            collisionState.HandlerA = handlerA;
            collisionState.HandlerB = handlerB;


            handlerA?.OnPairCreated(a, b, ref manifold, false, workerIndex, _simulation);
            if (collisionState.Alive == false)
                return;
            handlerB?.OnPairCreated(b, a, ref manifold, true, workerIndex, _simulation);
            if (collisionState.Alive == false)
                return;

            for (int i = 0; i < manifold.Count; ++i)
            {
                if (manifold.GetDepth(i) < 0)
                    continue;

                collisionState.Touching = true;

                handlerA?.OnStartedTouching(a, b, ref manifold, false, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                handlerB?.OnStartedTouching(b, a, ref manifold, true, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;

                handlerA?.OnTouching(a, b, ref manifold, false, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                handlerB?.OnTouching(b, a, ref manifold, true, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                break;
            }

            for (int i = 0; i < manifold.Count; ++i)
            {
                handlerA?.OnContactAdded(a, b, ref manifold, false, i, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                handlerB?.OnContactAdded(b, a, ref manifold, true, i, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }
        }

        handlerA?.OnPairUpdated(a, b, ref manifold, false, workerIndex, _simulation);
        if (collisionState.Alive == false)
            return;
        handlerB?.OnPairUpdated(b, a, ref manifold, true, workerIndex, _simulation);
        if (collisionState.Alive == false)
            return;

        System.Diagnostics.Debug.Assert(manifold.Count <= LastCollisionState.FeatureCount, "This was built on the assumption that nonconvex manifolds will have a maximum of 4 contacts, but that might have changed.");
        //If the above assert gets hit because of a change to nonconvex manifold capacities, the packed feature id representation this uses will need to be updated.
        //I very much doubt the nonconvex manifold will ever use more than 8 contacts, so addressing this wouldn't require much of a change.
        for (int j = 0; j < manifold.Count; ++j)
            collisionState.FeatureId[j] = manifold.GetFeatureId(j);

        collisionState.ContactCount = manifold.Count;
        collisionState.Flushes = _flushes;
    }

    public unsafe void Flush()
    {
#error Handle handler exceptions
        return;
        int workerIndex = 0;
        var manifold = new EmptyManifold();
        bool flippedManifold = false; // The flipped manifold argument does not make sense in this context given that we pass an empty one

        //Remove any stale collisions. Stale collisions are those which should have received a new manifold update but did not because the manifold is no longer active.
        foreach (var (pair, state) in _trackedCollisions)
        {
            if (state.Flushes == _flushes)
                continue;

            if ((state.Flushes & 1) != (_flushes & 1))
                continue;

            // Two flushes ago, remove the collision

#if DEBUG
            ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, pair, out _);
            _trackedCollisions.Remove(pair);
            System.Diagnostics.Debug.Assert(stateRef.Alive == false); // Notify HandleManifoldInner higher up the call stack that the manifold they are processing is dead
#else
            _trackedCollisions.Remove(pair);
#endif

            for (int i = 0; i < state.ContactCount; i++)
            {
                state.HandlerA?.OnContactRemoved(pair.A, pair.B, ref manifold, flippedManifold, state.FeatureId[i], workerIndex, _simulation);
                state.HandlerB?.OnContactRemoved(pair.B, pair.A, ref manifold, flippedManifold, state.FeatureId[i], workerIndex, _simulation);
            }

            if (state.Touching)
            {
                state.HandlerA?.OnStoppedTouching(pair.A, pair.B, ref manifold, flippedManifold, workerIndex, _simulation);
                state.HandlerB?.OnStoppedTouching(pair.B, pair.A, ref manifold, flippedManifold, workerIndex, _simulation);
            }

            state.HandlerA?.OnPairEnded(pair.A, pair.B, _simulation);
            state.HandlerB?.OnPairEnded(pair.B, pair.A, _simulation);
        }

        _flushes++;
    }

    /// <summary>
    /// Callback attached to the simulation's ITimestepper which executes just prior to collision detection to take a snapshot of activity states to determine which pairs we should expect updates in.
    /// </summary>
    /*void SetFreshnessForCurrentActivityStatus(float dt, IThreadDispatcher threadDispatcher)
    {
        //Every single pair tracked by the contact events has a 'freshness' flag. If the final flush sees a pair that is stale, it'll remove it
        //and any necessary events to represent the end of that pair are reported.
        //HandleManifoldForCollidable sets 'Fresh' to true for any processed pair, but pairs between sleeping or static bodies will not show up in HandleManifoldForCollidable since they're not active.
        //We don't want Flush to report that sleeping pairs have stopped colliding, so we pre-initialize any such sleeping/static pair as 'fresh'.

        //This could be multithreaded reasonably easily if there are a ton of listeners or collisions, but that would be a pretty high bar.
        //For simplicity, the demo will keep it single threaded.
        var bodyHandleToLocation = _simulation.Simulation.Bodies.HandleToLocation;
        foreach (var trackedCollision in _trackedCollisions)
        {
            var source = trackedCollision.Key.A.ContactEventHandler;

            //If it's a body, and it's in the active set (index 0), then every pair associated with the listener should expect updates.
            var sourceExpectsUpdates = source.Mobility != CollidableMobility.Static && bodyHandleToLocation[source.BodyHandle.Value].SetIndex == 0;
            if (sourceExpectsUpdates)
            {
                var previousCollisions = listeners[listenerIndex].PreviousCollisions;
                for (int j = 0; j < previousCollisions.Count; ++j)
                {
                    //Pair updates will set the 'freshness' to true when they happen, so that they won't be considered 'stale' in the flush and removed.
                    previousCollisions[j].Fresh = false;
                }
            }
            else
            {
                //The listener is either static or sleeping. We should only expect updates if the other collidable is awake.
                var previousCollisions = listeners[listenerIndex].PreviousCollisions;
                for (int j = 0; j < previousCollisions.Count; ++j)
                {
                    ref var previousCollision = ref previousCollisions[j];
                    previousCollision.Fresh = previousCollision.Collidable.Mobility == CollidableMobility.Static || bodyHandleToLocation[previousCollision.Collidable.BodyHandle.Value].SetIndex > 0;
                }
            }
        }
    }*/

    private unsafe struct LastCollisionState
    {
        public const int FeatureCount = 4;

        public IContactEventHandler? HandlerA, HandlerB;
        public bool Alive;
        public bool Touching;
        public int Flushes;
        public int ContactCount;
        //FeatureIds are identifiers encoding what features on the involved shapes contributed to the contact. We store up to 4 feature ids, one for each potential contact.
        //A "feature" is things like a face, vertex, or edge. There is no single interpretation for what a feature is- the mapping is defined on a per collision pair level.
        //In this demo, we only care to check whether a given contact in the current frame maps onto a contact from a previous frame.
        //We can use this to only emit 'contact added' events when a new contact with an unrecognized id is reported.
        public fixed int FeatureId[FeatureCount];
    }

    private readonly record struct OrderedPair
    {
        public readonly CollidableComponent A, B;
        public OrderedPair(CollidableComponent a, CollidableComponent b)
        {
            (A, B) = a.StableId > b.StableId ? (a, b) : (b, a);
        }
    }


    private struct EmptyManifold : IContactManifold<EmptyManifold>
    {
        public int Count => 0;
        public bool Convex => true;
        public Contact this[int contactIndex] { get => throw new IndexOutOfRangeException("This manifold is empty"); set => throw new IndexOutOfRangeException("This manifold is empty"); }
        public static ref ConvexContact GetConvexContactReference(ref EmptyManifold manifold, int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public static ref float GetDepthReference(ref EmptyManifold manifold, int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public static ref int GetFeatureIdReference(ref EmptyManifold manifold, int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public static ref Contact GetNonconvexContactReference(ref EmptyManifold manifold, int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public static ref Vector3 GetNormalReference(ref EmptyManifold manifold, int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public static ref Vector3 GetOffsetReference(ref EmptyManifold manifold, int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public void GetContact(int contactIndex, out Vector3 offset, out Vector3 normal, out float depth, out int featureId) => throw new IndexOutOfRangeException("This manifold is empty");
        public void GetContact(int contactIndex, out Contact contactData) => throw new IndexOutOfRangeException("This manifold is empty");
        public float GetDepth(int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public int GetFeatureId(int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public Vector3 GetNormal(int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
        public Vector3 GetOffset(int contactIndex) => throw new IndexOutOfRangeException("This manifold is empty");
    }
}

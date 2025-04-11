// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.InteropServices;
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
    private readonly HashSet<OrderedPair> _outdatedPairs = new();
    private readonly BufferPool _pool;
    private readonly BepuSimulation _simulation;
    private IndexSet _staticListenerFlags;
    private IndexSet _bodyListenerFlags;

    public ContactEventsManager(BufferPool pool, BepuSimulation simulation)
    {
        _pool = pool;
        _simulation = simulation;
    }

    public void Initialize()
    {
        _simulation.Simulation.Timestepper.BeforeCollisionDetection += TrackActivePairs;
    }

    public void Dispose()
    {
        _simulation.Simulation.Timestepper.BeforeCollisionDetection -= TrackActivePairs;
        if (_bodyListenerFlags.Flags.Allocated)
            _bodyListenerFlags.Dispose(_pool);
        if (_staticListenerFlags.Flags.Allocated)
            _staticListenerFlags.Dispose(_pool);
    }

    /// <summary>
    /// Begins listening for events related to the given collidable.
    /// </summary>
    public void Register(CollidableComponent collidable)
    {
        var reference = collidable.CollidableReference ?? throw new InvalidOperationException($"This Collidable's {nameof(CollidableReference)} should exist");
        if (reference.Mobility == CollidableMobility.Static)
            _staticListenerFlags.Add(reference.RawHandleValue, _pool);
        else
            _bodyListenerFlags.Add(reference.RawHandleValue, _pool);
    }

    /// <summary>
    /// Stops listening for events related to the given collidable.
    /// </summary>
    public void Unregister(CollidableComponent collidable)
    {
        var reference = collidable.CollidableReference ?? throw new InvalidOperationException($"This Collidable's {nameof(CollidableReference)} should exist");
        if (reference.Mobility == CollidableMobility.Static)
            _staticListenerFlags.Remove(reference.RawHandleValue);
        else
            _bodyListenerFlags.Remove(reference.RawHandleValue);

        ClearCollisionsOf(collidable);
    }

    /// <summary>
    /// Checks if a collidable is registered as a listener.
    /// </summary>
    public bool IsRegistered(CollidableComponent collidable)
    {
        if (collidable.CollidableReference is { } reference)
            return IsRegistered(reference);

        return false;
    }

    /// <summary>
    /// Checks if a collidable is registered as a listener.
    /// </summary>
    private bool IsRegistered(CollidableReference reference)
    {
        if (reference.Mobility == CollidableMobility.Static)
            return _staticListenerFlags.Contains(reference.RawHandleValue);
        else
            return _bodyListenerFlags.Contains(reference.RawHandleValue);
    }

    public void ClearCollisionsOf(CollidableComponent collidable)
    {
        // Really slow, but improving performance has a huge amount of gotchas since user code
        // may cause this method to be re-entrant through handler calls.
        // Something to investigate later

        var manifold = new EmptyManifold();
        foreach (var (pair, state) in _trackedCollisions)
        {
            if (!ReferenceEquals(pair.A, collidable) && !ReferenceEquals(pair.B, collidable))
                continue;

            ClearCollision(pair, ref manifold, 0);
        }
    }

    private unsafe void ClearCollision(OrderedPair pair, ref EmptyManifold manifold, int workerIndex)
    {
        const bool flippedManifold = false; // The flipped manifold argument does not make sense in this context given that we pass an empty one
#if DEBUG
        ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, pair, out _);
        _trackedCollisions.Remove(pair, out var state);
        System.Diagnostics.Debug.Assert(stateRef.Alive == false); // Notify HandleManifoldInner higher up the call stack that the manifold they are processing is dead
#else
        _trackedCollisions.Remove(pair, out var state);
#endif

        for (int i = 0; i < state.ACount; i++)
            state.HandlerA?.OnContactRemoved(pair.A, pair.B, ref manifold, flippedManifold, state.FeatureIdA[i], workerIndex, _simulation);
        for (int i = 0; i < state.BCount; i++)
            state.HandlerB?.OnContactRemoved(pair.B, pair.A, ref manifold, flippedManifold, state.FeatureIdB[i], workerIndex, _simulation);

        if (state.TryClear(Events.TouchingA))
            state.HandlerA?.OnStoppedTouching(pair.A, pair.B, ref manifold, flippedManifold, workerIndex, _simulation);
        if (state.TryClear(Events.TouchingB))
            state.HandlerB?.OnStoppedTouching(pair.B, pair.A, ref manifold, flippedManifold, workerIndex, _simulation);

        if (state.TryClear(Events.CreatedA))
            state.HandlerA?.OnPairEnded(pair.A, pair.B, _simulation);
        if (state.TryClear(Events.CreatedB))
            state.HandlerB?.OnPairEnded(pair.B, pair.A, _simulation);

        _outdatedPairs.Remove(pair);
    }

    public void HandleManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        bool aListener = IsRegistered(pair.A);
        bool bListener = IsRegistered(pair.B);
        if (aListener == false && bListener == false)
            return;

        HandleManifoldInner(workerIndex, _simulation.GetComponent(pair.A), _simulation.GetComponent(pair.B), ref manifold);
    }

    private unsafe void HandleManifoldInner<TManifold>(int workerIndex, CollidableComponent a, CollidableComponent b, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        System.Diagnostics.Debug.Assert(manifold.Count <= LastCollisionState.FeatureCount, "This was built on the assumption that nonconvex manifolds will have a maximum of 4 contacts, but that might have changed.");
        //If the above assert gets hit because of a change to nonconvex manifold capacities, the packed feature id representation this uses will need to be updated.
        //I very much doubt the nonconvex manifold will ever use more than 8 contacts, so addressing this wouldn't require much of a change.

        // We must first sort the collidables to ensure calls happen in a deterministic order, and to mimic `ClearCollision`'s order
        var orderedPair = new OrderedPair(a, b);

        bool aFlipped = ReferenceEquals(a, orderedPair.B); // Whether the manifold is flipped from a's point of view
        bool bFlipped = !aFlipped;

        (a, b) = (orderedPair.A, orderedPair.B);

        IContactEventHandler? handlerA;
        IContactEventHandler? handlerB;
        ref var collisionState = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, orderedPair, out bool alreadyExisted);
        if (alreadyExisted)
        {
            handlerA = collisionState.HandlerA;
            handlerB = collisionState.HandlerB;
            bool touching = false;
            for (int contactIndex = 0; contactIndex < manifold.Count; ++contactIndex)
            {
                if (manifold.GetDepth(contactIndex) < 0)
                    continue;

                touching = true;
                if (handlerA is not null && collisionState.TrySet(Events.TouchingA))
                {
                    handlerA.OnStartedTouching(a, b, ref manifold, aFlipped, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }
                if (handlerB is not null && collisionState.TrySet(Events.TouchingB))
                {
                    handlerB.OnStartedTouching(b, a, ref manifold, bFlipped, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }

                handlerA?.OnTouching(a, b, ref manifold, aFlipped, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                handlerB?.OnTouching(b, a, ref manifold, bFlipped, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
                break;
            }

            if (touching == false && handlerA is not null && collisionState.TryClear(Events.TouchingA))
            {
                handlerA.OnStoppedTouching(a, b, ref manifold, aFlipped, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }

            if (touching == false && handlerB is not null && collisionState.TryClear(Events.TouchingB))
            {
                handlerB.OnStoppedTouching(b, a, ref manifold, bFlipped, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }

            uint toRemove = (1u << collisionState.ACount) - 1u; // Bitmask to mark contacts we have to change
            uint toAdd = (1u << manifold.Count) - 1u;

            for (int i = 0; i < manifold.Count; ++i) // Check if any of our previous contact still exist
            {
                int featureId = manifold.GetFeatureId(i);
                for (int j = 0; j < collisionState.ACount; ++j)
                {
                    if (featureId != collisionState.FeatureIdA[j])
                        continue;

                    toAdd ^= 1u << i;
                    toRemove ^= 1u << j;
                    break;
                }
            }

            while (toRemove != 0)
            {
                int index = 31 - BitOperations.LeadingZeroCount(toRemove); // LeadingZeroCount to remove from the end to the start
                toRemove ^= 1u << index;

                int id = collisionState.FeatureIdA[index];

                collisionState.ACount--;
                if (index != collisionState.ACount)
                    collisionState.FeatureIdA[index] = collisionState.FeatureIdA[collisionState.ACount]; // Remove this index by swapping with last one

                handlerA?.OnContactRemoved(a, b, ref manifold, aFlipped, id, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;

                collisionState.BCount--;
                if (index != collisionState.BCount)
                    collisionState.FeatureIdB[index] = collisionState.FeatureIdB[collisionState.BCount];

                handlerB?.OnContactRemoved(b, a, ref manifold, bFlipped, id, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }

            while (toAdd != 0)
            {
                int index = BitOperations.TrailingZeroCount(toAdd); // We can add from the start to the end here
                toAdd ^= 1u << index;

                int featureId = manifold.GetFeatureId(index);

                collisionState.FeatureIdA[collisionState.ACount++] = featureId;
                handlerA?.OnContactAdded(a, b, ref manifold, aFlipped, index, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;

                collisionState.FeatureIdB[collisionState.BCount++] = featureId;
                handlerB?.OnContactAdded(b, a, ref manifold, bFlipped, index, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }
        }
        else
        {
            collisionState.Alive = true; // This is set as a flag to check for removal events
            handlerA = collisionState.HandlerA = a.ContactEventHandler;
            handlerB = collisionState.HandlerB = b.ContactEventHandler;

            if (handlerA is not null && collisionState.TrySet(Events.CreatedA))
            {
                handlerA.OnPairCreated(a, b, ref manifold, aFlipped, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }

            if (handlerB is not null && collisionState.TrySet(Events.CreatedB))
            {
                handlerB.OnPairCreated(b, a, ref manifold, bFlipped, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }

            for (int i = 0; i < manifold.Count; ++i)
            {
                if (manifold.GetDepth(i) < 0)
                    continue;

                if (handlerA is not null && collisionState.TrySet(Events.TouchingA))
                {
                    handlerA.OnStartedTouching(a, b, ref manifold, aFlipped, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }

                if (handlerB is not null && collisionState.TrySet(Events.TouchingB))
                {
                    handlerB.OnStartedTouching(b, a, ref manifold, bFlipped, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }

                if (handlerA is not null)
                {
                    handlerA.OnTouching(a, b, ref manifold, aFlipped, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }

                if (handlerB is not null)
                {
                    handlerB.OnTouching(b, a, ref manifold, bFlipped, workerIndex, _simulation);
                    if (collisionState.Alive == false)
                        return;
                }
                break;
            }

            for (int i = 0; i < manifold.Count; ++i)
            {
                int featureId = manifold.GetFeatureId(i);

                collisionState.FeatureIdA[collisionState.ACount++] = featureId;
                handlerA?.OnContactAdded(a, b, ref manifold, aFlipped, i, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;

                collisionState.FeatureIdB[collisionState.BCount++] = featureId;
                handlerB?.OnContactAdded(b, a, ref manifold, bFlipped, i, workerIndex, _simulation);
                if (collisionState.Alive == false)
                    return;
            }
        }

        if (handlerA is not null)
        {
            handlerA.OnPairUpdated(a, b, ref manifold, aFlipped, workerIndex, _simulation);
            if (collisionState.Alive == false)
                return;
        }

        if (handlerB is not null)
        {
            handlerB.OnPairUpdated(b, a, ref manifold, bFlipped, workerIndex, _simulation);
            if (collisionState.Alive == false)
                return;
        }

        _outdatedPairs.Remove(orderedPair);
    }

    public void Flush()
    {
        var manifold = new EmptyManifold();

        //Remove any stale collisions. Stale collisions are those which should have received a new manifold update but did not because the manifold is no longer active.
        foreach (var pair in _outdatedPairs)
            ClearCollision(pair, ref manifold, 0);
    }

    /// <summary>
    /// Callback attached to the simulation's ITimestepper which executes just prior to collision detection to take a snapshot of activity states to determine which pairs we should expect updates in.
    /// </summary>
    private void TrackActivePairs(float dt, IThreadDispatcher threadDispatcher)
    {
        // We need to be notified when two collidables are too far apart to have a manifold between them,
        // We'll track any collision were one of the pair is active, manifolds we receive will filter out those that are still in contact
        // leaving us to Flush() only those that are not

        var bodyHandleToLocation = _simulation.Simulation.Bodies.HandleToLocation;
        foreach (var trackedCollision in _trackedCollisions)
        {
            var aRef = trackedCollision.Key.A.CollidableReference ?? throw new InvalidOperationException();
            var bRef = trackedCollision.Key.B.CollidableReference ?? throw new InvalidOperationException();
            if ((aRef.Mobility != CollidableMobility.Static && bodyHandleToLocation[aRef.BodyHandle.Value].SetIndex == 0)
                || (bRef.Mobility != CollidableMobility.Static && bodyHandleToLocation[bRef.BodyHandle.Value].SetIndex == 0))
            {
                _outdatedPairs.Add(trackedCollision.Key); // It's active, if manifolds did not signal that they touched we should discard this one
            }
        }
    }

    private unsafe struct LastCollisionState
    {
        public const int FeatureCount = 4;

        public IContactEventHandler? HandlerA, HandlerB;
        public bool Alive;
        public Events EventsTriggered;
        public int ACount;
        public int BCount;
        //FeatureIds are identifiers encoding what features on the involved shapes contributed to the contact. We store up to 4 feature ids, one for each potential contact.
        //A "feature" is things like a face, vertex, or edge. There is no single interpretation for what a feature is- the mapping is defined on a per collision pair level.
        //In this demo, we only care to check whether a given contact in the current frame maps onto a contact from a previous frame.
        //We can use this to only emit 'contact added' events when a new contact with an unrecognized id is reported.
        public fixed int FeatureIdA[FeatureCount];
        public fixed int FeatureIdB[FeatureCount];

        public bool TrySet(Events e)
        {
            if ((e & EventsTriggered) == 0)
            {
                EventsTriggered |= e;
                return true;
            }

            return false;
        }

        public bool TryClear(Events e)
        {
            if ((e & EventsTriggered) == e)
            {
                EventsTriggered ^= e;
                return true;
            }

            return false;
        }
    }

    [Flags]
    private enum Events
    {
        CreatedA = 0b0001,
        CreatedB = 0b0010,
        TouchingA = 0b0100,
        TouchingB = 0b1000,
    }

    private readonly record struct OrderedPair
    {
        public readonly CollidableComponent A, B;
        public OrderedPair(CollidableComponent a, CollidableComponent b)
        {
            if (a.InstanceIndex != b.InstanceIndex)
                (A, B) = a.InstanceIndex > b.InstanceIndex ? (a, b) : (b, a);
            else if (a.GetHashCode() != b.GetHashCode())
                (A, B) = a.GetHashCode() > b.GetHashCode() ? (a, b) : (b, a);
            else if (ReferenceEquals(a, b))
                (A, B) = (a, b);
            else
                throw new InvalidOperationException("Could not order this pair of collidable, incredibly unlikely event");
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

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.CompilerServices;
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
    private IPerTypeManifoldStore[][] _manifoldStoresPerWorker;

    public ContactEventsManager(BufferPool pool, BepuSimulation simulation, int workerCount)
    {
        _pool = pool;
        _simulation = simulation;
        _manifoldStoresPerWorker = new IPerTypeManifoldStore[workerCount][];
        for (int i = 0; i < _manifoldStoresPerWorker.Length; i++)
            _manifoldStoresPerWorker[i] = [];
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsRegistered(CollidableReference reference)
    {
        if (reference.Mobility == CollidableMobility.Static)
            return _staticListenerFlags.Contains(reference.RawHandleValue);
        else
            return _bodyListenerFlags.Contains(reference.RawHandleValue);
    }

    public void ClearCollisionsOf(CollidableComponent collidable)
    {
        foreach (var workerStore in _manifoldStoresPerWorker)
        {
            foreach (var typeStore in workerStore)
                typeStore.ClearEventsOf(collidable);
        }

        // Really slow, but improving performance has a huge amount of gotchas since user code
        // may cause this method to be re-entrant through handler calls.
        // Something to investigate later

        var manifold = new EmptyManifold();
        foreach (var (pair, state) in _trackedCollisions)
        {
            if (!ReferenceEquals(pair.A, collidable) && !ReferenceEquals(pair.B, collidable))
                continue;

            ClearCollision(pair, in manifold);
        }
    }

    private void ClearCollision(OrderedPair pair, in EmptyManifold manifold)
    {
        const bool flippedManifold = false; // The flipped manifold argument does not make sense in this context given that we pass an empty one
#if DEBUG
        ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, pair, out _);
        _trackedCollisions.Remove(pair, out var state);
        System.Diagnostics.Debug.Assert(stateRef.Alive == false); // Notify HandleManifoldInner higher up the call stack that the manifold they are processing is dead
#else
        _trackedCollisions.Remove(pair, out var state);
#endif

        if (state.TryClear(Events.TouchingA))
        {
            state.HandlerA?.OnStoppedTouching(
                new ContactData<EmptyManifold>
                {
                    EventSource = pair.A,
                    Other = pair.B,
                    Manifold = manifold,
                    FlippedManifold = flippedManifold,
                    ChildIndexSource = 0,
                    ChildIndexOther = 0,
                    Simulation = _simulation,
                });
        }

        if (state.TryClear(Events.TouchingB))
        {
            state.HandlerB?.OnStoppedTouching(
                new ContactData<EmptyManifold>
                {
                    EventSource = pair.B,
                    Other = pair.A,
                    Manifold = manifold,
                    FlippedManifold = flippedManifold,
                    ChildIndexSource = 0,
                    ChildIndexOther = 0,
                    Simulation = _simulation,
                });
        }

        _outdatedPairs.Remove(pair);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleManifold<TManifold>(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        bool aListener = IsRegistered(pair.A);
        bool bListener = IsRegistered(pair.B);
        if (aListener == false && bListener == false)
            return;

        IPerTypeManifoldStore.StoreManifold(_manifoldStoresPerWorker, workerIndex, ref manifold, _simulation.GetComponent(pair.A), _simulation.GetComponent(pair.B), childIndexA, childIndexB);
    }

    private void RunManifoldEvent<TManifold>(CollidableComponent a, CollidableComponent b, int childIndexA, int childIndexB, TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // We must first sort the collidables to ensure calls happen in a deterministic order, and to mimic `ClearCollision`'s order
        var orderedPair = new OrderedPair(a, b);

        bool aFlipped = ReferenceEquals(a, orderedPair.B); // Whether the manifold is flipped from a's point of view
        bool bFlipped = !aFlipped;
        (a, b) = (orderedPair.A, orderedPair.B);

        var contactDataForA = new ContactData<TManifold>
        {
            EventSource = a,
            Other = b,
            Manifold = manifold,
            FlippedManifold = aFlipped,
            ChildIndexSource = childIndexA,
            ChildIndexOther = childIndexB,
            Simulation = _simulation,
        };

        var contactDataForB = new ContactData<TManifold>
        {
            EventSource = b,
            Other = a,
            Manifold = manifold,
            FlippedManifold = bFlipped,
            ChildIndexSource = childIndexB,
            ChildIndexOther = childIndexA,
            Simulation = _simulation,
        };

        IContactHandler? handlerA, handlerB;
        ref var collisionState = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, orderedPair, out bool alreadyExisted);
        if (alreadyExisted)
        {
            handlerA = collisionState.HandlerA;
            handlerB = collisionState.HandlerB;
        }
        else
        {
            collisionState.Alive = true; // This is set as a flag to check for removal events
            handlerA = collisionState.HandlerA = a.ContactEventHandler;
            handlerB = collisionState.HandlerB = b.ContactEventHandler;
        }

        bool touching = false;
        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) >= 0)
            {
                touching = true;
                break;
            }
        }

        if (touching)
        {
            if (handlerA is not null && collisionState.TrySet(Events.TouchingA))
            {
                handlerA.OnStartedTouching(contactDataForA);
                if (collisionState.Alive == false)
                    return;
            }

            if (handlerB is not null && collisionState.TrySet(Events.TouchingB))
            {
                handlerB.OnStartedTouching(contactDataForB);
                if (collisionState.Alive == false)
                    return;
            }

            if (handlerA is not null)
            {
                handlerA.OnTouching(contactDataForA);
                if (collisionState.Alive == false)
                    return;
            }

            if (handlerB is not null)
            {
                handlerB.OnTouching(contactDataForB);
                if (collisionState.Alive == false)
                    return;
            }
        }
        else
        {
            if (handlerA is not null && collisionState.TryClear(Events.TouchingA))
            {
                handlerA.OnStoppedTouching(contactDataForA);
                if (collisionState.Alive == false)
                    return;
            }

            if (handlerB is not null && collisionState.TryClear(Events.TouchingB))
            {
                handlerB.OnStoppedTouching(contactDataForB);
                if (collisionState.Alive == false)
                    return;
            }
        }

        _outdatedPairs.Remove(orderedPair);
    }

    public void Flush()
    {
        foreach (var workerStore in _manifoldStoresPerWorker)
        {
            foreach (var typeStore in workerStore)
                typeStore.RunEvents(this);
        }

        var manifold = new EmptyManifold();

        //Remove any stale collisions. Stale collisions are those which should have received a new manifold update but did not because the manifold is no longer active.
        foreach (var pair in _outdatedPairs)
            ClearCollision(pair, in manifold);
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

    private interface IPerTypeManifoldStore
    {
        void RunEvents(ContactEventsManager eventsManager);

        void ClearEventsOf(CollidableComponent collidableComponent);

        public static unsafe void StoreManifold<TManifold>(IPerTypeManifoldStore[][] manifoldLists, int workerIndex, ref TManifold manifold, CollidableComponent a, CollidableComponent b, int childIndexA, int childIndexB) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            var manifoldsForWorker = manifoldLists[workerIndex];
            int typeIndex = TypeIndex<TManifold>.Index;
            if (manifoldsForWorker.Length <= typeIndex)
            {
                // This type does not have a list to store those manifolds, make space and create an instance

                Array.Resize(ref manifoldsForWorker, typeIndex + 1);
                // Ensure we have stores for all previous types up to this one
                for (int j = 0; j < manifoldsForWorker.Length; j++)
                {
                    ref var spot = ref manifoldsForWorker[j];
                    if (spot == null!)
                        spot = manifoldStoreConstructors[j]();
                }

                manifoldLists[workerIndex] = manifoldsForWorker;
            }

            var handler = (ListOf<TManifold>)manifoldsForWorker[typeIndex];
            handler.Add((manifold, a, b, childIndexA, childIndexB));
        }

        private static int indexMax = -1;
        private static unsafe delegate*<IPerTypeManifoldStore>[] manifoldStoreConstructors = [];
        private static object perTypeLock = new();

        // On purpose, we want
        // ReSharper disable once UnusedTypeParameter
        private static class TypeIndex<TManifold> where TManifold : unmanaged, IContactManifold<TManifold>
        {
            public static readonly int Index;

            static unsafe TypeIndex()
            {
                lock (perTypeLock)
                {
                    Index = Interlocked.Increment(ref indexMax);
                    var cpy = new delegate*<IPerTypeManifoldStore>[manifoldStoreConstructors.Length + 1];
                    manifoldStoreConstructors.CopyTo(cpy, 0);
                    cpy[Index] = &ManifoldCtor;
                    manifoldStoreConstructors = cpy;
                }
            }

            private static ListOf<TManifold> ManifoldCtor() => new();
        }

        private class ListOf<TManifold> : List<(TManifold manifold, CollidableComponent a, CollidableComponent b, int childIndexA, int childIndexB)>, IPerTypeManifoldStore where TManifold : unmanaged, IContactManifold<TManifold>
        {
            public void RunEvents(ContactEventsManager eventsManager)
            {
                for (int i = Count - 1; i >= 0; i--) // reverse as the scope may end up calling ClearRelatedContacts
                {
                    var (manifold, a, b, childIndexA, childIndexB) = this[i];
                    eventsManager.RunManifoldEvent(a, b, childIndexA, childIndexB, manifold);
                    if (i > Count) // If the method above ended up removing a significant amount of events, make sure to continue from a sane spot
                        i = Count;
                }

                Clear();
            }

            public void ClearEventsOf(CollidableComponent collidableComponent)
            {
                var spanOfThis = CollectionsMarshal.AsSpan(this);
                for (int i = spanOfThis.Length - 1; i >= 0; i--)
                {
                    if (spanOfThis[i].a == collidableComponent || spanOfThis[i].b == collidableComponent)
                        RemoveAt(i);
                }
            }
        }
    }

    private struct LastCollisionState
    {
        public IContactHandler? HandlerA, HandlerB;
        public bool Alive;
        public Events EventsTriggered;

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
        TouchingA = 0b01,
        TouchingB = 0b10,
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

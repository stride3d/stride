// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
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
    private readonly HashSet<OrderedPair> _touchingPairs = new();
    private readonly BufferPool _pool;
    private readonly BepuSimulation _simulation;
    private IndexSet _staticListenerFlags;
    private IndexSet _bodyListenerFlags;
    private IPerTypeManifoldStore[][] _manifoldStoresPerWorker;

    // Groups of one event copied on the stack before the handlers run, larger events go through a pooled array
    private const int MaxGroupsOnStack = 64;

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

    public void ResizeWorkerCount(int newWorkerCount)
    {
        foreach (var workerStore in _manifoldStoresPerWorker)
        {
            foreach (var typeStore in workerStore)
            {
                if (typeStore.IsEmpty() == false)
                    throw new InvalidOperationException("Cannot resize the manifold store, manifolds have not been flushed yet");
            }
        }

        _manifoldStoresPerWorker = new IPerTypeManifoldStore[newWorkerCount][];
        for (int i = 0; i < _manifoldStoresPerWorker.Length; i++)
            _manifoldStoresPerWorker[i] = [];
    }

    /// <summary>
    /// Begins listening for events related to the given collidable.
    /// </summary>
    public void Register(CollidableComponent collidable)
    {
        if (collidable.CollidableReference is null)
            throw new InvalidOperationException($"This Collidable's {nameof(CollidableReference)} should exist");

        for (int i = 0; i < collidable.CollidableCount; i++)
        {
            var reference = collidable.GetCollidableReference(i);
            if (reference.Mobility == CollidableMobility.Static)
                _staticListenerFlags.Add(reference.RawHandleValue, _pool);
            else
                _bodyListenerFlags.Add(reference.RawHandleValue, _pool);
        }
    }

    /// <summary>
    /// Stops listening for events related to the given collidable.
    /// </summary>
    public void Unregister(CollidableComponent collidable)
    {
        if (collidable.CollidableReference is null)
            throw new InvalidOperationException($"This Collidable's {nameof(CollidableReference)} should exist");

        for (int i = 0; i < collidable.CollidableCount; i++)
        {
            var reference = collidable.GetCollidableReference(i);
            if (reference.Mobility == CollidableMobility.Static)
                _staticListenerFlags.Remove(reference.RawHandleValue);
            else
                _bodyListenerFlags.Remove(reference.RawHandleValue);
        }

        ClearStoredManifoldsOf(collidable);
        ClearTrackedCollisionsOf(collidable);
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

    /// <summary> Drops this step's manifolds involving <paramref name="collidable"/>, its references must still be valid </summary>
    public void ClearStoredManifoldsOf(CollidableComponent collidable)
    {
        if (collidable.CollidableReference is not { } reference)
            return;

        // Every group of a collidable carries the same owner key, however many bodies it spans: one scan per store is enough
        uint owner = reference.Packed;
        foreach (var workerStore in _manifoldStoresPerWorker)
        {
            foreach (var typeStore in workerStore)
                typeStore.ClearEventsOf(owner);
        }
    }

    /// <summary> Ends the collisions <paramref name="collidable"/> was part of, sending the matching events </summary>
    public void ClearTrackedCollisionsOf(CollidableComponent collidable)
    {
        // Really slow, but improving performance has a huge amount of gotchas since user code
        // may cause this method to be re-entrant through handler calls.
        // Something to investigate later

        foreach (var (pair, state) in _trackedCollisions)
        {
            if (!ReferenceEquals(pair.A, collidable) && !ReferenceEquals(pair.B, collidable))
                continue;

            ClearCollision(pair);
        }
    }

    private void ClearCollision(OrderedPair pair)
    {
#if DEBUG
        ref var stateRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedCollisions, pair, out _);
        _trackedCollisions.Remove(pair, out var state);
        Debug.Assert(stateRef.Alive == false); // Notify HandleManifoldInner higher up the call stack that the manifold they are processing is dead
#else
        _trackedCollisions.Remove(pair, out var state);
#endif

        _outdatedPairs.Remove(pair);

        if (state.TryClear(Events.TouchingA))
        {
            var contactDataForA = new Contacts<EmptyManifold>
            {
                Groups = ReadOnlySpan<ContactGroup<EmptyManifold>>.Empty,
                Simulation = _simulation,
                IsSourceOriginalA = true,
                EventSource = pair.A,
                Other = pair.B
            };
            state.HandlerA?.OnStoppedTouching(contactDataForA);
        }

        if (state.TryClear(Events.TouchingB))
        {
            var contactDataForB = new Contacts<EmptyManifold>
            {
                Groups = ReadOnlySpan<ContactGroup<EmptyManifold>>.Empty,
                Simulation = _simulation,
                IsSourceOriginalA = false,
                EventSource = pair.B,
                Other = pair.A
            };
            state.HandlerB?.OnStoppedTouching(contactDataForB);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreManifold<TManifold>(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        bool aListener = IsRegistered(pair.A);
        bool bListener = IsRegistered(pair.B);
        if (aListener == false && bListener == false)
            return;

        var componentA = _simulation.GetComponent(pair.A);
        var componentB = _simulation.GetComponent(pair.B);

        // Self contacts of a collidable spanning multiple bodies are not reported
        if (aListener && bListener && ReferenceEquals(componentA, componentB))
            return;

        uint ownerA = componentA.CollidableReference!.Value.Packed;
        uint ownerB = componentB.CollidableReference!.Value.Packed;
        var ownerSortedPair = ownerA > ownerB ? (ownerA, ownerB) : (ownerB, ownerA);
        IPerTypeManifoldStore.StoreManifold(_manifoldStoresPerWorker, workerIndex, ref manifold, pair, childIndexA, childIndexB, ownerSortedPair, ownerA > ownerB);
    }

    private void RunManifoldEvent<TManifold>(Span<ContactGroup<TManifold>> unsafeInfos) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // A collidable spanning multiple bodies can touch another one through hundreds of body pairs, more than the stack should take
        if (unsafeInfos.Length > MaxGroupsOnStack)
        {
            var rented = System.Buffers.ArrayPool<ContactGroup<TManifold>>.Shared.Rent(unsafeInfos.Length);
            try
            {
                var copy = rented.AsSpan(0, unsafeInfos.Length);
                unsafeInfos.CopyTo(copy);
                RunManifoldEventOnCopy(copy);
            }
            finally
            {
                System.Buffers.ArrayPool<ContactGroup<TManifold>>.Shared.Return(rented);
            }
            return;
        }

        // We have to do a stackalloc'ed copy as On*Touching may end up clearing the memory region where unsafeInfos resides through ClearEventsOf
        Span<ContactGroup<TManifold>> safeInfos = stackalloc ContactGroup<TManifold>[unsafeInfos.Length];
        unsafeInfos.CopyTo(safeInfos);
        RunManifoldEventOnCopy(safeInfos);
    }

    /// <param name="safeInfos"> A copy the handlers cannot reach, whatever they remove from the simulation </param>
    private void RunManifoldEventOnCopy<TManifold>(Span<ContactGroup<TManifold>> safeInfos) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        var orderedPair = new OrderedPair(_simulation.GetComponent(safeInfos[0].Pair.A), _simulation.GetComponent(safeInfos[0].Pair.B));

        _outdatedPairs.Remove(orderedPair);

        bool isAOriginalA = ReferenceEquals(_simulation.GetComponent(safeInfos[0].Pair.A), orderedPair.A);
        var contactDataForA = new Contacts<TManifold>
        {
            Groups = safeInfos,
            Simulation = _simulation,
            IsSourceOriginalA = isAOriginalA,
            EventSource = orderedPair.A,
            Other = orderedPair.B,
        };
        var contactDataForB = new Contacts<TManifold>
        {
            Groups = safeInfos,
            Simulation = _simulation,
            IsSourceOriginalA = isAOriginalA == false,
            EventSource = orderedPair.B,
            Other = orderedPair.A,
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
            handlerA = collisionState.HandlerA = orderedPair.A.ContactEventHandler;
            handlerB = collisionState.HandlerB = orderedPair.B.ContactEventHandler;
        }

        bool touching = _touchingPairs.Contains(orderedPair);

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
    }

    public void Flush()
    {
        // A collidable spanning multiple bodies gets one group per body pair, they must agree on whether the two collidables touch
        _touchingPairs.Clear();
        foreach (var workerStore in _manifoldStoresPerWorker)
        {
            foreach (var typeStore in workerStore)
                typeStore.CollectTouchingPairs(this);
        }

        // A soft body can touch through particles spread across several workers; merge every worker's
        // per-type list into worker 0 so RunEvents sees the full set in one pass and OnTouching fires once per step.
        MergeWorkersIntoWorkerZero();

        if (_manifoldStoresPerWorker.Length > 0)
        {
            foreach (var typeStore in _manifoldStoresPerWorker[0])
                typeStore?.RunEvents(this);
        }

        //Remove any stale collisions. Stale collisions are those which should have received a new manifold update but did not because the manifold is no longer active.
        foreach (var pair in _outdatedPairs)
            ClearCollision(pair);
    }

    private void MergeWorkersIntoWorkerZero()
    {
        if (_manifoldStoresPerWorker.Length <= 1)
            return;

        int maxTypes = 0;
        for (int w = 0; w < _manifoldStoresPerWorker.Length; w++)
        {
            if (_manifoldStoresPerWorker[w].Length > maxTypes)
                maxTypes = _manifoldStoresPerWorker[w].Length;
        }

        if (maxTypes == 0)
            return;

        IPerTypeManifoldStore.EnsureWorkerSlots(_manifoldStoresPerWorker, 0, maxTypes);
        var destStore = _manifoldStoresPerWorker[0];

        for (int w = 1; w < _manifoldStoresPerWorker.Length; w++)
        {
            var workerStore = _manifoldStoresPerWorker[w];
            for (int t = 0; t < workerStore.Length; t++)
            {
                var src = workerStore[t];
                if (src is null || src.IsEmpty())
                    continue;
                destStore[t].MergeFrom(src);
            }
        }
    }

    /// <summary>
    /// Callback attached to the simulation's ITimestepper which executes just prior to collision detection to take a snapshot of activity states to determine which pairs we should expect updates in.
    /// </summary>
    private void TrackActivePairs(float dt, IThreadDispatcher threadDispatcher)
    {
        // We need to be notified when two collidables are too far apart to have a manifold between them,
        // We'll track any collision were one of the pair is active, manifolds we receive will filter out those that are still in contact
        // leaving us to Flush() only those that are not

        foreach (var trackedCollision in _trackedCollisions)
        {
            if (IsActive(trackedCollision.Key.A) || IsActive(trackedCollision.Key.B))
                _outdatedPairs.Add(trackedCollision.Key); // It's active, if manifolds did not signal that they touched we should discard this one
        }
    }

    private bool IsActive(CollidableComponent collidable)
    {
        if (collidable.CollidableReference is null)
            throw new InvalidOperationException();

        var bodyHandleToLocation = _simulation.Simulation.Bodies.HandleToLocation;
        for (int i = 0; i < collidable.CollidableCount; i++)
        {
            var reference = collidable.GetCollidableReference(i);
            if (reference.Mobility != CollidableMobility.Static && bodyHandleToLocation[reference.BodyHandle.Value].SetIndex == 0)
                return true;
        }
        return false;
    }

    private void AddIfTouching<TManifold>(in ContactGroup<TManifold> group) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        for (int j = 0; j < group.Manifold.Count; ++j)
        {
            if (group.Manifold.GetDepth(j) >= 0)
            {
                _touchingPairs.Add(new OrderedPair(_simulation.GetComponent(group.Pair.A), _simulation.GetComponent(group.Pair.B)));
                return;
            }
        }
    }

    private interface IPerTypeManifoldStore
    {
        bool IsEmpty();

        void CollectTouchingPairs(ContactEventsManager eventsManager);

        void RunEvents(ContactEventsManager eventsManager);

        /// <summary> Removes the groups of the collidable whose owner key is <paramref name="owner"/> </summary>
        void ClearEventsOf(uint owner);

        void MergeFrom(IPerTypeManifoldStore other);

        internal static unsafe void EnsureWorkerSlots(IPerTypeManifoldStore[][] manifoldLists, int workerIndex, int slotCount)
        {
            var manifoldsForWorker = manifoldLists[workerIndex];
            if (manifoldsForWorker.Length >= slotCount)
                return;

            Array.Resize(ref manifoldsForWorker, slotCount);
            for (int j = 0; j < manifoldsForWorker.Length; j++)
            {
                ref var spot = ref manifoldsForWorker[j];
                if (spot == null!)
                    spot = manifoldStoreConstructors[j]();
            }
            manifoldLists[workerIndex] = manifoldsForWorker;
        }

        public static unsafe void StoreManifold<TManifold>(IPerTypeManifoldStore[][] manifoldLists, int workerIndex, ref TManifold manifold, CollidablePair pair, int childIndexA, int childIndexB, (uint A, uint B) ownerSortedPair, bool isPairAOwnerA) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            int typeIndex = TypeIndex<TManifold>.Index;
            EnsureWorkerSlots(manifoldLists, workerIndex, typeIndex + 1);
            var handler = (ListOf<TManifold>)manifoldLists[workerIndex][typeIndex];

            // Appended in arrival order, RunEvents sorts by owner once every worker is merged
            handler.Add(new ContactGroup<TManifold>(ref manifold, pair, childIndexA, childIndexB, ownerSortedPair, isPairAOwnerA));
        }

        private static int indexMax = -1;
        private static unsafe delegate*<IPerTypeManifoldStore>[] manifoldStoreConstructors = [];
        private static object perTypeLock = new();

        private class Comparer<TManifold> : IComparer<ContactGroup<TManifold>> where TManifold : unmanaged, IContactManifold<TManifold>
        {
            public static Comparer<TManifold> SharedInstance = new();

            public int Compare(ContactGroup<TManifold> x, ContactGroup<TManifold> y)
            {
                int aComp = x.SortedPair.A.CompareTo(y.SortedPair.A);
                return aComp != 0 ? aComp : x.SortedPair.B.CompareTo(y.SortedPair.B);
            }
        }

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

        private class ListOf<TManifold> : List<ContactGroup<TManifold>>, IPerTypeManifoldStore where TManifold : unmanaged, IContactManifold<TManifold>
        {
            public bool IsEmpty() => Count == 0;

            public void CollectTouchingPairs(ContactEventsManager eventsManager)
            {
                foreach (ref var group in CollectionsMarshal.AsSpan(this))
                    eventsManager.AddIfTouching(group);
            }

            public void MergeFrom(IPerTypeManifoldStore other)
            {
                var source = (ListOf<TManifold>)other;
                if (source.Count == 0)
                    return;

                int oldCount = Count;
                int newCount = oldCount + source.Count;
                EnsureCapacity(newCount);
                CollectionsMarshal.SetCount(this, newCount);
                var dest = CollectionsMarshal.AsSpan(this).Slice(oldCount, source.Count);
                CollectionsMarshal.AsSpan(source).CopyTo(dest);
                source.Clear();
            }

            public void RunEvents(ContactEventsManager eventsManager)
            {
                // StoreManifold appends on each worker and Flush merges worker 1..N into worker 0, whatever the amount of
                // workers the groups are in arrival order here. Sorted by owner, the body pairs of a collidable spanning
                // multiple bodies and the children of a compound are each reported as one range.
                if (Count > 1)
                    Sort(OwnerComparer.Instance);

                for (int i = Count - 1; i >= 0; i--) // reverse as the scope may end up calling ClearRelatedContacts
                {
                    var refPair = this[i].OwnerSortedPair;
                    int endExclusive = i + 1;
                    for (; i > 0 && this[i - 1].OwnerSortedPair == refPair; i--){ } // Find the range of collisions sharing the same pair

                    var transientSpan = CollectionsMarshal.AsSpan(this)[i..endExclusive];

                    eventsManager.RunManifoldEvent(transientSpan);
                    if (i > Count) // If the method above ended up removing a significant amount of events, make sure to continue from a sane spot
                        i = Count;
                }

                Clear();
            }

            private sealed class OwnerComparer : IComparer<ContactGroup<TManifold>>
            {
                public static readonly OwnerComparer Instance = new();

                public int Compare(ContactGroup<TManifold> x, ContactGroup<TManifold> y)
                {
                    int aComp = x.OwnerSortedPair.A.CompareTo(y.OwnerSortedPair.A);
                    if (aComp != 0)
                        return aComp;
                    int bComp = x.OwnerSortedPair.B.CompareTo(y.OwnerSortedPair.B);
                    return bComp != 0 ? bComp : Comparer<TManifold>.SharedInstance.Compare(x, y);
                }
            }

            public void ClearEventsOf(uint owner)
            {
                // Scan once and keep the pairs that do not involve the removed collidable; cheaper than a per-hit RemoveAt.
                var span = CollectionsMarshal.AsSpan(this);
                int kept = 0;
                for (int i = 0; i < span.Length; i++)
                {
                    ref var group = ref span[i];
                    if (group.OwnerSortedPair.A == owner || group.OwnerSortedPair.B == owner)
                        continue;
                    if (kept != i)
                        span[kept] = group;
                    kept++;
                }
                if (kept != span.Length)
                    RemoveRange(kept, span.Length - kept);
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

internal readonly record struct OrderedPair
{
    public readonly CollidableComponent A, B;

    public OrderedPair(CollidableComponent a, CollidableComponent b)
    {
        Debug.Assert(a.CollidableReference.HasValue);
        Debug.Assert(b.CollidableReference.HasValue);
        (A, B) = a.CollidableReference.Value.Packed > b.CollidableReference.Value.Packed ? (a, b) : (b, a);
    }

    public static (uint A, uint B) Sort(CollidablePair pair)
    {
        return pair.A.Packed > pair.B.Packed ? (pair.A.Packed, pair.B.Packed) : (pair.B.Packed, pair.A.Packed);
    }
}

using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Stride.BepuPhysics.Systems.Characters;

/// <summary>
/// Raw data for a dynamic character controller instance.
/// </summary>
public struct InternalCharacterData
{
    /// <summary>
    /// Direction the character is looking in world space. Defines the forward direction for movement.
    /// </summary>
    public Vector3 ViewDirection;
    /// <summary>
    /// Target horizontal velocity.
    /// X component refers to desired velocity along the strafing direction (perpendicular to the view direction projected down to the surface),
    /// Y component refers to the desired velocity along the forward direction (aligned with the view direction projected down to the surface).
    /// </summary>
    public Vector2 TargetVelocity;
    /// <summary>
    /// If true, the character will try to jump on the next time step. Will be reset to false after being processed.
    /// </summary>
    public bool TryJump;

    /// <summary>
    /// Handle of the body associated with the character.
    /// </summary>
    public BodyHandle BodyHandle;
    /// <summary>
    /// Character's up direction in the local space of the character's body.
    /// </summary>
    public Vector3 LocalUp;
    /// <summary>
    /// Velocity at which the character pushes off the support during a jump.
    /// </summary>
    public float JumpVelocity;
    /// <summary>
    /// Maximum force the character can apply tangent to the supporting surface to move.
    /// </summary>
    public float MaximumHorizontalForce;
    /// <summary>
    /// Maximum force the character can apply to glue itself to the supporting surface.
    /// </summary>
    public float MaximumVerticalForce;
    /// <summary>
    /// Cosine of the maximum slope angle that the character can treat as a support.
    /// </summary>
    public float CosMaximumSlope;
    /// <summary>
    /// Depth threshold beyond which a contact is considered a support if it the normal allows it.
    /// </summary>
    public float MinimumSupportDepth;
    /// <summary>
    /// Depth threshold beyond which a contact is considered a support if the previous frame had support, even if it isn't deep enough to meet the MinimumSupportDepth.
    /// </summary>
    public float MinimumSupportContinuationDepth;

    /// <summary>
    /// Whether the character is currently supported.
    /// </summary>
    public bool Supported;
    /// <summary>
    /// Collidable supporting the character, if any. Only valid if Supported is true.
    /// </summary>
    public CollidableReference Support;
    /// <summary>
    /// Handle of the character's motion constraint, if any. Only valid if Supported is true.
    /// </summary>
    public ConstraintHandle MotionConstraintHandle;
}

/// <summary>
/// System that manages all the characters in a simulation. Responsible for updating movement constraints based on character goals and contact states.
/// </summary>
internal unsafe class CharacterControllers : IDisposable
{
    /// <summary>
    /// Gets the simulation to which this set of characters belongs.
    /// </summary>
    public Simulation Simulation { get; private set; }
    BufferPool pool;

    Buffer<int> bodyHandleToCharacterIndex;
    QuickList<InternalCharacterData> characters;

    /// <summary>
    /// Gets the number of characters being controlled.
    /// </summary>
    public int CharacterCount { get { return characters.Count; } }

    /// <summary>
    /// Creates a character controller system.
    /// </summary>
    /// <param name="pool">Pool to allocate resources from.</param>
    /// <param name="initialCharacterCapacity">Number of characters to initially allocate space for.</param>
    /// <param name="initialBodyHandleCapacity">Number of body handles to initially allocate space for in the body handle->character mapping.</param>
    public CharacterControllers(BufferPool pool, int initialCharacterCapacity = 4096, int initialBodyHandleCapacity = 4096)
    {
        this.pool = pool;
        characters = new QuickList<InternalCharacterData>(initialCharacterCapacity, pool);
        ResizeBodyHandleCapacity(initialBodyHandleCapacity);
        analyzeContactsWorker = AnalyzeContactsWorker;
        expandBoundingBoxesWorker = ExpandBoundingBoxesWorker;
    }

    /// <summary>
    /// Caches the simulation associated with the characters.
    /// </summary>
    /// <param name="simulation">Simulation to be associated with the characters.</param>
    public void Initialize(Simulation simulation)
    {
        Simulation = simulation;
        simulation.Solver.Register<DynamicCharacterMotionConstraint>();
        simulation.Solver.Register<StaticCharacterMotionConstraint>();
        simulation.Timestepper.BeforeCollisionDetection += PrepareForContacts;
        simulation.Timestepper.CollisionsDetected += AnalyzeContacts;
    }

    private void ResizeBodyHandleCapacity(int bodyHandleCapacity)
    {
        var oldCapacity = bodyHandleToCharacterIndex.Length;
        pool.ResizeToAtLeast(ref bodyHandleToCharacterIndex, bodyHandleCapacity, bodyHandleToCharacterIndex.Length);
        if (bodyHandleToCharacterIndex.Length > oldCapacity)
        {
            Unsafe.InitBlockUnaligned(ref Unsafe.As<int, byte>(ref bodyHandleToCharacterIndex[oldCapacity]), 0xFF, (uint)((bodyHandleToCharacterIndex.Length - oldCapacity) * sizeof(int)));
        }
    }

    /// <summary>
    /// Gets the current memory slot index of a character using its associated body handle.
    /// </summary>
    /// <param name="bodyHandle">Body handle associated with the character to look up the index of.</param>
    /// <returns>Index of the character associated with the body handle.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCharacterIndexForBodyHandle(int bodyHandle)
    {
        Debug.Assert(bodyHandle >= 0 && bodyHandle < bodyHandleToCharacterIndex.Length && bodyHandleToCharacterIndex[bodyHandle] >= 0, "Can only look up indices for body handles associated with characters in this CharacterControllers instance.");
        return bodyHandleToCharacterIndex[bodyHandle];
    }

    /// <summary>
    /// Gets a reference to the character at the given memory slot index.
    /// </summary>
    /// <param name="index">Index of the character to retrieve.</param>
    /// <returns>Reference to the character at the given memory slot index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref InternalCharacterData GetCharacterByIndex(int index)
    {
        return ref characters[index];
    }

    /// <summary>
    /// Gets a reference to the character using the handle of the character's body.
    /// </summary>
    /// <param name="bodyHandle">Body handle of the character to look up.</param>
    /// <returns>Reference to the character associated with the given body handle.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref InternalCharacterData GetCharacterByBodyHandle(BodyHandle bodyHandle)
    {
        Debug.Assert(bodyHandle.Value >= 0 && bodyHandle.Value < bodyHandleToCharacterIndex.Length && bodyHandleToCharacterIndex[bodyHandle.Value] >= 0, "Can only look up indices for body handles associated with characters in this CharacterControllers instance.");
        return ref characters[bodyHandleToCharacterIndex[bodyHandle.Value]];
    }

    /// <summary>
    /// Allocates a character.
    /// </summary>
    /// <param name="bodyHandle">Body handle associated with the character.</param>
    /// <returns>Reference to the allocated character.</returns>
    public ref InternalCharacterData AllocateCharacter(BodyHandle bodyHandle)
    {
        Debug.Assert(bodyHandle.Value >= 0 && (bodyHandle.Value >= bodyHandleToCharacterIndex.Length || bodyHandleToCharacterIndex[bodyHandle.Value] == -1),
            "Cannot allocate more than one character for the same body handle.");
        if (bodyHandle.Value >= bodyHandleToCharacterIndex.Length)
            ResizeBodyHandleCapacity(Math.Max(bodyHandle.Value + 1, bodyHandleToCharacterIndex.Length * 2));
        var characterIndex = characters.Count;
        ref var character = ref characters.Allocate(pool);
        character = default;
        character.BodyHandle = bodyHandle;
        bodyHandleToCharacterIndex[bodyHandle.Value] = characterIndex;
        return ref character;
    }

    /// <summary>
    /// Removes a character from the character controllers set by the character's index.
    /// </summary>
    /// <param name="characterIndex">Index of the character to remove.</param>
    public void RemoveCharacterByIndex(int characterIndex)
    {
        Debug.Assert(characterIndex >= 0 && characterIndex < characters.Count, "Character index must exist in the set of characters.");
        ref var character = ref characters[characterIndex];
        Debug.Assert(character.BodyHandle.Value >= 0 && character.BodyHandle.Value < bodyHandleToCharacterIndex.Length && bodyHandleToCharacterIndex[character.BodyHandle.Value] == characterIndex,
            "Character must exist in the set of characters.");
        bodyHandleToCharacterIndex[character.BodyHandle.Value] = -1;
        characters.FastRemoveAt(characterIndex);
        //If the removal moved a character, update the body handle mapping.
        if (characters.Count > characterIndex)
        {
            bodyHandleToCharacterIndex[characters[characterIndex].BodyHandle.Value] = characterIndex;
        }
    }

    /// <summary>
    /// Removes a character from the character controllers set by the body handle associated with the character.
    /// </summary>
    /// <param name="bodyHandle">Body handle associated with the character to remove.</param>
    public void RemoveCharacterByBodyHandle(BodyHandle bodyHandle)
    {
        Debug.Assert(bodyHandle.Value >= 0 && bodyHandle.Value < bodyHandleToCharacterIndex.Length && bodyHandleToCharacterIndex[bodyHandle.Value] >= 0,
            "Removing a character by body handle requires that a character associated with the given body handle actually exists.");
        RemoveCharacterByIndex(bodyHandleToCharacterIndex[bodyHandle.Value]);
    }

    struct SupportCandidate
    {
        public Vector3 OffsetFromCharacter;
        public float Depth;
        public Vector3 OffsetFromSupport;
        public Vector3 Normal;
        public CollidableReference Support;
    }

    struct ContactCollectionWorkerCache
    {
        public Buffer<SupportCandidate> SupportCandidates;

        public ContactCollectionWorkerCache(int maximumCharacterCount, BufferPool pool)
        {
            pool.Take(maximumCharacterCount, out SupportCandidates);
            for (int i = 0; i < maximumCharacterCount; ++i)
            {
                //Initialize the depths to a value that guarantees replacement.
                SupportCandidates[i].Depth = float.MinValue;
            }
        }

        public void Dispose(BufferPool pool)
        {
            pool.Return(ref SupportCandidates);
        }
    }


    Buffer<ContactCollectionWorkerCache> contactCollectionWorkerCaches;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool TryReportContacts<TManifold>(CollidableReference characterCollidable, CollidableReference supportCollidable, CollidablePair pair, ref TManifold manifold, int workerIndex) where TManifold : struct, IContactManifold<TManifold>
    {
        if (characterCollidable.Mobility == CollidableMobility.Dynamic && characterCollidable.BodyHandle.Value < bodyHandleToCharacterIndex.Length)
        {
            var characterBodyHandle = characterCollidable.BodyHandle;
            var characterIndex = bodyHandleToCharacterIndex[characterBodyHandle.Value];
            if (characterIndex >= 0)
            {
                //This is actually a character.
                ref var character = ref characters[characterIndex];
                //Our job here is to process the manifold into a support representation. That means a single point, normal, and importance heuristic.
                //Note that we cannot safely pick from the candidates in this function- it is likely executed from a multithreaded context, so all we can do is
                //output the pair's result into a worker-exclusive buffer.

                //Contacts with sufficiently negative depth will not be considered support candidates.
                //Contacts with intermediate depth (above minimum threshold, but still below negative epsilon) may be candidates if the character previously had support.
                //Contacts with depth above negative epsilon always pass the depth test.

                //Maximum depth is used to heuristically choose which contact represents the support.
                //Note that this could be changed to subtly modify the behavior- for example, dotting the movement direction with the support normal and such.
                //A more careful choice of heuristic could make the character more responsive when trying to 'step' up obstacles.

                //Note that the body may be inactive during this callback even though it will be activated by new constraints after the narrow phase flushes.
                //Have to take into account the current potentially inactive location.
                ref var bodyLocation = ref Simulation.Bodies.HandleToLocation[character.BodyHandle.Value];
                ref var set = ref Simulation.Bodies.Sets[bodyLocation.SetIndex];
                ref var pose = ref set.DynamicsState[bodyLocation.Index].Motion.Pose;
                QuaternionEx.Transform(character.LocalUp, pose.Orientation, out var up);
                //Note that this branch is compiled out- the generic constraints force type specialization.
                if (manifold.Convex)
                {
                    ref var convexManifold = ref Unsafe.As<TManifold, ConvexContactManifold>(ref manifold);
                    var normalUpDot = Vector3.Dot(convexManifold.Normal, up);
                    //The narrow phase generates contacts with normals pointing from B to A by convention.
                    //If the character is collidable B, then we need to negate the comparison.
                    if ((pair.B.Packed == characterCollidable.Packed ? -normalUpDot : normalUpDot) > character.CosMaximumSlope)
                    {
                        //This manifold has a slope that is potentially supportive.
                        //Can the maximum depth contact be used as a support?
                        var maximumDepth = convexManifold.Contact0.Depth;
                        var maximumDepthIndex = 0;
                        for (int i = 1; i < convexManifold.Count; ++i)
                        {
                            ref var candidateDepth = ref Unsafe.Add(ref convexManifold.Contact0, i).Depth;
                            if (candidateDepth > maximumDepth)
                            {
                                maximumDepth = candidateDepth;
                                maximumDepthIndex = i;
                            }
                        }
                        if (maximumDepth >= character.MinimumSupportDepth || (character.Supported && maximumDepth > character.MinimumSupportContinuationDepth))
                        {
                            ref var supportCandidate = ref contactCollectionWorkerCaches[workerIndex].SupportCandidates[characterIndex];
                            if (supportCandidate.Depth < maximumDepth)
                            {
                                //This support candidate should be replaced.
                                supportCandidate.Depth = maximumDepth;
                                ref var deepestContact = ref Unsafe.Add(ref convexManifold.Contact0, maximumDepthIndex);
                                var offsetFromB = deepestContact.Offset - convexManifold.OffsetB;
                                if (pair.B.Packed == characterCollidable.Packed)
                                {
                                    supportCandidate.Normal = -convexManifold.Normal;
                                    supportCandidate.OffsetFromCharacter = offsetFromB;
                                    supportCandidate.OffsetFromSupport = deepestContact.Offset;
                                }
                                else
                                {
                                    supportCandidate.Normal = convexManifold.Normal;
                                    supportCandidate.OffsetFromCharacter = deepestContact.Offset;
                                    supportCandidate.OffsetFromSupport = offsetFromB;
                                }
                                supportCandidate.Support = supportCollidable;
                            }
                        }
                    }
                }
                else
                {
                    ref var nonconvexManifold = ref Unsafe.As<TManifold, NonconvexContactManifold>(ref manifold);
                    //The narrow phase generates contacts with normals pointing from B to A by convention.
                    //If the character is collidable B, then we need to negate the comparison.
                    //This manifold has a slope that is potentially supportive.
                    //Can the maximum depth contact be used as a support?
                    var maximumDepth = float.MinValue;
                    var maximumDepthIndex = -1;
                    for (int i = 0; i < nonconvexManifold.Count; ++i)
                    {
                        ref var candidate = ref Unsafe.Add(ref nonconvexManifold.Contact0, i);
                        if (candidate.Depth > maximumDepth)
                        {
                            //All the nonconvex candidates can have different normals, so we have to perform the (calibrated) normal test on every single one.
                            var upDot = Vector3.Dot(candidate.Normal, up);
                            if ((pair.B.Packed == characterCollidable.Packed ? -upDot : upDot) > character.CosMaximumSlope)
                            {
                                maximumDepth = candidate.Depth;
                                maximumDepthIndex = i;
                            }
                        }
                    }
                    if (maximumDepth >= character.MinimumSupportDepth || (character.Supported && maximumDepth > character.MinimumSupportContinuationDepth))
                    {
                        ref var supportCandidate = ref contactCollectionWorkerCaches[workerIndex].SupportCandidates[characterIndex];
                        if (supportCandidate.Depth < maximumDepth)
                        {
                            //This support candidate should be replaced.
                            ref var deepestContact = ref Unsafe.Add(ref nonconvexManifold.Contact0, maximumDepthIndex);
                            supportCandidate.Depth = maximumDepth;
                            var offsetFromB = deepestContact.Offset - nonconvexManifold.OffsetB;
                            if (pair.B.Packed == characterCollidable.Packed)
                            {
                                supportCandidate.Normal = -deepestContact.Normal;
                                supportCandidate.OffsetFromCharacter = offsetFromB;
                                supportCandidate.OffsetFromSupport = deepestContact.Offset;
                            }
                            else
                            {
                                supportCandidate.Normal = deepestContact.Normal;
                                supportCandidate.OffsetFromCharacter = deepestContact.Offset;
                                supportCandidate.OffsetFromSupport = offsetFromB;
                            }
                            supportCandidate.Support = supportCollidable;
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// Reports contacts about a collision to the character system. If the pair does not involve a character or there are no contacts, does nothing and returns false.
    /// </summary>
    /// <param name="pair">Pair of objects associated with the contact manifold.</param>
    /// <param name="manifold">Contact manifold between the colliding objects.</param>
    /// <param name="workerIndex">Index of the currently executing worker thread.</param>
    /// <param name="materialProperties">Material properties for this pair. Will be modified if the pair involves a character.</param>
    /// <returns>True if the pair involved a character pair and has contacts, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReportContacts<TManifold>(in CollidablePair pair, ref TManifold manifold, int workerIndex, ref PairMaterialProperties materialProperties) where TManifold : struct, IContactManifold<TManifold>
    {
        Debug.Assert(contactCollectionWorkerCaches.Allocated && workerIndex < contactCollectionWorkerCaches.Length && contactCollectionWorkerCaches[workerIndex].SupportCandidates.Allocated,
            "Worker caches weren't properly allocated; did you forget to call PrepareForContacts before collision detection?");
        if (manifold.Count == 0)
            return false;
        //It's possible for neither, one, or both collidables to be a character. Check each one, treating the other as a potential support.
        var aIsCharacter = TryReportContacts(pair.A, pair.B, pair, ref manifold, workerIndex);
        var bIsCharacter = TryReportContacts(pair.B, pair.A, pair, ref manifold, workerIndex);
        if (aIsCharacter || bIsCharacter)
        {
            //The character's motion over the surface should be controlled entirely by the horizontal motion constraint.
            //Note- you could use the friction coefficient to change the horizontal motion constraint's maximum force to simulate different environments if you want.
            //That would just require caching a bit more information for the AnalyzeContacts function to use.
            materialProperties.FrictionCoefficient = 0;
            return true;
        }
        return false;
    }

    Buffer<(int Start, int Count)> boundingBoxExpansionJobs;

    void ExpandBoundingBoxes(int start, int count)
    {
        var end = start + count;
        for (int i = start; i < end; ++i)
        {
            ref var character = ref characters[i];
            var characterBody = Simulation.Bodies[character.BodyHandle];
            if (characterBody.Awake)
            {
                Simulation.BroadPhase.GetActiveBoundsPointers(characterBody.Collidable.BroadPhaseIndex, out var min, out var max);
                QuaternionEx.Transform(character.LocalUp, characterBody.Pose.Orientation, out var characterUp);
                var supportExpansion = character.MinimumSupportContinuationDepth * characterUp;
                *min += Vector3.Min(Vector3.Zero, supportExpansion);
                *max += Vector3.Max(Vector3.Zero, supportExpansion);
            }
        }
    }

    int boundingBoxExpansionJobIndex;
    Action<int> expandBoundingBoxesWorker;

    void ExpandBoundingBoxesWorker(int workerIndex)
    {
        while (true)
        {
            var jobIndex = Interlocked.Increment(ref boundingBoxExpansionJobIndex);
            if (jobIndex < boundingBoxExpansionJobs.Length)
            {
                ref var job = ref boundingBoxExpansionJobs[jobIndex];
                ExpandBoundingBoxes(job.Start, job.Count);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Preallocates space for support data collected during the narrow phase. Should be called before the narrow phase executes.
    /// </summary>
    void PrepareForContacts(float dt, IThreadDispatcher threadDispatcher = null)
    {
        Debug.Assert(!contactCollectionWorkerCaches.Allocated, "Worker caches were already allocated; did you forget to call AnalyzeContacts after collision detection to flush the previous frame's results?");
        var threadCount = threadDispatcher == null ? 1 : threadDispatcher.ThreadCount;
        pool.Take(threadCount, out contactCollectionWorkerCaches);
        for (int i = 0; i < contactCollectionWorkerCaches.Length; ++i)
        {
            contactCollectionWorkerCaches[i] = new ContactCollectionWorkerCache(characters.Count, pool);
        }
        //While the character will retain support with contacts with depths above the MinimumSupportContinuationDepth if there was support in the previous frame,
        //it's possible for the contacts to be lost because the bounding box isn't expanded by MinimumSupportContinuationDepth and the broad phase doesn't see the support collidable.
        //Here, we expand the bounding boxes to compensate.
        if (threadCount == 1 || characters.Count < 256)
        {
            ExpandBoundingBoxes(0, characters.Count);
        }
        else
        {
            var jobCount = Math.Min(characters.Count, threadCount);
            var charactersPerJob = characters.Count / jobCount;
            var baseCharacterCount = charactersPerJob * jobCount;
            var remainder = characters.Count - baseCharacterCount;
            pool.Take(jobCount, out boundingBoxExpansionJobs);
            var previousEnd = 0;
            for (int jobIndex = 0; jobIndex < jobCount; ++jobIndex)
            {
                var charactersForJob = jobIndex < remainder ? charactersPerJob + 1 : charactersPerJob;
                ref var job = ref boundingBoxExpansionJobs[jobIndex];
                job.Start = previousEnd;
                job.Count = charactersForJob;
                previousEnd += job.Count;
            }

            boundingBoxExpansionJobIndex = -1;
            threadDispatcher.DispatchWorkers(expandBoundingBoxesWorker, boundingBoxExpansionJobs.Length);
            pool.Return(ref boundingBoxExpansionJobs);
        }
    }

    struct PendingDynamicConstraint
    {
        public int CharacterIndex;
        public DynamicCharacterMotionConstraint Description;
    }
    struct PendingStaticConstraint
    {
        public int CharacterIndex;
        public StaticCharacterMotionConstraint Description;
    }
    struct Jump
    {
        //Note that not every jump will contain a support body, so this can waste memory.
        //That's not really a concern- jumps are very rare (relatively speaking), so all we're wasting is capacity, not bandwidth.
        public int CharacterBodyIndex;
        public Vector3 CharacterVelocityChange;
        public int SupportBodyIndex;
        public Vector3 SupportImpulseOffset;
    }

    struct AnalyzeContactsWorkerCache
    {
        //The solver does not permit multithreaded removals and additions. We handle all of them in a sequential postpass.
        public QuickList<ConstraintHandle> ConstraintHandlesToRemove;
        public QuickList<PendingDynamicConstraint> DynamicConstraintsToAdd;
        public QuickList<PendingStaticConstraint> StaticConstraintsToAdd;
        public QuickList<Jump> Jumps;

        public AnalyzeContactsWorkerCache(int maximumCharacterCount, BufferPool pool)
        {
            ConstraintHandlesToRemove = new QuickList<ConstraintHandle>(maximumCharacterCount, pool);
            DynamicConstraintsToAdd = new QuickList<PendingDynamicConstraint>(maximumCharacterCount, pool);
            StaticConstraintsToAdd = new QuickList<PendingStaticConstraint>(maximumCharacterCount, pool);
            Jumps = new QuickList<Jump>(maximumCharacterCount, pool);
        }

        public void Dispose(BufferPool pool)
        {
            ConstraintHandlesToRemove.Dispose(pool);
            DynamicConstraintsToAdd.Dispose(pool);
            StaticConstraintsToAdd.Dispose(pool);
            Jumps.Dispose(pool);
        }
    }

    Buffer<AnalyzeContactsWorkerCache> analyzeContactsWorkerCaches;

    void AnalyzeContactsForCharacterRegion(int start, int exclusiveEnd, int workerIndex)
    {
        ref var analyzeContactsWorkerCache = ref analyzeContactsWorkerCaches[workerIndex];
        for (int characterIndex = start; characterIndex < exclusiveEnd; ++characterIndex)
        {
            //Note that this iterates over both active and inactive characters rather than segmenting inactive characters into their own collection.
            //This demands branching, but the expectation is that the vast majority of characters will be active, so there is less value in copying them into stasis.
            ref var character = ref characters[characterIndex];
            ref var bodyLocation = ref Simulation.Bodies.HandleToLocation[character.BodyHandle.Value];
            if (bodyLocation.SetIndex == 0)
            {
                var supportCandidate = contactCollectionWorkerCaches[0].SupportCandidates[characterIndex];
                for (int j = 1; j < contactCollectionWorkerCaches.Length; ++j)
                {
                    ref var workerCandidate = ref contactCollectionWorkerCaches[j].SupportCandidates[characterIndex];
                    if (workerCandidate.Depth > supportCandidate.Depth)
                    {
                        supportCandidate = workerCandidate;
                    }
                }
                //We need to protect against one possible corner case: if the body supporting the character was removed, the associated motion constraint was also removed.
                //Arbitrarily un-support the character if we detect this.
                if (character.Supported)
                {
                    //If the constraint no longer exists at all,
                    if (!Simulation.Solver.ConstraintExists(character.MotionConstraintHandle) ||
                        //or if the constraint does exist but is now used by a different constraint type,
                        (Simulation.Solver.HandleToConstraint[character.MotionConstraintHandle.Value].TypeId != DynamicCharacterMotionTypeProcessor.BatchTypeId &&
                         Simulation.Solver.HandleToConstraint[character.MotionConstraintHandle.Value].TypeId != StaticCharacterMotionTypeProcessor.BatchTypeId))
                    {
                        //then the character isn't actually supported anymore.
                        character.Supported = false;
                    }
                    //Note that it's sufficient to only check that the type matches the dynamic motion constraint type id because no other systems ever create dynamic character motion constraints.
                    //Other systems may result in the constraint's removal, but no other system will ever *create* it.
                    //Further, during this analysis loop, we do not create any constraints. We only set up pending additions to be processed after the multithreaded analysis completes.
                }

                //The body is active. We may need to remove the associated constraint from the solver. Remove if any of the following hold:
                //1) The character was previously supported but is no longer.
                //2) The character was previously supported by a body, and is now supported by a different body.
                //3) The character was previously supported by a static, and is now supported by a body.
                //4) The character was previously supported by a body, and is now supported by a static.
                var shouldRemove = character.Supported && (character.TryJump || supportCandidate.Depth == float.MinValue || character.Support.Packed != supportCandidate.Support.Packed);
                if (shouldRemove)
                {
                    //Mark the constraint for removal.
                    analyzeContactsWorkerCache.ConstraintHandlesToRemove.AllocateUnsafely() = character.MotionConstraintHandle;
                }

                //If the character is jumping, don't create a constraint.
                if (supportCandidate.Depth > float.MinValue && character.TryJump)
                {
                    QuaternionEx.Transform(character.LocalUp, Simulation.Bodies.ActiveSet.DynamicsState[bodyLocation.Index].Motion.Pose.Orientation, out var characterUp);
                    //Note that we assume that character orientations are constant. This isn't necessarily the case in all uses, but it's a decent approximation.
                    var characterUpVelocity = Vector3.Dot(Simulation.Bodies.ActiveSet.DynamicsState[bodyLocation.Index].Motion.Velocity.Linear, characterUp);
                    //We don't want the character to be able to 'superboost' by simply adding jump speed on top of horizontal motion.
                    //Instead, jumping targets a velocity change necessary to reach character.JumpVelocity along the up axis.
                    if (character.Support.Mobility != CollidableMobility.Static)
                    {
                        ref var supportingBodyLocation = ref Simulation.Bodies.HandleToLocation[character.Support.BodyHandle.Value];
                        Debug.Assert(supportingBodyLocation.SetIndex == 0, "If the character is active, any support should be too.");
                        ref var supportVelocity = ref Simulation.Bodies.ActiveSet.DynamicsState[supportingBodyLocation.Index].Motion.Velocity;
                        var wxr = Vector3.Cross(supportVelocity.Angular, supportCandidate.OffsetFromSupport);
                        var supportContactVelocity = supportVelocity.Linear + wxr;
                        var supportUpVelocity = Vector3.Dot(supportContactVelocity, characterUp);

                        //If the support is dynamic, apply an opposing impulse. Note that velocity changes cannot safely be applied during multithreaded execution;
                        //characters could share support bodies, and a character might be a support of another character.
                        //That's really not concerning from a performance perspective- characters don't jump many times per frame.
                        ref var jump = ref analyzeContactsWorkerCache.Jumps.AllocateUnsafely();
                        jump.CharacterBodyIndex = bodyLocation.Index;
                        jump.CharacterVelocityChange = characterUp * MathF.Max(0, character.JumpVelocity - (characterUpVelocity - supportUpVelocity));
                        if (character.Support.Mobility == CollidableMobility.Dynamic)
                        {
                            jump.SupportBodyIndex = supportingBodyLocation.Index;
                            jump.SupportImpulseOffset = supportCandidate.OffsetFromSupport;
                        }
                        else
                        {
                            //No point in applying impulses to kinematics.
                            jump.SupportBodyIndex = -1;
                        }
                    }
                    else
                    {
                        //Static bodies have no velocity, so we don't have to consider the support.
                        ref var jump = ref analyzeContactsWorkerCache.Jumps.AllocateUnsafely();
                        jump.CharacterBodyIndex = bodyLocation.Index;
                        jump.CharacterVelocityChange = characterUp * MathF.Max(0, character.JumpVelocity - characterUpVelocity);
                        jump.SupportBodyIndex = -1;
                    }
                    character.Supported = false;
                }
                else if (supportCandidate.Depth > float.MinValue)
                {
                    //If a support currently exists and there is still an old constraint, then update it.
                    //If a support currently exists and there is not an old constraint, add the new constraint.

                    //Project the view direction down onto the surface as represented by the contact normal.
                    Matrix3x3 surfaceBasis;
                    surfaceBasis.Y = supportCandidate.Normal;
                    //Note negation: we're using a right handed basis where -Z is forward, +Z is backward.
                    QuaternionEx.Transform(character.LocalUp, Simulation.Bodies.ActiveSet.DynamicsState[bodyLocation.Index].Motion.Pose.Orientation, out var up);
                    var rayDistance = Vector3.Dot(character.ViewDirection, surfaceBasis.Y);
                    var rayVelocity = Vector3.Dot(up, surfaceBasis.Y);
                    Debug.Assert(rayVelocity > 0,
                        "The calibrated support normal and the character's up direction should have a positive dot product if the maximum slope is working properly. Is the maximum slope >= pi/2?");
                    surfaceBasis.Z = up * (rayDistance / rayVelocity) - character.ViewDirection;
                    var zLengthSquared = surfaceBasis.Z.LengthSquared();
                    if (zLengthSquared > 1e-12f)
                    {
                        surfaceBasis.Z /= MathF.Sqrt(zLengthSquared);
                    }
                    else
                    {
                        QuaternionEx.GetQuaternionBetweenNormalizedVectors(Vector3.UnitY, surfaceBasis.Y, out var rotation);
                        QuaternionEx.TransformUnitZ(rotation, out surfaceBasis.Z);
                    }
                    surfaceBasis.X = Vector3.Cross(surfaceBasis.Y, surfaceBasis.Z);
                    QuaternionEx.CreateFromRotationMatrix(surfaceBasis, out var surfaceBasisQuaternion);
                    if (supportCandidate.Support.Mobility != CollidableMobility.Static)
                    {
                        //The character is supported by a body.
                        var motionConstraint = new DynamicCharacterMotionConstraint
                        {
                            MaximumHorizontalForce = character.MaximumHorizontalForce,
                            MaximumVerticalForce = character.MaximumVerticalForce,
                            OffsetFromCharacterToSupportPoint = supportCandidate.OffsetFromCharacter,
                            OffsetFromSupportToSupportPoint = supportCandidate.OffsetFromSupport,
                            SurfaceBasis = surfaceBasisQuaternion,
                            TargetVelocity = character.TargetVelocity,
                            Depth = supportCandidate.Depth
                        };
                        if (character.Supported && !shouldRemove)
                        {
                            //Already exists, update it.
                            Simulation.Solver.ApplyDescriptionWithoutWaking(character.MotionConstraintHandle, motionConstraint);
                        }
                        else
                        {
                            //Doesn't exist, mark it for addition.
                            ref var pendingConstraint = ref analyzeContactsWorkerCache.DynamicConstraintsToAdd.AllocateUnsafely();
                            pendingConstraint.Description = motionConstraint;
                            pendingConstraint.CharacterIndex = characterIndex;
                        }
                    }
                    else
                    {
                        //The character is supported by a static.
                        var motionConstraint = new StaticCharacterMotionConstraint
                        {
                            MaximumHorizontalForce = character.MaximumHorizontalForce,
                            MaximumVerticalForce = character.MaximumVerticalForce,
                            OffsetFromCharacterToSupportPoint = supportCandidate.OffsetFromCharacter,
                            SurfaceBasis = surfaceBasisQuaternion,
                            TargetVelocity = character.TargetVelocity,
                            Depth = supportCandidate.Depth
                        };
                        if (character.Supported && !shouldRemove)
                        {
                            //Already exists, update it.
                            Simulation.Solver.ApplyDescriptionWithoutWaking(character.MotionConstraintHandle, motionConstraint);
                        }
                        else
                        {
                            //Doesn't exist, mark it for addition.
                            ref var pendingConstraint = ref analyzeContactsWorkerCache.StaticConstraintsToAdd.AllocateUnsafely();
                            pendingConstraint.Description = motionConstraint;
                            pendingConstraint.CharacterIndex = characterIndex;
                        }
                    }
                    character.Supported = true;
                    character.Support = supportCandidate.Support;
                }
                else
                {
                    character.Supported = false;
                }
            }
            //The TryJump flag is always reset even if the attempt failed.
            character.TryJump = false;
        }
    }

    struct AnalyzeContactsJob
    {
        public int Start;
        public int ExclusiveEnd;
    }

    int analysisJobIndex;
    int analysisJobCount;
    Buffer<AnalyzeContactsJob> jobs;
    Action<int> analyzeContactsWorker;

    void AnalyzeContactsWorker(int workerIndex)
    {
        int jobIndex;
        while ((jobIndex = Interlocked.Increment(ref analysisJobIndex)) < analysisJobCount)
        {
            ref var job = ref jobs[jobIndex];
            AnalyzeContactsForCharacterRegion(job.Start, job.ExclusiveEnd, workerIndex);
        }
    }


    /// <summary>
    /// Updates all character support states and motion constraints based on the current character goals and all the contacts collected since the last call to AnalyzeContacts.
    /// Attach to a simulation callback where the most recent contact is available and before the solver executes.
    /// </summary>
    void AnalyzeContacts(float dt, IThreadDispatcher threadDispatcher)
    {
        //var start = Stopwatch.GetTimestamp();
        Debug.Assert(contactCollectionWorkerCaches.Allocated, "Worker caches weren't properly allocated; did you forget to call PrepareForContacts before collision detection?");

        if (threadDispatcher == null)
        {
            pool.Take(1, out analyzeContactsWorkerCaches);
            analyzeContactsWorkerCaches[0] = new AnalyzeContactsWorkerCache(characters.Count, pool);
            AnalyzeContactsForCharacterRegion(0, characters.Count, 0);
        }
        else
        {
            analysisJobCount = Math.Min(characters.Count, threadDispatcher.ThreadCount * 4);
            if (analysisJobCount > 0)
            {
                pool.Take(threadDispatcher.ThreadCount, out analyzeContactsWorkerCaches);
                pool.Take(analysisJobCount, out jobs);
                for (int i = 0; i < threadDispatcher.ThreadCount; ++i)
                {
                    analyzeContactsWorkerCaches[i] = new AnalyzeContactsWorkerCache(characters.Count, pool);
                }
                var baseCount = characters.Count / analysisJobCount;
                var remainder = characters.Count - baseCount * analysisJobCount;
                var previousEnd = 0;
                for (int i = 0; i < analysisJobCount; ++i)
                {
                    ref var job = ref jobs[i];
                    job.Start = previousEnd;
                    job.ExclusiveEnd = job.Start + (i < remainder ? baseCount + 1 : baseCount);
                    previousEnd = job.ExclusiveEnd;
                }
                analysisJobIndex = -1;
                threadDispatcher.DispatchWorkers(analyzeContactsWorker, analysisJobCount);
                pool.Return(ref jobs);
            }
        }
        //We're done with all the contact collection worker caches.
        for (int i = 0; i < contactCollectionWorkerCaches.Length; ++i)
        {
            contactCollectionWorkerCaches[i].Dispose(pool);
        }
        pool.Return(ref contactCollectionWorkerCaches);

        if (analyzeContactsWorkerCaches.Allocated)
        {
            //Flush all the worker caches. Note that we perform all removals before moving onto any additions to avoid unnecessary constraint batches
            //caused by the new and old constraint affecting the same bodies.
            for (int threadIndex = 0; threadIndex < analyzeContactsWorkerCaches.Length; ++threadIndex)
            {
                ref var cache = ref analyzeContactsWorkerCaches[threadIndex];
                for (int i = 0; i < cache.ConstraintHandlesToRemove.Count; ++i)
                {
                    Simulation.Solver.Remove(cache.ConstraintHandlesToRemove[i]);
                }
            }
            for (int threadIndex = 0; threadIndex < analyzeContactsWorkerCaches.Length; ++threadIndex)
            {
                ref var workerCache = ref analyzeContactsWorkerCaches[threadIndex];
                for (int i = 0; i < workerCache.StaticConstraintsToAdd.Count; ++i)
                {
                    ref var pendingConstraint = ref workerCache.StaticConstraintsToAdd[i];
                    ref var character = ref characters[pendingConstraint.CharacterIndex];
                    Debug.Assert(character.Support.Mobility == CollidableMobility.Static);
                    character.MotionConstraintHandle = Simulation.Solver.Add(character.BodyHandle, pendingConstraint.Description);
                }
                for (int i = 0; i < workerCache.DynamicConstraintsToAdd.Count; ++i)
                {
                    ref var pendingConstraint = ref workerCache.DynamicConstraintsToAdd[i];
                    ref var character = ref characters[pendingConstraint.CharacterIndex];
                    Debug.Assert(character.Support.Mobility != CollidableMobility.Static);
                    character.MotionConstraintHandle = Simulation.Solver.Add(character.BodyHandle, character.Support.BodyHandle, pendingConstraint.Description);
                }
                ref var activeSet = ref Simulation.Bodies.ActiveSet;
                for (int i = 0; i < workerCache.Jumps.Count; ++i)
                {
                    ref var jump = ref workerCache.Jumps[i];
                    activeSet.DynamicsState[jump.CharacterBodyIndex].Motion.Velocity.Linear += jump.CharacterVelocityChange;
                    if (jump.SupportBodyIndex >= 0)
                    {
                        BodyReference.ApplyImpulse(Simulation.Bodies.ActiveSet, jump.SupportBodyIndex, jump.CharacterVelocityChange / -activeSet.DynamicsState[jump.CharacterBodyIndex].Inertia.Local.InverseMass, jump.SupportImpulseOffset);
                    }
                }
                workerCache.Dispose(pool);
            }
            pool.Return(ref analyzeContactsWorkerCaches);
        }

        //var end = Stopwatch.GetTimestamp();
        //Console.WriteLine($"Time (ms): {(end - start) / (1e-3 * Stopwatch.Frequency)}");
    }

    /// <summary>
    /// Ensures that the internal structures of the character controllers system can handle the given number of characters and body handles, resizing if necessary.
    /// </summary>
    /// <param name="characterCapacity">Minimum character capacity to require.</param>
    /// <param name="bodyHandleCapacity">Minimum number of body handles to allocate space for.</param>
    public void EnsureCapacity(int characterCapacity, int bodyHandleCapacity)
    {
        characters.EnsureCapacity(characterCapacity, pool);
        if (bodyHandleToCharacterIndex.Length < bodyHandleCapacity)
        {
            ResizeBodyHandleCapacity(bodyHandleCapacity);
        }
    }

    /// <summary>
    /// Resizes the internal structures of the character controllers system for the target sizes. Will not shrink below the currently active data size.
    /// </summary>
    /// <param name="characterCapacity">Target character capacity to allocate space for.</param>
    /// <param name="bodyHandleCapacity">Target number of body handles to allocate space for.</param>
    public void Resize(int characterCapacity, int bodyHandleCapacity)
    {
        int lastOccupiedIndex = -1;
        for (int i = bodyHandleToCharacterIndex.Length - 1; i >= 0; --i)
        {
            if (bodyHandleToCharacterIndex[i] != -1)
            {
                lastOccupiedIndex = i;
                break;
            }
        }
        var targetHandleCapacity = BufferPool.GetCapacityForCount<int>(Math.Max(lastOccupiedIndex + 1, bodyHandleCapacity));
        if (targetHandleCapacity != bodyHandleToCharacterIndex.Length)
            ResizeBodyHandleCapacity(targetHandleCapacity);

        var targetCharacterCapacity = BufferPool.GetCapacityForCount<int>(Math.Max(characters.Count, characterCapacity));
        if (targetCharacterCapacity != characters.Span.Length)
            characters.Resize(targetCharacterCapacity, pool);
    }

    bool disposed;
    /// <summary>
    /// Returns pool-allocated resources.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Simulation.Timestepper.BeforeCollisionDetection -= PrepareForContacts;
            Simulation.Timestepper.CollisionsDetected -= AnalyzeContacts;
            characters.Dispose(pool);
            pool.Return(ref bodyHandleToCharacterIndex);
        }
    }
}

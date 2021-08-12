// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;
using Stride.Rendering;
using static BulletSharp.UnsafeNativeMethods;

namespace Stride.Physics
{
    public class Simulation : IDisposable
    {
        const CollisionFilterGroups DefaultGroup = (CollisionFilterGroups)BulletSharp.CollisionFilterGroups.DefaultFilter;
        const CollisionFilterGroupFlags DefaultFlags = (CollisionFilterGroupFlags)BulletSharp.CollisionFilterGroups.AllFilter;

        private readonly PhysicsProcessor processor;

        private readonly BulletSharp.DiscreteDynamicsWorld discreteDynamicsWorld;
        private readonly BulletSharp.CollisionWorld collisionWorld;

        private readonly BulletSharp.CollisionDispatcher dispatcher;
        private readonly BulletSharp.CollisionConfiguration collisionConfiguration;
        private readonly BulletSharp.DbvtBroadphase broadphase;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly BulletSharp.ContactSolverInfo solverInfo;

        private readonly BulletSharp.DispatcherInfo dispatchInfo;

        internal readonly bool CanCcd;

#if DEBUG
        private static readonly Logger Log = GlobalLogger.GetLogger(typeof(Simulation).FullName);
#endif

        public bool ContinuousCollisionDetection
        {
            get
            {
                if (!CanCcd)
                {
                    throw new Exception("ContinuousCollisionDetection must be enabled at physics engine initialization using the proper flag.");
                }

                return dispatchInfo.UseContinuous;
            }
            set
            {
                if (!CanCcd)
                {
                    throw new Exception("ContinuousCollisionDetection must be enabled at physics engine initialization using the proper flag.");
                }

                dispatchInfo.UseContinuous = value;
            }
        }

        /// <summary>
        /// Totally disable the simulation if set to true
        /// </summary>
        public static bool DisableSimulation = false;

        public delegate PhysicsEngineFlags OnSimulationCreationDelegate();

        /// <summary>
        /// Temporary solution to inject engine flags
        /// </summary>
        public static OnSimulationCreationDelegate OnSimulationCreation;

        /// <summary>
        /// Initializes the Physics engine using the specified flags.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="configuration"></param>
        /// <exception cref="System.NotImplementedException">SoftBody processing is not yet available</exception>
        internal Simulation(PhysicsProcessor processor, PhysicsSettings configuration)
        {
            this.processor = processor;

            if (configuration.Flags == PhysicsEngineFlags.None)
            {
                configuration.Flags = OnSimulationCreation?.Invoke() ?? configuration.Flags;
            }

            MaxSubSteps = configuration.MaxSubSteps;
            FixedTimeStep = configuration.FixedTimeStep;

            collisionConfiguration = new BulletSharp.DefaultCollisionConfiguration();
            dispatcher = new BulletSharp.CollisionDispatcher(collisionConfiguration);
            broadphase = new BulletSharp.DbvtBroadphase();

            //this allows characters to have proper physics behavior
            broadphase.OverlappingPairCache.SetInternalGhostPairCallback(new BulletSharp.GhostPairCallback());

            //2D pipeline
            var simplex = new BulletSharp.VoronoiSimplexSolver();
            var pdSolver = new BulletSharp.MinkowskiPenetrationDepthSolver();
            var convexAlgo = new BulletSharp.Convex2DConvex2DAlgorithm.CreateFunc(simplex, pdSolver);

            dispatcher.RegisterCollisionCreateFunc(BulletSharp.BroadphaseNativeType.Convex2DShape, BulletSharp.BroadphaseNativeType.Convex2DShape, convexAlgo);
            //dispatcher.RegisterCollisionCreateFunc(BulletSharp.BroadphaseNativeType.Box2DShape, BulletSharp.BroadphaseNativeType.Convex2DShape, convexAlgo);
            //dispatcher.RegisterCollisionCreateFunc(BulletSharp.BroadphaseNativeType.Convex2DShape, BulletSharp.BroadphaseNativeType.Box2DShape, convexAlgo);
            //dispatcher.RegisterCollisionCreateFunc(BulletSharp.BroadphaseNativeType.Box2DShape, BulletSharp.BroadphaseNativeType.Box2DShape, new BulletSharp.Box2DBox2DCollisionAlgorithm.CreateFunc());
            //~2D pipeline

            //default solver
            var solver = new BulletSharp.SequentialImpulseConstraintSolver();

            if (configuration.Flags.HasFlag(PhysicsEngineFlags.CollisionsOnly))
            {
                collisionWorld = new BulletSharp.CollisionWorld(dispatcher, broadphase, collisionConfiguration);
            }
            else if (configuration.Flags.HasFlag(PhysicsEngineFlags.SoftBodySupport))
            {
                //mSoftRigidDynamicsWorld = new BulletSharp.SoftBody.SoftRigidDynamicsWorld(mDispatcher, mBroadphase, solver, mCollisionConf);
                //mDiscreteDynamicsWorld = mSoftRigidDynamicsWorld;
                //mCollisionWorld = mSoftRigidDynamicsWorld;
                throw new NotImplementedException("SoftBody processing is not yet available");
            }
            else
            {
                discreteDynamicsWorld = new BulletSharp.DiscreteDynamicsWorld(dispatcher, broadphase, solver, collisionConfiguration);
                collisionWorld = discreteDynamicsWorld;
            }

            if (discreteDynamicsWorld != null)
            {
                solverInfo = discreteDynamicsWorld.SolverInfo; //we are required to keep this reference, or the GC will mess up
                dispatchInfo = discreteDynamicsWorld.DispatchInfo;

                solverInfo.SolverMode |= BulletSharp.SolverModes.CacheFriendly; //todo test if helps with performance or not

                if (configuration.Flags.HasFlag(PhysicsEngineFlags.ContinuousCollisionDetection))
                {
                    CanCcd = true;
                    solverInfo.SolverMode |= BulletSharp.SolverModes.Use2FrictionDirections | BulletSharp.SolverModes.RandomizeOrder;
                    dispatchInfo.UseContinuous = true;
                }
                else
                {
                    CanCcd = false;
                    dispatchInfo.UseContinuous = false;
                }
            }
        }


        private readonly Dictionary<Collision, (IntPtr, IntPtr)> collisions = new();
        private readonly Dictionary<(IntPtr, IntPtr), Collision> outdatedCollisions = new();

        private readonly Stack<Channel<HashSet<ContactPoint>>> channelsPool = new();
        private readonly Dictionary<Collision, (Channel<HashSet<ContactPoint>> Channel, HashSet<ContactPoint> PreviousContacts)> contactChangedChannels = new();

        private readonly Stack<HashSet<ContactPoint>> contactsPool = new();
        private readonly Dictionary<Collision, HashSet<ContactPoint>> contactsUpToDate = new();

        private readonly List<Collision> markedAsNewColl = new();
        private readonly List<Collision> markedAsDeprecatedColl = new();
        internal readonly HashSet<Collision> EndedFromComponentRemoval = new();

        /// <summary>
        /// Every pair of components currently colliding with each other
        /// </summary>
        public ICollection<Collision> CurrentCollisions => collisions.Keys;
        
        /// <summary>
        /// Should static - static collisions of StaticColliderComponent yield
        /// <see cref="PhysicsComponent"/>.<see cref="PhysicsComponent.NewCollision()"/> and added to
        /// <see cref="PhysicsComponent"/>.<see cref="PhysicsComponent.Collisions"/> ?
        /// </summary>
        /// <remarks>
        /// Regardless of the state of this value you can still retrieve static-static collisions
        /// through <see cref="CurrentCollisions"/>.
        /// </remarks>
        public bool IncludeStaticAgainstStaticCollisions { get; set; } = false;


        internal void UpdateContacts()
        {
            EndedFromComponentRemoval.Clear();
            // Mark previous collisions as outdated,
            // we'll iterate through bullet's actives and remove them from here
            // to be left with only the outdated ones.
            foreach (var collision in collisions)
            {
                outdatedCollisions.Add(collision.Value, collision.Key);
            }

            // If this needs to be even faster, look into btPersistentManifold.ContactStartedCallback,
            // not yet covered by the wrapper

            int numManifolds = collisionWorld.Dispatcher.NumManifolds;
            var dispatcherNativePtr = collisionWorld.Dispatcher.Native;
            for (int i = 0; i < numManifolds; i++)
            {
                var persManifoldPtr = btDispatcher_getManifoldByIndexInternal(dispatcherNativePtr, i);

                int numContacts = btPersistentManifold_getNumContacts(persManifoldPtr);
                if (numContacts == 0)
                    continue;
                
                var ptrA = btPersistentManifold_getBody0(persManifoldPtr);
                var ptrB = btPersistentManifold_getBody1(persManifoldPtr);
                bool aFirst;
                unsafe { aFirst = ptrA.ToPointer() > ptrB.ToPointer(); }
                (IntPtr, IntPtr) collId = aFirst ? (ptrA, ptrB) : (ptrB, ptrA);

                // This collision is up-to-date, remove it from the outdated collisions
                if (outdatedCollisions.Remove(collId))
                    continue;
                
                // Likely a new collision, or a duplicate
                
                var a = BulletSharp.CollisionObject.GetManaged(collId.Item1);
                var b = BulletSharp.CollisionObject.GetManaged(collId.Item2);
                var collision = new Collision(a.UserObject as PhysicsComponent, b.UserObject as PhysicsComponent);
                // PairCachingGhostObject has two identical manifolds when colliding, not 100% sure why that is,
                // CompoundColliderShape shapes all map to the same PhysicsComponent but create unique manifolds.
                if (collisions.TryAdd(collision, collId))
                {
                    markedAsNewColl.Add(collision);
                }
            }

            // This set only contains outdated collisions by now,
            // mark them as out of date for events and remove them from current collisions
            foreach (var (_, outdatedCollision) in outdatedCollisions)
            {
                markedAsDeprecatedColl.Add(outdatedCollision);
                collisions.Remove(outdatedCollision);
            }

            outdatedCollisions.Clear();
        }


        internal void ClearCollisionDataOf(PhysicsComponent component)
        {
            foreach (var (collision, key) in collisions)
            {
                if (ReferenceEquals(collision.ColliderA, component) || ReferenceEquals(collision.ColliderB, component))
                {
                    outdatedCollisions.Add(key, collision);
                    EndedFromComponentRemoval.Add(collision);
                }
            }

            // Remove collision and update contact data
            foreach (var (_, collision) in outdatedCollisions)
            {
                if (contactChangedChannels.TryGetValue(collision, out var tuple))
                {
                    contactChangedChannels[collision] = (tuple.Channel, LatestContactPointsFor(collision));
                    contactsUpToDate[collision] = contactsPool.Count == 0 ? new HashSet<ContactPoint>() : contactsPool.Pop();
                }
                else if (contactsUpToDate.TryGetValue(collision, out var set))
                {
                    set.Clear();
                }

                collisions.Remove(collision);
            }

            // Send contacts changed and cleanup channel-related pooled data
            foreach (var (_, collision) in outdatedCollisions)
            {
                if (contactChangedChannels.TryGetValue(collision, out var tuple) == false)
                    continue;

                var channel = tuple.Channel;
                var previousContacts = tuple.PreviousContacts;
                var newContacts = contactsUpToDate[collision];

                if (previousContacts.SetEquals(newContacts) == false)
                {
                    while (channel.Balance < 0)
                    {
                        channel.Send(previousContacts);
                    }
                }

                previousContacts.Clear();
                contactsPool.Push(previousContacts);
                channelsPool.Push(channel);
                contactChangedChannels.Remove(collision);
            }

            // Send collision ended
            foreach (var (_, refCollision) in outdatedCollisions)
            {
                var collision = new Collision(refCollision.ColliderA, refCollision.ColliderB);
                // See: SendEvents()
                if (IncludeStaticAgainstStaticCollisions == false
                    && collision.ColliderA is StaticColliderComponent
                    && collision.ColliderB is StaticColliderComponent)
                {
                    collision.ColliderA.Collisions.Remove( collision );
                    collision.ColliderB.Collisions.Remove( collision );
                    continue;
                }
                
                while (collision.ColliderA.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderA.PairEndedChannel.Send(collision);
                }

                while (collision.ColliderB.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderB.PairEndedChannel.Send(collision);
                }
                collision.ColliderA.Collisions.Remove( collision );
                collision.ColliderB.Collisions.Remove( collision );
            }

            outdatedCollisions.Clear();
        }


        internal void SendEvents()
        {
            int previousSets = 0;
            // Move outdated contacts back to the pool, or into contact changed to be compared for changes
            foreach (var (coll, hashset) in contactsUpToDate)
            {
                if (contactChangedChannels.TryGetValue(coll, out var tuple))
                {
                    contactChangedChannels[coll] = (tuple.Channel, hashset);
                    previousSets++;
                }
                else
                {
                    hashset.Clear();
                    contactsPool.Push(hashset);
                }
            }

            contactsUpToDate.Clear();

            if (previousSets != contactChangedChannels.Count)
            {
                throw new InvalidOperationException($"All {nameof(contactChangedChannels)} should have hashsets associated to them");
            }

            foreach (var collision in markedAsNewColl)
            {
                if (IncludeStaticAgainstStaticCollisions == false
                    && collision.ColliderA is StaticColliderComponent
                    && collision.ColliderB is StaticColliderComponent)
                {
                    continue;
                }

                collision.ColliderA.Collisions.Add( collision );
                collision.ColliderB.Collisions.Add( collision );
                
                while (collision.ColliderA.NewPairChannel.Balance < 0)
                {
                    collision.ColliderA.NewPairChannel.Send(collision);
                }

                while (collision.ColliderB.NewPairChannel.Balance < 0)
                {
                    collision.ColliderB.NewPairChannel.Send(collision);
                }
            }

            foreach (var (collision, (channel, previousContacts)) in contactChangedChannels)
            {
                var newContacts = LatestContactPointsFor(collision);

                if (previousContacts.SetEquals(newContacts) == false)
                {
                    while (channel.Balance < 0)
                    {
                        channel.Send(previousContacts);
                    }
                }

                previousContacts.Clear();
                contactsPool.Push(previousContacts);
            }

            // Deprecated collisions don't need to send contact changes, move channels to the pool
            foreach (var collision in markedAsDeprecatedColl)
            {
                if (contactChangedChannels.TryGetValue(collision, out var tuple))
                {
                    channelsPool.Push(tuple.Channel);
                    contactChangedChannels.Remove(collision);
                }
            }

            foreach (var collision in markedAsDeprecatedColl)
            {
                if (IncludeStaticAgainstStaticCollisions == false
                    && collision.ColliderA is StaticColliderComponent
                    && collision.ColliderB is StaticColliderComponent)
                {
                    // Try to remove them still if they were added while
                    // 'IncludeStaticAgainstStaticCollisions' was true
                    collision.ColliderA.Collisions.Remove( collision );
                    collision.ColliderB.Collisions.Remove( collision );
                    continue;
                }
                
                // IncludeStaticAgainstStaticCollisions:
                // Can't do much if something is awaiting the end of a specific
                // static-static collision below though
                while (collision.ColliderA.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderA.PairEndedChannel.Send(collision);
                }

                while (collision.ColliderB.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderB.PairEndedChannel.Send(collision);
                }
                
                collision.ColliderA.Collisions.Remove( collision );
                collision.ColliderB.Collisions.Remove( collision );
            }

            markedAsNewColl.Clear();
            markedAsDeprecatedColl.Clear();

            // Mark un-awaited channels for removal
            foreach (var (collision, (channel, _)) in contactChangedChannels)
            {
                if (channel.Balance < 0)
                    continue;

                markedAsDeprecatedColl.Add(collision);
                channelsPool.Push(channel);
            }

            foreach (var collision in markedAsDeprecatedColl)
            {
                contactChangedChannels.Remove(collision);
            }

            markedAsDeprecatedColl.Clear();
        }


        internal HashSet<ContactPoint> LatestContactPointsFor(Collision coll)
        {
            if (contactsUpToDate.TryGetValue(coll, out var buffer))
                return buffer;

            buffer = contactsPool.Count == 0 ? new HashSet<ContactPoint>() : contactsPool.Pop();
            contactsUpToDate[coll] = buffer;

            if (collisions.ContainsKey(coll) == false)
                return buffer;

            int numManifolds = collisionWorld.Dispatcher.NumManifolds;
            var dispatcherNativePtr = collisionWorld.Dispatcher.Native;
            for (int i = 0; i < numManifolds; i++)
            {
                var persManifoldPtr = btDispatcher_getManifoldByIndexInternal(dispatcherNativePtr, i);

                int numContacts = btPersistentManifold_getNumContacts(persManifoldPtr);
                if (numContacts == 0)
                    continue;
                
                var ptrA = btPersistentManifold_getBody0(persManifoldPtr);
                var ptrB = btPersistentManifold_getBody1(persManifoldPtr);

                // Distinct bullet pointer can map to the same PhysicsComponent through CompoundColliderShapes
                // We're retrieving all contacts for a pair of PhysicsComponent here, not for a unique collider
                var collA = BulletSharp.CollisionObject.GetManaged(ptrA).UserObject as PhysicsComponent;
                var collB = BulletSharp.CollisionObject.GetManaged(ptrB).UserObject as PhysicsComponent;
                
                if (false == (coll.ColliderA == collA && coll.ColliderB == collB 
                              || coll.ColliderA == collB && coll.ColliderB == collA))
                {
                    continue;
                }
                
                for (int j = 0; j < numContacts; j++)
                {
                    var point = BulletSharp.ManifoldPoint.FromPtr(btPersistentManifold_getContactPoint(persManifoldPtr, j));
                    buffer.Add(new ContactPoint
                    {
                        ColliderA = collA,
                        ColliderB = collB,
                        Distance = point.m_distance1,
                        Normal = point.m_normalWorldOnB,
                        PositionOnA = point.m_positionWorldOnA,
                        PositionOnB = point.m_positionWorldOnB,
                    });
                }
            }

            return buffer;
        }


        internal ChannelMicroThreadAwaiter<HashSet<ContactPoint>> ContactChanged(Collision coll)
        {
            if (collisions.ContainsKey(coll) == false)
                throw new InvalidOperationException("The collision object has been destroyed.");

            // Forces this frame's contact to be retrieved and stored so that we can compare it for changes
            LatestContactPointsFor(coll);

            if (contactChangedChannels.TryGetValue(coll, out var tuple))
                return tuple.Channel.Receive();

            var channel = channelsPool.Count == 0 ? new Channel<HashSet<ContactPoint>>{ Preference = ChannelPreference.PreferSender } : channelsPool.Pop();
            contactChangedChannels[coll] = (channel, null);
            return channel.Receive();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //if (mSoftRigidDynamicsWorld != null) mSoftRigidDynamicsWorld.Dispose();
            if (discreteDynamicsWorld != null)
            {
                discreteDynamicsWorld.Dispose();
            }
            else
            {
                collisionWorld?.Dispose();
            }

            broadphase?.Dispose();
            dispatcher?.Dispose();
            collisionConfiguration?.Dispose();
        }

        /// <summary>
        /// Enables or disables the rendering of collider shapes
        /// </summary>
        public bool ColliderShapesRendering
        {
            set
            {
                processor.RenderColliderShapes(value);
            }
        }

        public RenderGroup ColliderShapesRenderGroup { get; set; } = RenderGroup.Group0;

        internal void AddCollider(PhysicsComponent component, CollisionFilterGroupFlags group, CollisionFilterGroupFlags mask)
        {
            collisionWorld.AddCollisionObject(component.NativeCollisionObject, (BulletSharp.CollisionFilterGroups)group, (BulletSharp.CollisionFilterGroups)mask);
        }

        internal void RemoveCollider(PhysicsComponent component)
        {
            collisionWorld.RemoveCollisionObject(component.NativeCollisionObject);
        }

        internal void AddRigidBody(RigidbodyComponent rigidBody, CollisionFilterGroupFlags group, CollisionFilterGroupFlags mask)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            discreteDynamicsWorld.AddRigidBody(rigidBody.InternalRigidBody, (short)group, (short)mask);
        }

        internal void RemoveRigidBody(RigidbodyComponent rigidBody)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            discreteDynamicsWorld.RemoveRigidBody(rigidBody.InternalRigidBody);
        }

        internal void AddCharacter(CharacterComponent character, CollisionFilterGroupFlags group, CollisionFilterGroupFlags mask)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            var collider = character.NativeCollisionObject;
            var action = character.KinematicCharacter;
            discreteDynamicsWorld.AddCollisionObject(collider, (BulletSharp.CollisionFilterGroups)group, (BulletSharp.CollisionFilterGroups)mask);
            discreteDynamicsWorld.AddAction(action);

            character.Simulation = this;
        }

        internal void RemoveCharacter(CharacterComponent character)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            var collider = character.NativeCollisionObject;
            var action = character.KinematicCharacter;
            discreteDynamicsWorld.RemoveCollisionObject(collider);
            discreteDynamicsWorld.RemoveAction(action);

            character.Simulation = null;
        }

        /// <summary>
        /// Creates the constraint.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="rigidBodyA">The rigid body a.</param>
        /// <param name="frameA">The frame a.</param>
        /// <param name="useReferenceFrameA">if set to <c>true</c> [use reference frame a].</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Cannot perform this action when the physics engine is set to CollisionsOnly
        /// or
        /// Both RigidBodies must be valid
        /// or
        /// A Gear constraint always needs two rigidbodies to be created.
        /// </exception>
        public static Constraint CreateConstraint(ConstraintTypes type, RigidbodyComponent rigidBodyA, Matrix frameA, bool useReferenceFrameA = false)
        {
            return CreateConstraintInternal(type, rigidBodyA, frameA, useReferenceFrameA:useReferenceFrameA);
        }
        
        /// <summary>
        /// Creates the constraint.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="rigidBodyA">The rigid body a.</param>
        /// <param name="rigidBodyB">The rigid body b.</param>
        /// <param name="frameA">The frame a.</param>
        /// <param name="frameB">The frame b.</param>
        /// <param name="useReferenceFrameA">if set to <c>true</c> [use reference frame a].</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Cannot perform this action when the physics engine is set to CollisionsOnly
        /// or
        /// Both RigidBodies must be valid
        /// </exception>
        public static Constraint CreateConstraint(ConstraintTypes type, RigidbodyComponent rigidBodyA, RigidbodyComponent rigidBodyB, Matrix frameA, Matrix frameB, bool useReferenceFrameA = false)
        {
            if (rigidBodyA == null || rigidBodyB == null) throw new Exception("Both RigidBodies must be valid");
            return CreateConstraintInternal(type, rigidBodyA, frameA, rigidBodyB, frameB, useReferenceFrameA);
        }


        static Constraint CreateConstraintInternal(ConstraintTypes type, RigidbodyComponent rigidBodyA, Matrix frameA, RigidbodyComponent rigidBodyB = null, Matrix frameB = default, bool useReferenceFrameA = false)
        {
            if (rigidBodyA == null) throw new Exception($"{nameof(rigidBodyA)} must be valid");
            if (rigidBodyB != null && rigidBodyB.Simulation != rigidBodyA.Simulation) throw new Exception("Both RigidBodies must be on the same simulation");

            Constraint constraintBase;
            var rbA = rigidBodyA.InternalRigidBody;
            var rbB = rigidBodyB?.InternalRigidBody;
            switch (type)
            {
                case ConstraintTypes.Point2Point:
                {
                    var constraint = new Point2PointConstraint
                    {
                        InternalPoint2PointConstraint = 
                            rigidBodyB == null ? 
                            new BulletSharp.Point2PointConstraint(rbA, frameA.TranslationVector ) :
                            new BulletSharp.Point2PointConstraint(rbA, rbB, frameA.TranslationVector, frameB.TranslationVector),
                    };
                    constraintBase = constraint;

                    constraint.InternalConstraint = constraint.InternalPoint2PointConstraint;
                    break;
                }
                case ConstraintTypes.Hinge:
                {
                    var constraint = new HingeConstraint
                    {
                        InternalHingeConstraint = 
                            rigidBodyB == null ? 
                                new BulletSharp.HingeConstraint(rbA, frameA ) :
                                new BulletSharp.HingeConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                    };
                    constraintBase = constraint;

                    constraint.InternalConstraint = constraint.InternalHingeConstraint;
                    break;
                }
                case ConstraintTypes.Slider:
                {
                    var constraint = new SliderConstraint
                    {
                        InternalSliderConstraint = 
                            rigidBodyB == null ? 
                                new BulletSharp.SliderConstraint(rbA, frameA, useReferenceFrameA ) :
                                new BulletSharp.SliderConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                    };
                    constraintBase = constraint;

                    constraint.InternalConstraint = constraint.InternalSliderConstraint;
                    break;
                }
                case ConstraintTypes.ConeTwist:
                {
                    var constraint = new ConeTwistConstraint
                    {
                        InternalConeTwistConstraint =  
                            rigidBodyB == null ? 
                                new BulletSharp.ConeTwistConstraint(rbA, frameA) :
                                new BulletSharp.ConeTwistConstraint(rbA, rbB, frameA, frameB),
                    };
                    constraintBase = constraint;

                    constraint.InternalConstraint = constraint.InternalConeTwistConstraint;
                    break;
                }
                case ConstraintTypes.Generic6DoF:
                {
                    var constraint = new Generic6DoFConstraint
                    {
                        InternalGeneric6DofConstraint =  
                            rigidBodyB == null ? 
                                new BulletSharp.Generic6DofConstraint(rbA, frameA, useReferenceFrameA) :
                                new BulletSharp.Generic6DofConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                    };
                    constraintBase = constraint;

                    constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint;
                    break;
                }
                case ConstraintTypes.Generic6DoFSpring:
                {
                    var constraint = new Generic6DoFSpringConstraint
                    {
                        InternalGeneric6DofSpringConstraint =  
                            rigidBodyB == null ? 
                                new BulletSharp.Generic6DofSpringConstraint(rbA, frameA, useReferenceFrameA) :
                                new BulletSharp.Generic6DofSpringConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),
                    };
                    constraintBase = constraint;

                    constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint = constraint.InternalGeneric6DofSpringConstraint;
                    break;
                }
                case ConstraintTypes.Gear:
                {
                    var constraint = new GearConstraint
                    {
                        InternalGearConstraint =  
                            rigidBodyB == null ? 
                                throw new Exception("A Gear constraint always needs two rigidbodies to be created.") :
                                new BulletSharp.GearConstraint(rbA, rbB, frameA.TranslationVector, frameB.TranslationVector),
                    };
                    constraintBase = constraint;

                    constraint.InternalConstraint = constraint.InternalGearConstraint;
                    break;
                }
                default:
                    throw new ArgumentException(type.ToString());
            }

            if(rigidBodyB != null)
            {
                constraintBase.RigidBodyB = rigidBodyB;
                rigidBodyB.LinkedConstraints.Add(constraintBase);
            }
            rigidBodyA.LinkedConstraints.Add(constraintBase);

            return constraintBase;
        }


        /// <summary>
        /// Adds the constraint to the engine processing pipeline.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <exception cref="System.Exception">Cannot perform this action when the physics engine is set to CollisionsOnly</exception>
        public void AddConstraint(Constraint constraint)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            discreteDynamicsWorld.AddConstraint(constraint.InternalConstraint);
            constraint.Simulation = this;
        }

        /// <summary>
        /// Adds the constraint to the engine processing pipeline.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <param name="disableCollisionsBetweenLinkedBodies">if set to <c>true</c> [disable collisions between linked bodies].</param>
        /// <exception cref="System.Exception">Cannot perform this action when the physics engine is set to CollisionsOnly</exception>
        public void AddConstraint(Constraint constraint, bool disableCollisionsBetweenLinkedBodies)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            discreteDynamicsWorld.AddConstraint(constraint.InternalConstraint, disableCollisionsBetweenLinkedBodies);
            constraint.Simulation = this;
        }

        /// <summary>
        /// Removes the constraint from the engine processing pipeline.
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        /// <exception cref="System.Exception">Cannot perform this action when the physics engine is set to CollisionsOnly</exception>
        public void RemoveConstraint(Constraint constraint)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            discreteDynamicsWorld.RemoveConstraint(constraint.InternalConstraint);
            constraint.Simulation = null;
        }

        /// <summary>
        /// Raycasts and returns the closest hit
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="filterGroup">The collision group of this raycast</param>
        /// <param name="filterFlags">The collision group that this raycast can collide with</param>
        /// <returns>The list with hit results.</returns>
        public HitResult Raycast(Vector3 from, Vector3 to, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
        {
            var callback = StrideClosestRayResultCallback.Shared(ref from, ref to, filterGroup, filterFlags);
            collisionWorld.RayTest(from, to, callback);
            return callback.Result;
        }

        /// <summary>
        /// Raycasts, returns true when it hit something
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="result">Raycast info</param>
        /// <param name="filterGroup">The collision group of this raycast</param>
        /// <param name="filterFlags">The collision group that this raycast can collide with</param>
        /// <returns>The list with hit results.</returns>
        public bool Raycast(Vector3 from, Vector3 to, out HitResult result, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
        {
            var callback = StrideClosestRayResultCallback.Shared(ref from, ref to, filterGroup, filterFlags);
            collisionWorld.RayTest(from, to, callback);
            result = callback.Result;
            return result.Succeeded;
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// Filtering by CollisionGroup
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <param name="filterGroup">The collision group of this raycast</param>
        /// <param name="filterFlags">The collision group that this raycast can collide with</param>
        public void RaycastPenetrating(Vector3 from, Vector3 to, IList<HitResult> resultsOutput, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
        {
            var callback = StrideAllHitsRayResultCallback.Shared(ref from, ref to, resultsOutput, filterGroup, filterFlags);
            collisionWorld.RayTest(from, to, callback);
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and returns the closest hit
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="filterGroup">The collision group of this shape sweep</param>
        /// <param name="filterFlags">The collision group that this shape sweep can collide with</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public HitResult ShapeSweep(ColliderShape shape, Matrix from, Matrix to, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
        {
            var sh = shape.InternalShape as BulletSharp.ConvexShape;
            if (sh == null) throw new Exception("This kind of shape cannot be used for a ShapeSweep.");

            var callback = StrideClosestConvexResultCallback.Shared(filterGroup, filterFlags);
            collisionWorld.ConvexSweepTest(sh, from, to, callback);
            return callback.Result;
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and never stops until "to"
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <param name="filterGroup">The collision group of this shape sweep</param>
        /// <param name="filterFlags">The collision group that this shape sweep can collide with</param>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public void ShapeSweepPenetrating(ColliderShape shape, Matrix from, Matrix to, IList<HitResult> resultsOutput, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterFlags = DefaultFlags)
        {
            var sh = shape.InternalShape as BulletSharp.ConvexShape;
            if (sh == null)
            {
                throw new Exception("This kind of shape cannot be used for a ShapeSweep.");
            }
            
            var rcb = StrideAllHitsConvexResultCallback.Shared(resultsOutput, filterGroup, filterFlags);
            collisionWorld.ConvexSweepTest(sh, from, to, rcb);
        }

        /// <summary>
        /// Gets or sets the gravity.
        /// </summary>
        /// <value>
        /// The gravity.
        /// </value>
        /// <exception cref="System.Exception">
        /// Cannot perform this action when the physics engine is set to CollisionsOnly
        /// </exception>
        public Vector3 Gravity
        {
            get
            {
                if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");
                return discreteDynamicsWorld.Gravity;
            }
            set
            {
                if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");
                discreteDynamicsWorld.Gravity = value;
            }
        }

        /// <summary>
        /// The maximum number of steps that the Simulation is allowed to take each tick.
        /// If the engine is running slow (large deltaTime), then you must increase the number of maxSubSteps to compensate for this, otherwise your simulation is “losing” time.
        /// It's important that frame DeltaTime is always less than MaxSubSteps*FixedTimeStep, otherwise you are losing time.
        /// </summary>
        public int MaxSubSteps { get; set; }

        /// <summary>
        /// By decreasing the size of fixedTimeStep, you are increasing the “resolution” of the simulation.
        /// Default is 1.0f / 60.0f or 60fps
        /// </summary>
        public float FixedTimeStep { get; set; }

        public void ClearForces()
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");
            discreteDynamicsWorld.ClearForces();
        }

        public bool SpeculativeContactRestitution
        {
            get
            {
                if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");
                return discreteDynamicsWorld.ApplySpeculativeContactRestitution;
            }
            set
            {
                if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");
                discreteDynamicsWorld.ApplySpeculativeContactRestitution = value;
            }
        }

        public class SimulationArgs : EventArgs
        {
            public float DeltaTime;
        }

        /// <summary>
        /// Called right before the physics simulation.
        /// This event might not be fired by the main thread.
        /// </summary>
        public event EventHandler<SimulationArgs> SimulationBegin;

        protected virtual void OnSimulationBegin(SimulationArgs e)
        {
            var handler = SimulationBegin;
            handler?.Invoke(this, e);
        }

        internal int UpdatedRigidbodies;

        private readonly SimulationArgs simulationArgs = new SimulationArgs();

        internal ProfilingState SimulationProfiler;

        internal void Simulate(float deltaTime)
        {
            if (collisionWorld == null) return;

            simulationArgs.DeltaTime = deltaTime;

            UpdatedRigidbodies = 0;

            OnSimulationBegin(simulationArgs);

            SimulationProfiler = Profiler.Begin(PhysicsProfilingKeys.SimulationProfilingKey);

            if (discreteDynamicsWorld != null) discreteDynamicsWorld.StepSimulation(deltaTime, MaxSubSteps, FixedTimeStep);
            else collisionWorld.PerformDiscreteCollisionDetection();

            SimulationProfiler.End("Alive rigidbodies: {0}", UpdatedRigidbodies);

            OnSimulationEnd(simulationArgs);
        }

        /// <summary>
        /// Called right after the physics simulation.
        /// This event might not be fired by the main thread.
        /// </summary>
        public event EventHandler<SimulationArgs> SimulationEnd;

        protected virtual void OnSimulationEnd(SimulationArgs e)
        {
            var handler = SimulationEnd;
            handler?.Invoke(this, e);
        }

        private class StrideAllHitsConvexResultCallback : StrideReusableConvexResultCallback
        {
            [ThreadStatic]
            static StrideAllHitsConvexResultCallback shared;

            public IList<HitResult> ResultsList { get; set; }

            public StrideAllHitsConvexResultCallback(IList<HitResult> results)
            {
                ResultsList = results;
            }

            public override float AddSingleResult(ref BulletSharp.LocalConvexResult convexResult, bool normalInWorldSpace)
            {
                ResultsList.Add(ComputeHitResult(ref convexResult, normalInWorldSpace));
                return convexResult.m_hitFraction;
            }

            public static StrideAllHitsConvexResultCallback Shared(IList<HitResult> buffer, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                if (shared == null)
                {
                    shared = new StrideAllHitsConvexResultCallback(buffer);
                }
                shared.ResultsList = buffer;
                shared.Recycle(filterGroup, filterMask);
                return shared;
            }
        }

        private class StrideClosestConvexResultCallback : StrideReusableConvexResultCallback
        {
            [ThreadStatic]
            static StrideClosestConvexResultCallback shared;

            BulletSharp.LocalConvexResult closestHit;
            bool normalInWorldSpace;
            float? closestFraction;
            public HitResult Result => ComputeHitResult(ref closestHit, normalInWorldSpace);

            public override float AddSingleResult(ref BulletSharp.LocalConvexResult convexResult, bool normalInWorldSpaceParam)
            {
                float fraction = convexResult.m_hitFraction;
                // First hit or closest hit yet
                if (closestFraction == null || closestFraction > fraction)
                {
                    closestHit = convexResult;
                    closestFraction = fraction;
                    normalInWorldSpace = normalInWorldSpaceParam;
                }
                return fraction;
            }

            public override void Recycle(CollisionFilterGroups filterGroup = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags filterMask = (CollisionFilterGroupFlags)(-1))
            {
                base.Recycle(filterGroup, filterMask);
                closestFraction = null;
                closestHit = default;
            }

            public static StrideClosestConvexResultCallback Shared(CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                if (shared == null)
                {
                    shared = new StrideClosestConvexResultCallback();
                }
                shared.Recycle(filterGroup, filterMask);
                return shared;
            }
        }

        private class StrideAllHitsRayResultCallback : StrideReusableRayResultCallback
        {
            [ThreadStatic]
            static StrideAllHitsRayResultCallback shared;

            public IList<HitResult> ResultsList { get; set; }

            public StrideAllHitsRayResultCallback(ref Vector3 from, ref Vector3 to, IList<HitResult> results) : base(ref from, ref to)
            {
                ResultsList = results;
            }

            public override float AddSingleResult(ref BulletSharp.LocalRayResult rayResult, bool normalInWorldSpace)
            {
                ResultsList.Add(ComputeHitResult(ref rayResult, normalInWorldSpace));
                return rayResult.m_hitFraction;
            }

            public static StrideAllHitsRayResultCallback Shared(ref Vector3 from, ref Vector3 to, IList<HitResult> buffer, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                if (shared == null)
                {
                    shared = new StrideAllHitsRayResultCallback(ref from, ref to, buffer);
                }
                shared.ResultsList = buffer;
                shared.Recycle(ref from, ref to, filterGroup, filterMask);
                return shared;
            }
        }

        private class StrideClosestRayResultCallback : StrideReusableRayResultCallback
        {
            [ThreadStatic]
            static StrideClosestRayResultCallback shared;

            BulletSharp.LocalRayResult closestHit;
            bool normalInWorldSpace;
            float? closestFraction;
            public HitResult Result => ComputeHitResult(ref closestHit, normalInWorldSpace);

            public StrideClosestRayResultCallback(ref Vector3 from, ref Vector3 to) : base(ref from, ref to)
            {
            }

            public override float AddSingleResult(ref BulletSharp.LocalRayResult rayResult, bool normalInWorldSpaceParam)
            {
                float fraction = rayResult.m_hitFraction;
                // First hit or closest hit yet
                if (closestFraction == null || closestFraction > fraction)
                {
                    closestHit = rayResult;
                    closestFraction = fraction;
                    normalInWorldSpace = normalInWorldSpaceParam;
                    CollisionObject = rayResult.CollisionObject;
                    ClosestHitFraction = fraction;
                }
                return fraction;
            }

            public override void Recycle(ref Vector3 from, ref Vector3 to, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                base.Recycle(ref from, ref to, filterGroup, filterMask);
                closestFraction = null;
                closestHit = default;
            }

            public static StrideClosestRayResultCallback Shared(ref Vector3 from, ref Vector3 to, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                if (shared == null)
                {
                    shared = new StrideClosestRayResultCallback(ref from, ref to);
                }
                shared.Recycle(ref from, ref to, filterGroup, filterMask);
                return shared;
            }
        }

        private abstract class StrideReusableRayResultCallback : BulletSharp.RayResultCallback
        {
            public Vector3 RayFromWorld { get; protected set; }
            public Vector3 RayToWorld { get; protected set; }

            public StrideReusableRayResultCallback(ref Vector3 from, ref Vector3 to) : base()
            {
                RayFromWorld = from;
                RayToWorld = to;
            }

            public HitResult ComputeHitResult(ref BulletSharp.LocalRayResult rayResult, bool normalInWorldSpace)
            {
                var obj = rayResult.CollisionObject;
                if (obj == null)
                {
                    return new HitResult() { Succeeded = false };
                }

                Vector3 normal = rayResult.m_hitNormalLocal;
                if (!normalInWorldSpace)
                {
                    normal = Vector3.TransformNormal(normal, obj.WorldTransform.Basis);
                }

                return new HitResult
                {
                    Succeeded = true,
                    Collider = obj.UserObject as PhysicsComponent,
                    Point = Vector3.Lerp(RayFromWorld, RayToWorld, rayResult.m_hitFraction),
                    Normal = normal,
                    HitFraction = rayResult.m_hitFraction,
                };
            }

            public virtual void Recycle(ref Vector3 from, ref Vector3 to, CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                RayFromWorld = from;
                RayToWorld = to;
                ClosestHitFraction = float.PositiveInfinity;
                CollisionObject = null;
                Flags = 0;
                CollisionFilterGroup = (int)filterGroup;
                CollisionFilterMask = (int)filterMask;
            }
        }

        private abstract class StrideReusableConvexResultCallback : BulletSharp.ConvexResultCallback
        {
            public HitResult ComputeHitResult(ref BulletSharp.LocalConvexResult convexResult, bool normalInWorldSpace)
            {
                var obj = convexResult.HitCollisionObject;
                if ( obj == null )
                {
                    return new HitResult() { Succeeded = false };
                }

                Vector3 normal = convexResult.m_hitNormalLocal;
                if (!normalInWorldSpace)
                {
                    normal = Vector3.TransformNormal(normal, obj.WorldTransform.Basis);
                }

                return new HitResult
                {
                    Succeeded = true,
                    Collider = obj.UserObject as PhysicsComponent,
                    Point = convexResult.m_hitPointLocal,
                    Normal = normal,
                    HitFraction = convexResult.m_hitFraction,
                };
            }

            public virtual void Recycle(CollisionFilterGroups filterGroup = DefaultGroup, CollisionFilterGroupFlags filterMask = DefaultFlags)
            {
                ClosestHitFraction = float.PositiveInfinity;
                CollisionFilterGroup = (int)filterGroup;
                CollisionFilterMask = (int)filterMask;
            }
        }
    }
}
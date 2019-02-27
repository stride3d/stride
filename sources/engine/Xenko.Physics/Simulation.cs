// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Physics
{
    public class Simulation : IDisposable
    {
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

                if (configuration.Flags.HasFlag(PhysicsEngineFlags.ContinuosCollisionDetection))
                {
                    CanCcd = true;
                    solverInfo.SolverMode |= BulletSharp.SolverModes.Use2FrictionDirections | BulletSharp.SolverModes.RandomizeOrder;
                    dispatchInfo.UseContinuous = true;
                }
            }
        }

        private readonly List<Collision> newCollisionsCache = new List<Collision>();
        private readonly List<Collision> removedCollisionsCache = new List<Collision>();
        private readonly List<ContactPoint> newContactsFastCache = new List<ContactPoint>();
        private readonly List<ContactPoint> updatedContactsCache = new List<ContactPoint>();
        private readonly List<ContactPoint> removedContactsCache = new List<ContactPoint>();

        //private ProfilingState contactsProfilingState;

        private readonly Dictionary<ContactPoint, Collision> contactToCollision = new Dictionary<ContactPoint, Collision>(ContactPointEqualityComparer.Default);

        internal void SendEvents()
        {
            foreach (var collision in newCollisionsCache)
            {
                while (collision.ColliderA.NewPairChannel.Balance < 0)
                {
                    collision.ColliderA.NewPairChannel.Send(collision);
                }

                while (collision.ColliderB.NewPairChannel.Balance < 0)
                {
                    collision.ColliderB.NewPairChannel.Send(collision);
                }
            }

            foreach (var collision in removedCollisionsCache)
            {
                while (collision.ColliderA.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderA.PairEndedChannel.Send(collision);
                }

                while (collision.ColliderB.PairEndedChannel.Balance < 0)
                {
                    collision.ColliderB.PairEndedChannel.Send(collision);
                }
            }

            foreach (var contactPoint in newContactsFastCache)
            {
                Collision collision;
                if (contactToCollision.TryGetValue(contactPoint, out collision))
                {
                    while (collision.NewContactChannel.Balance < 0)
                    {
                        collision.NewContactChannel.Send(contactPoint);
                    }
                }
            }

            foreach (var contactPoint in updatedContactsCache)
            {
                Collision collision;
                if (contactToCollision.TryGetValue(contactPoint, out collision))
                {
                    while (collision.ContactUpdateChannel.Balance < 0)
                    {
                        collision.ContactUpdateChannel.Send(contactPoint);
                    }
                }
            }

            foreach (var contactPoint in removedContactsCache)
            {
                Collision collision;
                if (contactToCollision.TryGetValue(contactPoint, out collision))
                {
                    while (collision.ContactEndedChannel.Balance < 0)
                    {
                        collision.ContactEndedChannel.Send(contactPoint);
                    }
                }
            }

            //contactsProfilingState.End("Contacts: {0}", currentFrameContacts.Count);
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
            discreteDynamicsWorld.AddCharacter(action);

            character.Simulation = this;
        }

        internal void RemoveCharacter(CharacterComponent character)
        {
            if (discreteDynamicsWorld == null) throw new Exception("Cannot perform this action when the physics engine is set to CollisionsOnly");

            var collider = character.NativeCollisionObject;
            var action = character.KinematicCharacter;
            discreteDynamicsWorld.RemoveCollisionObject(collider);
            discreteDynamicsWorld.RemoveCharacter(action);

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
            if (rigidBodyA == null) throw new Exception("Both RigidBodies must be valid");

            var rbA = rigidBodyA.InternalRigidBody;

            switch (type)
            {
                case ConstraintTypes.Point2Point:
                    {
                        var constraint = new Point2PointConstraint
                        {
                            InternalPoint2PointConstraint = new BulletSharp.Point2PointConstraint(rbA, frameA.TranslationVector),

                            RigidBodyA = rigidBodyA,
                        };

                        constraint.InternalConstraint = constraint.InternalPoint2PointConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Hinge:
                    {
                        var constraint = new HingeConstraint
                        {
                            InternalHingeConstraint = new BulletSharp.HingeConstraint(rbA, frameA, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                        };

                        constraint.InternalConstraint = constraint.InternalHingeConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Slider:
                    {
                        var constraint = new SliderConstraint
                        {
                            InternalSliderConstraint = new BulletSharp.SliderConstraint(rbA, frameA, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                        };

                        constraint.InternalConstraint = constraint.InternalSliderConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.ConeTwist:
                    {
                        var constraint = new ConeTwistConstraint
                        {
                            InternalConeTwistConstraint = new BulletSharp.ConeTwistConstraint(rbA, frameA),

                            RigidBodyA = rigidBodyA,
                        };

                        constraint.InternalConstraint = constraint.InternalConeTwistConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Generic6DoF:
                    {
                        var constraint = new Generic6DoFConstraint
                        {
                            InternalGeneric6DofConstraint = new BulletSharp.Generic6DofConstraint(rbA, frameA, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                        };

                        constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Generic6DoFSpring:
                    {
                        var constraint = new Generic6DoFSpringConstraint
                        {
                            InternalGeneric6DofSpringConstraint = new BulletSharp.Generic6DofSpringConstraint(rbA, frameA, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                        };

                        constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint = constraint.InternalGeneric6DofSpringConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Gear:
                    {
                        throw new Exception("A Gear constraint always needs two rigidbodies to be created.");
                    }
            }

            return null;
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
            //todo check if the 2 rbs are on the same engine instance!

            var rbA = rigidBodyA.InternalRigidBody;
            var rbB = rigidBodyB.InternalRigidBody;

            switch (type)
            {
                case ConstraintTypes.Point2Point:
                    {
                        var constraint = new Point2PointConstraint
                        {
                            InternalPoint2PointConstraint = new BulletSharp.Point2PointConstraint(rbA, rbB, frameA.TranslationVector, frameB.TranslationVector),

                            RigidBodyA = rigidBodyA,
                            RigidBodyB = rigidBodyB,
                        };

                        constraint.InternalConstraint = constraint.InternalPoint2PointConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);
                        rigidBodyB.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Hinge:
                    {
                        var constraint = new HingeConstraint
                        {
                            InternalHingeConstraint = new BulletSharp.HingeConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                            RigidBodyB = rigidBodyB,
                        };

                        constraint.InternalConstraint = constraint.InternalHingeConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);
                        rigidBodyB.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Slider:
                    {
                        var constraint = new SliderConstraint
                        {
                            InternalSliderConstraint = new BulletSharp.SliderConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                            RigidBodyB = rigidBodyB,
                        };

                        constraint.InternalConstraint = constraint.InternalSliderConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);
                        rigidBodyB.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.ConeTwist:
                    {
                        var constraint = new ConeTwistConstraint
                        {
                            InternalConeTwistConstraint = new BulletSharp.ConeTwistConstraint(rbA, rbB, frameA, frameB),

                            RigidBodyA = rigidBodyA,
                            RigidBodyB = rigidBodyB,
                        };

                        constraint.InternalConstraint = constraint.InternalConeTwistConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);
                        rigidBodyB.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Generic6DoF:
                    {
                        var constraint = new Generic6DoFConstraint
                        {
                            InternalGeneric6DofConstraint = new BulletSharp.Generic6DofConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                            RigidBodyB = rigidBodyB,
                        };

                        constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);
                        rigidBodyB.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Generic6DoFSpring:
                    {
                        var constraint = new Generic6DoFSpringConstraint
                        {
                            InternalGeneric6DofSpringConstraint = new BulletSharp.Generic6DofSpringConstraint(rbA, rbB, frameA, frameB, useReferenceFrameA),

                            RigidBodyA = rigidBodyA,
                            RigidBodyB = rigidBodyB,
                        };

                        constraint.InternalConstraint = constraint.InternalGeneric6DofConstraint = constraint.InternalGeneric6DofSpringConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);
                        rigidBodyB.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
                case ConstraintTypes.Gear:
                    {
                        var constraint = new GearConstraint
                        {
                            InternalGearConstraint = new BulletSharp.GearConstraint(rbA, rbB, frameA.TranslationVector, frameB.TranslationVector),

                            RigidBodyA = rigidBodyA,
                            RigidBodyB = rigidBodyB,
                        };

                        constraint.InternalConstraint = constraint.InternalGearConstraint;

                        rigidBodyA.LinkedConstraints.Add(constraint);
                        rigidBodyB.LinkedConstraints.Add(constraint);

                        return constraint;
                    }
            }

            return null;
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

        private class XenkoAllHitsConvexResultCallback : BulletSharp.AllHitsConvexResultCallback
        {
            private IList<HitResult> resultsList;

            public XenkoAllHitsConvexResultCallback(IList<HitResult> results)
            {
                resultsList = results;
            }

            public override void AddSingleResult(BulletSharp.CollisionObject collisionObject, ref Vector3 point, ref Vector3 normal, float hitFraction)
            {
                resultsList.Add(new HitResult
                {
                    Succeeded = true,
                    Collider = collisionObject.UserObject as PhysicsComponent,
                    Point = point,
                    Normal = normal,
                    HitFraction = hitFraction,
                });
            }
        }

        private class XenkoAllHitsRayResultCallback : BulletSharp.AllHitsRayResultCallback
        {
            private IList<HitResult> resultsList;

            public XenkoAllHitsRayResultCallback(ref Vector3 from, ref Vector3 to, IList<HitResult> results) : base(ref from, ref to)
            {
                resultsList = results;
            }

            public override void AddSingleResult(BulletSharp.CollisionObject collisionObject, ref Vector3 point, ref Vector3 normal, float hitFraction)
            {
                resultsList.Add(new HitResult
                {
                    Succeeded = true,
                    Collider = collisionObject.UserObject as PhysicsComponent,
                    Point = point,
                    Normal = normal,
                    HitFraction = hitFraction,
                });
            }
        }

        /// <summary>
        /// Raycasts and stops at the first hit.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns></returns>
        public HitResult Raycast(Vector3 from, Vector3 to)
        {
            var result = new HitResult(); //result.Succeeded is false by default

            using (var rcb = new BulletSharp.ClosestRayResultCallback(from, to))
            {
                collisionWorld.RayTest(ref from, ref to, rcb);

                if (rcb.CollisionObject == null) return result;
                result.Succeeded = true;
                result.Collider = (PhysicsComponent)rcb.CollisionObject.UserObject;
                result.Normal = rcb.HitNormalWorld;
                result.Point = rcb.HitPointWorld;
                result.HitFraction = rcb.ClosestHitFraction;
            }

            return result;
        }

        /// <summary>
        /// Raycasts and stops at the first hit.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <returns>The list with hit results.</returns>
        public HitResult Raycast(Vector3 from, Vector3 to, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            var result = new HitResult(); //result.Succeeded is false by default

            using (var rcb = new BulletSharp.ClosestRayResultCallback(from, to)
            {
                CollisionFilterGroup = (short)collisionFilterGroups,
                CollisionFilterMask = (short)collisionFilterGroupFlags,
            })
            {
                collisionWorld.RayTest(ref from, ref to, rcb);

                if (rcb.CollisionObject == null) return result;
                result.Succeeded = true;
                result.Collider = (PhysicsComponent)rcb.CollisionObject.UserObject;
                result.Normal = rcb.HitNormalWorld;
                result.Point = rcb.HitPointWorld;
                result.HitFraction = rcb.ClosestHitFraction;
            }

            return result;
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        public void RaycastPenetrating(Vector3 from, Vector3 to, IList<HitResult> resultsOutput)
        {
            using (var rcb = new XenkoAllHitsRayResultCallback(ref from, ref to, resultsOutput))
            {
                collisionWorld.RayTest(ref from, ref to, rcb);
            }
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>The list with hit results.</returns>
        public FastList<HitResult> RaycastPenetrating(Vector3 from, Vector3 to)
        {
            var results = new FastList<HitResult>();
            RaycastPenetrating(from, to, results);
            return results;
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// Filtering by CollisionGroup
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        public void RaycastPenetrating(Vector3 from, Vector3 to, IList<HitResult> resultsOutput, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            using (var rcb = new XenkoAllHitsRayResultCallback(ref from, ref to, resultsOutput)
            {
                CollisionFilterGroup = (short)collisionFilterGroups,
                CollisionFilterMask = (short)collisionFilterGroupFlags,
            })
            {
                collisionWorld.RayTest(ref from, ref to, rcb);
            }
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <returns>The list with hit results.</returns>
        public FastList<HitResult> RaycastPenetrating(Vector3 from, Vector3 to, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            var results = new FastList<HitResult>();
            RaycastPenetrating(from, to, results, collisionFilterGroups, collisionFilterGroupFlags);
            return results;
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and stops at the first hit
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public HitResult ShapeSweep(ColliderShape shape, Matrix from, Matrix to)
        {
            var sh = shape.InternalShape as BulletSharp.ConvexShape;
            if (sh == null) throw new Exception("This kind of shape cannot be used for a ShapeSweep.");

            var result = new HitResult(); //result.Succeeded is false by default

            using (var rcb = new BulletSharp.ClosestConvexResultCallback(from.TranslationVector, to.TranslationVector))
            {
                collisionWorld.ConvexSweepTest(sh, from, to, rcb);

                if (rcb.HitCollisionObject == null) return result;
                result.Succeeded = true;
                result.Collider = (PhysicsComponent)rcb.HitCollisionObject.UserObject;
                result.Normal = rcb.HitNormalWorld;
                result.Point = rcb.HitPointWorld;
                result.HitFraction = rcb.ClosestHitFraction;
            }

            return result;
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and stops at the first hit
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public HitResult ShapeSweep(ColliderShape shape, Matrix from, Matrix to, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            var sh = shape.InternalShape as BulletSharp.ConvexShape;
            if (sh == null) throw new Exception("This kind of shape cannot be used for a ShapeSweep.");

            var result = new HitResult(); //result.Succeeded is false by default

            using (var rcb = new BulletSharp.ClosestConvexResultCallback(from.TranslationVector, to.TranslationVector)
            {
                CollisionFilterGroup = (BulletSharp.CollisionFilterGroups)collisionFilterGroups,
                CollisionFilterMask = (BulletSharp.CollisionFilterGroups)collisionFilterGroupFlags,
            })
            {
                collisionWorld.ConvexSweepTest(sh, from, to, rcb);

                if (rcb.HitCollisionObject == null) return result;
                result.Succeeded = true;
                result.Collider = (PhysicsComponent)rcb.HitCollisionObject.UserObject;
                result.Normal = rcb.HitNormalWorld;
                result.Point = rcb.HitPointWorld;
                result.HitFraction = rcb.ClosestHitFraction;
            }

            return result;
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and never stops until "to"
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public void ShapeSweepPenetrating(ColliderShape shape, Matrix from, Matrix to, IList<HitResult> resultsOutput)
        {
            var sh = shape.InternalShape as BulletSharp.ConvexShape;
            if (sh == null) throw new Exception("This kind of shape cannot be used for a ShapeSweep.");

            using (var rcb = new XenkoAllHitsConvexResultCallback(resultsOutput))
            {
                collisionWorld.ConvexSweepTest(sh, from, to, rcb);
            }
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and never stops until "to"
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>The list with hit results.</returns>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public FastList<HitResult> ShapeSweepPenetrating(ColliderShape shape, Matrix from, Matrix to)
        {
            var results = new FastList<HitResult>();
            ShapeSweepPenetrating(shape, from, to, results);
            return results;
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and never stops until "to"
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public void ShapeSweepPenetrating(ColliderShape shape, Matrix from, Matrix to, IList<HitResult> resultsOutput, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            var sh = shape.InternalShape as BulletSharp.ConvexShape;
            if (sh == null) throw new Exception("This kind of shape cannot be used for a ShapeSweep.");

            using (var rcb = new XenkoAllHitsConvexResultCallback(resultsOutput)
            {
                CollisionFilterGroup = (BulletSharp.CollisionFilterGroups)collisionFilterGroups,
                CollisionFilterMask = (BulletSharp.CollisionFilterGroups)collisionFilterGroupFlags,
            })
            {
                collisionWorld.ConvexSweepTest(sh, from, to, rcb);
            }
        }

        /// <summary>
        /// Performs a sweep test using a collider shape and never stops until "to"
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <returns>The list with hit results.</returns>
        /// <exception cref="System.Exception">This kind of shape cannot be used for a ShapeSweep.</exception>
        public FastList<HitResult> ShapeSweepPenetrating(ColliderShape shape, Matrix from, Matrix to, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            var results = new FastList<HitResult>();
            ShapeSweepPenetrating(shape, from, to, results, collisionFilterGroups, collisionFilterGroupFlags);
            return results;
        }

        /// <summary>
        /// Gets or sets the gravity.
        /// </summary>
        /// <value>
        /// The gravity.
        /// </value>
        /// <exception cref="System.Exception">
        /// Cannot perform this action when the physics engine is set to CollisionsOnly
        /// or
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

        private readonly FastList<ContactPoint> newContacts = new FastList<ContactPoint>();
        private readonly FastList<ContactPoint> updatedContacts = new FastList<ContactPoint>();
        private readonly FastList<ContactPoint> removedContacts = new FastList<ContactPoint>();

        private readonly Queue<Collision> collisionsPool = new Queue<Collision>();

        internal void BeginContactTesting()
        {
            //remove previous frame removed collisions
            foreach (var collision in removedCollisionsCache)
            {
                collision.Destroy();
                collisionsPool.Enqueue(collision);
            }

            //clean caches
            newCollisionsCache.Clear();
            removedCollisionsCache.Clear();
            newContactsFastCache.Clear();
            updatedContactsCache.Clear();
            removedContactsCache.Clear();

            //swap the lists
            var previous = currentFrameContacts;
            currentFrameContacts = previousFrameContacts;
            currentFrameContacts.Clear();
            previousFrameContacts = previous;         
        }

        private void ContactRemoval(ContactPoint contact, PhysicsComponent component0, PhysicsComponent component1)
        {
            Collision existingPair = null;
            foreach (var x in component0.Collisions)
            {
                if (x.InternalEquals(component0, component1))
                {
                    existingPair = x;
                    break;
                }
            }
            if (existingPair == null)
            {
#if DEBUG
                //should not happen?
                Log.Warning("Pair not present.");
#endif
                return;
            }

            if (existingPair.Contacts.Contains(contact))
            {
                existingPair.Contacts.Remove(contact);
                removedContactsCache.Add(contact);

                contactToCollision.Remove(contact);

                if (existingPair.Contacts.Count == 0)
                {
                    component0.Collisions.Remove(existingPair);
                    component1.Collisions.Remove(existingPair);
                    removedCollisionsCache.Add(existingPair);
                }
            }
            else
            {
#if DEBUG
                //should not happen?
                Log.Warning("Contact not in pair.");
#endif
            }
        }

        internal void EndContactTesting()
        {
            newContacts.Clear(true);
            updatedContacts.Clear(true);
            removedContacts.Clear(true);

            foreach (var currentFrameContact in currentFrameContacts)
            {
                if (!previousFrameContacts.Contains(currentFrameContact))
                {
                    newContacts.Add(currentFrameContact);
                }
                else
                {
                    updatedContacts.Add(currentFrameContact);
                }
            }

            foreach (var previousFrameContact in previousFrameContacts)
            {
                if (!currentFrameContacts.Contains(previousFrameContact))
                {
                    removedContacts.Add(previousFrameContact);
                }
            }

            foreach (var contact in newContacts)
            {
                var component0 = contact.ColliderA;
                var component1 = contact.ColliderB;

                Collision existingPair = null;
                foreach (var x in component0.Collisions)
                {
                    if (x.InternalEquals(component0, component1))
                    {
                        existingPair = x;
                        break;
                    }
                }
                if (existingPair != null)
                {
                    if (existingPair.Contacts.Contains(contact))
                    {
#if DEBUG
                        //should not happen?
                        Log.Warning("Contact already added.");
#endif
                        continue;
                    }

                    existingPair.Contacts.Add(contact);
                }
                else
                {
                    var newPair = collisionsPool.Count == 0 ? new Collision() : collisionsPool.Dequeue();
                    newPair.Initialize(component0, component1);
                    newPair.Contacts.Add(contact);
                    component0.Collisions.Add(newPair);
                    component1.Collisions.Add(newPair);

                    contactToCollision.Add(contact, newPair);

                    newCollisionsCache.Add(newPair);
                    newContactsFastCache.Add(contact);
                }
            }

            foreach (var contact in updatedContacts)
            {
                var component0 = contact.ColliderA;
                var component1 = contact.ColliderB;

                Collision existingPair = null;
                foreach (var x in component0.Collisions)
                {
                    if (x.InternalEquals(component0, component1))
                    {
                        existingPair = x;
                        break;
                    }
                }
                if (existingPair != null)
                {
                    if (existingPair.Contacts.Contains(contact))
                    {
                        //update data values (since comparison is only at pointer level internally)
                        existingPair.Contacts.Remove(contact);
                        existingPair.Contacts.Add(contact);
                        updatedContactsCache.Add(contact);
                    }
                    else
                    {
#if DEBUG
                        //should not happen?
                        Log.Warning("Contact not in pair.");
#endif
                    }
                }
                else
                {
#if DEBUG
                    //should not happen?
                    Log.Warning("Pair not present.");
#endif
                }
            }

            foreach (var contact in removedContacts)
            {
                var component0 = contact.ColliderA;
                var component1 = contact.ColliderB;

                ContactRemoval(contact, component0, component1);
            }     
        }

        private HashSet<ContactPoint> currentFrameContacts = new HashSet<ContactPoint>(ContactPointEqualityComparer.Default);
        private HashSet<ContactPoint> previousFrameContacts = new HashSet<ContactPoint>(ContactPointEqualityComparer.Default);

        internal unsafe void ContactTest(PhysicsComponent component)
        {
            IntPtr buffer;
            int bufferSize;
            collisionWorld.GetCollisions(component.NativeCollisionObject, (short)component.CanCollideWith, (short)component.CollisionGroup, out buffer, out bufferSize);
            var contacts = (NativeContactPoint*)buffer;
            for (var i = 0; i < bufferSize; i++)
            {
                var contact = contacts[i];

                var obj0 = BulletSharp.CollisionObject.GetManaged(contact.ColliderA);
                var obj1 = BulletSharp.CollisionObject.GetManaged(contact.ColliderB);
                var component0 = (PhysicsComponent)obj0.UserObject;
                var component1 = (PhysicsComponent)obj1.UserObject;

                //disable static-static
                if ((component0 is StaticColliderComponent && component1 is StaticColliderComponent) || !component0.Enabled || !component1.Enabled)
                    continue;

                currentFrameContacts.Add(new ContactPoint
                {
                    ColliderA = component0,
                    ColliderB = component1,
                    Distance = contact.Distance,
                    Normal = contact.Normal,
                    PositionOnA = contact.PositionOnA,
                    PositionOnB = contact.PositionOnB,
                });
            }
        }

        private readonly FastList<ContactPoint> previousToRemove = new FastList<ContactPoint>();

        internal void CleanContacts(PhysicsComponent component)
        {
            previousToRemove.Clear(true);

            foreach (var previousFrameContact in previousFrameContacts)
            {
                var component0 = previousFrameContact.ColliderA;
                var component1 = previousFrameContact.ColliderB;
                if (component == component0 || component == component1)
                {
                    previousToRemove.Add(previousFrameContact);
                    ContactRemoval(previousFrameContact, component0, component1);
                }
            }

            foreach (var contactPoint in previousToRemove)
            {
                previousFrameContacts.Remove(contactPoint);
            }
        }
    }
}

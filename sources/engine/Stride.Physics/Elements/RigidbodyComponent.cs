// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Rendering;

namespace Xenko.Physics
{
    [DataContract("RigidbodyComponent")]
    [Display("Rigidbody")]
    public sealed class RigidbodyComponent : PhysicsSkinnedComponentBase
    {
        [DataMemberIgnore]
        internal BulletSharp.RigidBody InternalRigidBody;

        [DataMemberIgnore]
        internal XenkoMotionState MotionState;

        private float mass = 1.0f;
        private RigidBodyTypes type;
        private Vector3 gravity = Vector3.Zero;
        private float angularDamping;
        private float linearDamping;
        private bool overrideGravity;

        /// <summary>
        /// Gets the linked constraints.
        /// </summary>
        /// <value>
        /// The linked constraints.
        /// </value>
        [DataMemberIgnore]
        public List<Constraint> LinkedConstraints { get; }

        public RigidbodyComponent()
        {
            LinkedConstraints = new List<Constraint>();
            ProcessCollisions = true;
        }

        /// <summary>
        /// Gets or sets the kinematic property
        /// </summary>
        /// <value>true, false</value>
        /// <userdoc>
        /// Move the rigidbody only by the transform property, not other forces
        /// </userdoc>
        [DataMember(75)]
        public bool IsKinematic
        {
            get { return RigidBodyType == RigidBodyTypes.Kinematic; }
            set
            {
                RigidBodyType = value ? RigidBodyTypes.Kinematic : RigidBodyTypes.Dynamic;
            }
        }

        /// <summary>
        /// Gets or sets the mass of this Rigidbody
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// Objects with higher mass push objects with lower mass more when they collide. For large differences, use point values; for example, write 0.1 or 10, not 1 or 100000.
        /// </userdoc>
        [DataMember(80)]
        [DataMemberRange(0, 6)]
        public float Mass
        {
            get
            {
                return mass;
            }
            set
            {
                if (value < 0)
                {
                    throw new InvalidOperationException("the Mass of a Rigidbody cannot be negative.");
                }

                mass = value;

                if (InternalRigidBody == null) return;

                var inertia = ColliderShape.InternalShape.CalculateLocalInertia(value);
                InternalRigidBody.SetMassProps(value, inertia);
                InternalRigidBody.UpdateInertiaTensor(); //this was the major headache when I had to debug Slider and Hinge constraint
            }
        }

        /// <summary>
        /// Gets the collider shape.
        /// </summary>
        /// <value>
        /// The collider shape
        /// </value>
        [DataMemberIgnore]
        public override ColliderShape ColliderShape
        {
            get
            {
                return colliderShape;
            }
            set
            {
                colliderShape = value;

                if (value == null)
                    return;

                if (InternalRigidBody == null)
                    return;

                if (NativeCollisionObject != null)
                    NativeCollisionObject.CollisionShape = value.InternalShape;

                var inertia = colliderShape.InternalShape.CalculateLocalInertia(mass);
                InternalRigidBody.SetMassProps(mass, inertia);
                InternalRigidBody.UpdateInertiaTensor(); //this was the major headache when I had to debug Slider and Hinge constraint
            }
        }

        /// <summary>
        /// Gets or sets the linear damping of this rigidbody
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// The amount of damping for directional forces
        /// </userdoc>
        [DataMember(85)]
        public float LinearDamping
        {
            get
            {
                return linearDamping;
            }
            set
            {
                linearDamping = value;

                InternalRigidBody?.SetDamping(value, AngularDamping);
            }
        }

        /// <summary>
        /// Gets or sets the angular damping of this rigidbody
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// The amount of damping for rotational forces
        /// </userdoc>
        [DataMember(90)]
        public float AngularDamping
        {
            get
            {
                return angularDamping;
            }
            set
            {
                angularDamping = value;

                InternalRigidBody?.SetDamping(LinearDamping, value);
            }
        }

        /// <summary>
        /// Gets or sets if this Rigidbody overrides world gravity
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// Override gravity with the vector specified in Gravity
        /// </userdoc>
        [DataMember(95)]
        public bool OverrideGravity
        {
            get
            {
                return overrideGravity;
            }
            set
            {
                overrideGravity = value;

                if (InternalRigidBody == null) return;

                if (value)
                {
                    if ((InternalRigidBody.Flags & BulletSharp.RigidBodyFlags.DisableWorldGravity) != 0) return;
                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                    InternalRigidBody.Flags |= BulletSharp.RigidBodyFlags.DisableWorldGravity;
                }
                else
                {
                    if ((InternalRigidBody.Flags & BulletSharp.RigidBodyFlags.DisableWorldGravity) == 0) return;
                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                    InternalRigidBody.Flags ^= BulletSharp.RigidBodyFlags.DisableWorldGravity;
                }
            }
        }

        /// <summary>
        /// Gets or sets the gravity acceleration applied to this RigidBody
        /// </summary>
        /// <value>
        /// A vector representing moment and direction
        /// </value>
        /// <userdoc>
        /// The gravity acceleration applied to this rigidbody
        /// </userdoc>
        [DataMember(100)]
        public Vector3 Gravity
        {
            get
            {
                return gravity;
            }
            set
            {
                gravity = value;

                if (InternalRigidBody != null)
                {
                    InternalRigidBody.Gravity = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [DataMemberIgnore]
        public RigidBodyTypes RigidBodyType
        {
            get
            {
                return type;
            }
            set
            {
                type = value;

                if (InternalRigidBody == null)
                {
                    return;
                }

                switch (value)
                {
                    case RigidBodyTypes.Dynamic:
                        InternalRigidBody.CollisionFlags &= ~(BulletSharp.CollisionFlags.StaticObject | BulletSharp.CollisionFlags.KinematicObject);
                        break;

                    case RigidBodyTypes.Static:
                        InternalRigidBody.CollisionFlags &= ~BulletSharp.CollisionFlags.KinematicObject;
                        InternalRigidBody.CollisionFlags |= BulletSharp.CollisionFlags.StaticObject;
                        break;

                    case RigidBodyTypes.Kinematic:
                        InternalRigidBody.CollisionFlags &= ~BulletSharp.CollisionFlags.StaticObject;
                        InternalRigidBody.CollisionFlags |= BulletSharp.CollisionFlags.KinematicObject;
                        break;

                    default:
                        throw new NotSupportedException(nameof(value));
                }
                if (!OverrideGravity)
                {
                    if (value == RigidBodyTypes.Dynamic)
                    {
                        InternalRigidBody.Gravity = Simulation.Gravity;
                    }
                    else
                    {
                        InternalRigidBody.Gravity = Vector3.Zero;
                    }
                }
                InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
                InternalRigidBody.LinearVelocity = Vector3.Zero;
                InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
                InternalRigidBody.AngularVelocity = Vector3.Zero;
            }
        }

        protected override void OnAttach()
        {
            MotionState = new XenkoMotionState(this);

            SetupBoneLink();

            var rbci = new BulletSharp.RigidBodyConstructionInfo(0.0f, MotionState, ColliderShape.InternalShape, Vector3.Zero);
            InternalRigidBody = new BulletSharp.RigidBody(rbci)
            {
                UserObject = this,
            };

            NativeCollisionObject = InternalRigidBody;

            NativeCollisionObject.ContactProcessingThreshold = !Simulation.CanCcd ? 1e18f : 1e30f;

            if (ColliderShape.NeedsCustomCollisionCallback)
            {
                NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.CustomMaterialCallback;
            }

            if (ColliderShape.Is2D) //set different defaults for 2D shapes
            {
                InternalRigidBody.LinearFactor = new Vector3(1.0f, 1.0f, 0.0f);
                InternalRigidBody.AngularFactor = new Vector3(0.0f, 0.0f, 1.0f);
            }

            var inertia = ColliderShape.InternalShape.CalculateLocalInertia(mass);
            InternalRigidBody.SetMassProps(mass, inertia);
            InternalRigidBody.UpdateInertiaTensor(); //this was the major headache when I had to debug Slider and Hinge constraint

            base.OnAttach();

            Mass = mass;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
            OverrideGravity = overrideGravity;
            Gravity = gravity;
            RigidBodyType = IsKinematic ? RigidBodyTypes.Kinematic : RigidBodyTypes.Dynamic;

            Simulation.AddRigidBody(this, (CollisionFilterGroupFlags)CollisionGroup, CanCollideWith);
        }

        protected override void OnDetach()
        {
            MotionState.Dispose();
            MotionState.Clear();

            if (NativeCollisionObject == null)
                return;

            //Remove constraints safely
            var toremove = new FastList<Constraint>();
            foreach (var c in LinkedConstraints)
            {
                toremove.Add(c);
            }

            foreach (var disposable in toremove)
            {
                disposable.Dispose();
            }

            LinkedConstraints.Clear();
            //~Remove constraints

            Simulation.RemoveRigidBody(this);

            InternalRigidBody = null;

            base.OnDetach();
        }

        protected internal override void OnUpdateDraw()
        {
            base.OnUpdateDraw();

            if (type == RigidBodyTypes.Dynamic && BoneIndex != -1)
            {
                //write to ModelViewHierarchy
                var model = Data.ModelComponent;
                model.Skeleton.NodeTransformations[BoneIndex].Flags = !IsKinematic ? ModelNodeFlags.EnableRender | ModelNodeFlags.OverrideWorldMatrix : ModelNodeFlags.Default;
                if (!IsKinematic) model.Skeleton.NodeTransformations[BoneIndex].WorldMatrix = BoneWorldMatrixOut;
            }
        }

        //This is called by the physics engine to update the transformation of Dynamic rigidbodies.
        private void RigidBodySetWorldTransform(ref Matrix physicsTransform)
        {
            Data.PhysicsComponent.Simulation.SimulationProfiler.Mark();
            Data.PhysicsComponent.Simulation.UpdatedRigidbodies++;

            if (BoneIndex == -1)
            {
                UpdateTransformationComponent(ref physicsTransform);
            }
            else
            {
                UpdateBoneTransformation(ref physicsTransform);
            }
        }

        //This is valid for Dynamic rigidbodies (called once at initialization)
        //and Kinematic rigidbodies, called every simulation tick (if body not sleeping) to let the physics engine know where the kinematic body is.
        private void RigidBodyGetWorldTransform(out Matrix physicsTransform)
        {
            Data.PhysicsComponent.Simulation.SimulationProfiler.Mark();
            Data.PhysicsComponent.Simulation.UpdatedRigidbodies++;

            if (BoneIndex == -1)
            {
                DerivePhysicsTransformation(out physicsTransform);
            }
            else
            {
                DeriveBonePhysicsTransformation(out physicsTransform);
            }
        }

        /// <summary>
        /// Gets the total torque.
        /// </summary>
        /// <value>
        /// The total torque.
        /// </value>
        public Vector3 TotalTorque => InternalRigidBody?.TotalTorque ?? Vector3.Zero;

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        /// <param name="impulse">The impulse.</param>
        public void ApplyImpulse(Vector3 impulse)
        {
            if (InternalRigidBody == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            InternalRigidBody.ApplyCentralImpulse(impulse);
        }

        /// <summary>
        /// Applies the impulse.
        /// </summary>
        /// <param name="impulse">The impulse.</param>
        /// <param name="localOffset">The local offset.</param>
        public void ApplyImpulse(Vector3 impulse, Vector3 localOffset)
        {
            if (InternalRigidBody == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            InternalRigidBody.ApplyImpulse(impulse, localOffset);
        }

        /// <summary>
        /// Applies the force.
        /// </summary>
        /// <param name="force">The force.</param>
        public void ApplyForce(Vector3 force)
        {
            if (InternalRigidBody == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            InternalRigidBody.ApplyCentralForce(force);
        }

        /// <summary>
        /// Applies the force.
        /// </summary>
        /// <param name="force">The force.</param>
        /// <param name="localOffset">The local offset.</param>
        public void ApplyForce(Vector3 force, Vector3 localOffset)
        {
            if (InternalRigidBody == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            InternalRigidBody.ApplyForce(force, localOffset);
        }

        /// <summary>
        /// Applies the torque.
        /// </summary>
        /// <param name="torque">The torque.</param>
        public void ApplyTorque(Vector3 torque)
        {
            if (InternalRigidBody == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            InternalRigidBody.ApplyTorque(torque);
        }

        /// <summary>
        /// Applies the torque impulse.
        /// </summary>
        /// <param name="torque">The torque.</param>
        public void ApplyTorqueImpulse(Vector3 torque)
        {
            if (InternalRigidBody == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            InternalRigidBody.ApplyTorqueImpulse(torque);
        }

        /// <summary>
        /// Clears all forces being applied to this rigidbody
        /// </summary>
        public void ClearForces()
        {
            if (InternalRigidBody == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            InternalRigidBody?.ClearForces();
            InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
            InternalRigidBody.LinearVelocity = Vector3.Zero;
            InternalRigidBody.InterpolationAngularVelocity = Vector3.Zero;
            InternalRigidBody.AngularVelocity = Vector3.Zero;
        }

        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>
        /// The angular velocity.
        /// </value>
        [DataMemberIgnore]
        public Vector3 AngularVelocity
        {
            get
            {
                return InternalRigidBody?.AngularVelocity ?? Vector3.Zero;
            }
            set
            {
                if (InternalRigidBody == null)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }

                InternalRigidBody.AngularVelocity = value;
            }
        }

        /// <summary>
        /// Gets or sets the linear velocity.
        /// </summary>
        /// <value>
        /// The linear velocity.
        /// </value>
        [DataMemberIgnore]
        public Vector3 LinearVelocity
        {
            get
            {
                return InternalRigidBody?.LinearVelocity ?? Vector3.Zero;
            }
            set
            {
                if (InternalRigidBody == null)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }

                InternalRigidBody.LinearVelocity = value;
            }
        }

        /// <summary>
        /// Gets the total force.
        /// </summary>
        /// <value>
        /// The total force.
        /// </value>
        public Vector3 TotalForce => InternalRigidBody?.TotalForce ?? Vector3.Zero;

        /// <summary>
        /// Gets or sets the angular factor.
        /// </summary>
        /// <value>
        /// The angular factor.
        /// </value>
        [DataMemberIgnore]
        public Vector3 AngularFactor
        {
            get
            {
                return InternalRigidBody?.AngularFactor ?? Vector3.Zero;
            }
            set
            {
                if (InternalRigidBody == null)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }

                InternalRigidBody.AngularFactor = value;
            }
        }

        /// <summary>
        /// Gets or sets the linear factor.
        /// </summary>
        /// <value>
        /// The linear factor.
        /// </value>
        [DataMemberIgnore]
        public Vector3 LinearFactor
        {
            get
            {
                return InternalRigidBody?.LinearFactor ?? Vector3.Zero;
            }
            set
            {
                if (InternalRigidBody == null)
                {
                    throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
                }

                InternalRigidBody.LinearFactor = value;
            }
        }

        internal class XenkoMotionState : BulletSharp.MotionState
        {
            private RigidbodyComponent rigidBody;

            public XenkoMotionState(RigidbodyComponent rb)
            {
                rigidBody = rb;
            }

            public void Clear()
            {
                rigidBody = null;
            }

            public override void GetWorldTransform(out BulletSharp.Math.Matrix transform)
            {
                rigidBody.RigidBodyGetWorldTransform(out var xenkoMatrix);
                transform = xenkoMatrix;
            }

            public override void SetWorldTransform(ref BulletSharp.Math.Matrix transform)
            {
                Matrix asXenkoMatrix = transform;
                rigidBody.RigidBodySetWorldTransform(ref asXenkoMatrix);
            }
        }
    }
}

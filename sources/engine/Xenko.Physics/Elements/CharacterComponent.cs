// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Physics
{
    [DataContract("CharacterComponent")]
    [Display("Character")]
    public sealed class CharacterComponent : PhysicsComponent
    {
        public CharacterComponent()
        {
            StepHeight = 0.1f;
            ProcessCollisions = true;
        }

        /// <summary>
        /// Jumps this instance.
        /// </summary>
        public void Jump(Vector3 jumpDirection)
        {
            if (KinematicCharacter == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            KinematicCharacter.Jump(ref jumpDirection);
        }

        /// <summary>
        /// Jumps this instance.
        /// </summary>
        public void Jump()
        {
            if (KinematicCharacter == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            var zeroV = Vector3.Zero; //passing zero will jump on Up Axis
            KinematicCharacter.Jump(ref zeroV);
        }

        /// <summary>
        /// Gets or sets the height of the character step.
        /// </summary>
        /// <value>
        /// The height of the character step.
        /// </value>
        /// <userdoc>
        /// Only valid for CharacterController type, describes the max slope height a character can climb. Cannot change during run-time.
        /// </userdoc>
        [DataMember(75)]
        [DefaultValue(0.1f)]
        public float StepHeight { get; set; }

        private float fallSpeed = 10.0f;

        /// <summary>
        /// Gets or sets if this character element fall speed
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// The fall speed of this character
        /// </userdoc>
        [DataMember(80)]
        public float FallSpeed
        {
            get
            {
                return fallSpeed;
            }
            set
            {
                fallSpeed = value;
                
                KinematicCharacter?.SetFallSpeed(value);
            }
        }

        private AngleSingle maxSlope = new AngleSingle(45, AngleType.Degree);

        /// <summary>
        /// Gets or sets if this character element max slope
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// The max slope this character can climb
        /// </userdoc>
        [Display("Maximum Slope")]
        [DataMember(85)]
        public AngleSingle MaxSlope
        {
            get
            {
                return maxSlope;
            }
            set
            {
                maxSlope = value;

                if (KinematicCharacter != null)
                {
                    KinematicCharacter.MaxSlope = value.Radians;
                }
            }
        }

        private float jumpSpeed = 5.0f;

        /// <summary>
        /// Gets or sets if this character jump speed
        /// </summary>
        /// <value>
        /// A float representing character jump speed in Xenko world units
        /// </value>
        /// <userdoc>
        /// The speed of the jump
        /// </userdoc>
        [DataMember(90)]
        public float JumpSpeed
        {
            get
            {
                return jumpSpeed;
            }
            set
            {
                jumpSpeed = value;

                KinematicCharacter?.SetJumpSpeed(value);
            }
        }

        private Vector3 gravity = new Vector3(0.0f, -10.0f, 0.0f);

        /// <summary>
        /// Gets or sets if this character is affected by any gravity
        /// </summary>
        /// <value>
        /// A Vector3 representing directional gravity in Xenko world units
        /// </value>
        /// <userdoc>
        /// The gravity force applied to this character
        /// </userdoc>
        [Display("Gravity")]
        [DataMember(95)]
        public Vector3 Gravity
        {
            get
            {
                return gravity;
            }
            set
            {
                gravity = value;

                if (KinematicCharacter != null)
                {
                    KinematicCharacter.Gravity = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is on the ground.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is grounded; otherwise, <c>false</c>.
        /// </value>
        public bool IsGrounded => KinematicCharacter?.OnGround() ?? false;

        /// <summary>
        /// Teleports the specified target position.
        /// </summary>
        /// <param name="targetPosition">The target position.</param>
        public void Teleport(Vector3 targetPosition)
        {
            if (KinematicCharacter == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            //we assume that the user wants to teleport in world/entity space
            var entityPos = Entity.Transform.Position;
            var physPos = PhysicsWorldTransform.TranslationVector;
            var diff = physPos - entityPos;
            KinematicCharacter.Warp(targetPosition + diff);
        }

        /// <summary>
        /// Moves the character towards the specified movement vector.
        /// Motion will stay in place unless modified or canceled passing Vector3.Zero.
        /// </summary>
        /// <param name="movement">The velocity vector, typically direction * delta time `var dt = this.GetSimulation().FixedTimeStep;` * speed.</param>
        [Obsolete("Please use SetVelocity instead. SetVelocity internally applies this.GetSimulation().FixedTimeStep")]
        public void Move(Vector3 movement)
        {
            if (KinematicCharacter == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            KinematicCharacter.SetWalkDirection(movement);
        }

        /// <summary>
        /// Sets the character velocity.
        /// Velocity will be applied every frame unless modified or canceled passing Vector3.Zero.
        /// </summary>
        /// <remarks>The engine internally will multiply velocity with the simulation fixed time step.</remarks>
        /// <param name="velocity">The velocity vector, typically direction * speed.</param>
        public void SetVelocity(Vector3 velocity)
        {
            if (KinematicCharacter == null)
            {
                throw new InvalidOperationException("Attempted to call a Physics function that is avaliable only when the Entity has been already added to the Scene.");
            }

            KinematicCharacter.SetWalkDirection(velocity * Simulation.FixedTimeStep);
        }

        /// <summary>
        /// Sets or gets the orientation of the Entity attached to this character controller
        /// </summary>
        /// <remarks>This orientation has no impact in the physics simulation</remarks>
        [DataMemberIgnore]
        public Quaternion Orientation { get; set; }

        [DataMemberIgnore]
        internal BulletSharp.KinematicCharacterController KinematicCharacter;

        protected override void OnAttach()
        {
            NativeCollisionObject = new BulletSharp.PairCachingGhostObject
            {
                CollisionShape = ColliderShape.InternalShape,
                UserObject = this,
            };

            NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.CharacterObject;

            if (ColliderShape.NeedsCustomCollisionCallback)
            {
                NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.CustomMaterialCallback;
            }

            NativeCollisionObject.ContactProcessingThreshold = !Simulation.CanCcd ? 1e18f : 1e30f;

            KinematicCharacter = new BulletSharp.KinematicCharacterController((BulletSharp.PairCachingGhostObject)NativeCollisionObject, (BulletSharp.ConvexShape)ColliderShape.InternalShape, StepHeight, Vector3.UnitY);

            base.OnAttach();

            FallSpeed = fallSpeed;
            MaxSlope = maxSlope;
            JumpSpeed = jumpSpeed;
            Gravity = gravity;

            UpdatePhysicsTransformation(); //this will set position and rotation of the collider

            Simulation.AddCharacter(this, (CollisionFilterGroupFlags)CollisionGroup, CanCollideWith);
        }

        protected override void OnDetach()
        {
            if (KinematicCharacter == null) return;

            Simulation.RemoveCharacter(this);

            KinematicCharacter.Dispose();
            KinematicCharacter = null;

            base.OnDetach();
        }
    }
}

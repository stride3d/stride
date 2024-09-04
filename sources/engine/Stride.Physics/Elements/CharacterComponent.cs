// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics
{
    [DataContract("CharacterComponent")]
    [Display("Character")]
    public sealed class CharacterComponent : PhysicsComponent
    {
        public CharacterComponent()
        {
            Orientation = Quaternion.Identity;
            StepHeight = 0.1f;
        }

        /// <summary>
        /// Jumps this instance.
        /// </summary>
        public void Jump(Vector3 jumpDirection)
        {
            if (KinematicCharacter == null)
            {
                LogPhysicsFunctionError();
                return;
            }
            BulletSharp.Math.Vector3 bV3 = jumpDirection;
            KinematicCharacter.Jump(ref bV3);
        }

        /// <summary>
        /// Jumps this instance.
        /// </summary>
        public void Jump()
        {
            if (KinematicCharacter == null)
            {
                LogPhysicsFunctionError();
                return;
            }
            KinematicCharacter.Jump();
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

                if (KinematicCharacter != null)
                {
                    KinematicCharacter.FallSpeed = fallSpeed;
                }
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

        /// <summary>
        /// Gets the linear velocity from the kinematic character
        /// </summary>
        /// <value>
        /// Vector3
        /// </value>
        /// <userdoc>
        /// The linear speed of the character component
        /// </userdoc>
        [DataMemberIgnore]
        public Vector3 LinearVelocity
        {
            get
            {
                return KinematicCharacter != null ? KinematicCharacter.LinearVelocity : Vector3.Zero;
            }
        }

        private float jumpSpeed = 5.0f;

        /// <summary>
        /// Gets or sets if this character jump speed
        /// </summary>
        /// <value>
        /// A float representing character jump speed in Stride world units
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

                if (KinematicCharacter != null)
                {
                    KinematicCharacter.JumpSpeed = jumpSpeed;
                }
            }
        }

        private Vector3 gravity = new Vector3(0.0f, -10.0f, 0.0f);

        /// <summary>
        /// Gets or sets if this character is affected by any gravity
        /// </summary>
        /// <value>
        /// A Vector3 representing directional gravity in Stride world units
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
        public bool IsGrounded => KinematicCharacter?.OnGround ?? false;

        /// <summary>
        /// Teleports the specified target position.
        /// </summary>
        /// <param name="targetPosition">The target position.</param>
        public void Teleport(Vector3 targetPosition)
        {
            if (KinematicCharacter == null)
            {
                LogPhysicsFunctionError();
                return;
            }

            //we assume that the user wants to teleport in world/entity space
            var entityPos = Entity.Transform.Position;
            var physPos = PhysicsWorldTransform.TranslationVector;
            var diff = physPos - entityPos;
            BulletSharp.Math.Vector3 bV3 = targetPosition + diff;
            KinematicCharacter.Warp(ref bV3);
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
                LogPhysicsFunctionError();
                return;
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
                LogPhysicsFunctionError();
                return;
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

            BulletSharp.Math.Vector3 unitY = new BulletSharp.Math.Vector3(0f, 1f, 0f);
            KinematicCharacter = new BulletSharp.KinematicCharacterController((BulletSharp.PairCachingGhostObject)NativeCollisionObject, (BulletSharp.ConvexShape)ColliderShape.InternalShape, StepHeight, ref unitY);

            base.OnAttach();

            FallSpeed = fallSpeed;
            MaxSlope = maxSlope;
            JumpSpeed = jumpSpeed;
            Gravity = gravity;

            UpdatePhysicsTransformation(); //this will set position and rotation of the collider

            Simulation.AddCharacter(this, (CollisionFilterGroupFlags)CollisionGroup, CanCollideWith);
        }

        /// <summary>
        /// Reconstruct of character controller to avoid possible UAF issues
        /// Context: On ColliderShape changes, when ComposeShape is ran to rebuild ColliderShape properties, disposing the old ColliderShape can cause UAF
        /// issues inside KinematicCharacter and inside Simulation discreteDynamicWorld due to old disposed references to the native object.
        /// </summary>
        public override void ComposeShape()
        {
            //Disposing of ColliderShape should happen before we remove NativeCollisionObject and KinematicCharacter from Simulation
            base.ComposeShape();

            //make PairCachingGhostObject references in KinematicCharacter valid, therefore make new instance of kinematic character controller with  
            //updated NativeCollisionObject keep references valid
            if (KinematicCharacter != null)
            {
                //very mediocre workaround to avoid the nullref when we remove character
                Simulation simRef = Simulation;

                //remove references in discreteDynamicsWorld
                Simulation.RemoveCharacter(this);
                Simulation = simRef;

                //destroy and deref KinematicCharacter then reconstruct it
                BulletSharp.KinematicCharacterController kinematicCharacterProperties = KinematicCharacter;
                KinematicCharacter.Dispose();
                KinematicCharacter = null;
                BulletSharp.Math.Vector3 unitY = new BulletSharp.Math.Vector3(0f, 1f, 0f);
                //Make new KinematicCharacter
                KinematicCharacter = new BulletSharp.KinematicCharacterController((BulletSharp.PairCachingGhostObject)NativeCollisionObject, (BulletSharp.ConvexShape)ColliderShape.InternalShape, StepHeight, ref unitY);
                OverrideKinematicCharacterValues(kinematicCharacterProperties);

                //now we can add these references BACK in Simulation's discreteDynamicsWorld
                Simulation.AddCharacter(this, (CollisionFilterGroupFlags)CollisionGroup, CanCollideWith);
            }
        }

        /// <summary>
        /// When we reconstruct our KinematicCharacter, we would want to preserve some physics properties. Try to have the
        /// physics of the object try to match as close as possible.
        /// </summary>
        /// <param name="oldController"></param>
        private void OverrideKinematicCharacterValues(BulletSharp.KinematicCharacterController oldController)
        {
            KinematicCharacter.FallSpeed = oldController.FallSpeed;
            KinematicCharacter.Gravity = oldController.Gravity;
            KinematicCharacter.JumpSpeed = oldController.JumpSpeed;
            KinematicCharacter.LinearVelocity = oldController.LinearVelocity;
        }

        protected override void OnDetach()
        {
            if (KinematicCharacter == null) return;

            Simulation.RemoveCharacter(this);

            KinematicCharacter.Dispose();
            KinematicCharacter = null;

            base.OnDetach();
        }

        /// <summary>
        /// Run specific error when physics functions are called on components that do not have proper setup.
        /// Captures good tracing info for debugging purposes.
        /// </summary>
        private void LogPhysicsFunctionError()
        {
            StackFrame frame = new StackTrace(true).GetFrame(2);
            logger.Error($"Component:[{this}] attempted to call a Physics function that is available only when the Entity has been already added to the Scene. " +
                $"This may be due to a {this} without any physical shapes.\nLocation: {frame.GetFileName()} at Line Number: {frame.GetFileLineNumber()} from Method: {frame.GetMethod().Name} ");
        }
    }
}

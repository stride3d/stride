// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;

namespace ThirdPersonPlatformer.Player
{
    public class PlayerController : SyncScript
    {
        [Display("Run Speed")]
        public float MaxRunSpeed { get; set; } = 10;

        public static readonly EventKey<bool> IsGroundedEventKey = new EventKey<bool>();

        public static readonly EventKey<float> RunSpeedEventKey = new EventKey<float>();

        // This component is the physics representation of a controllable character
        private CharacterComponent character;
        private Entity modelChildEntity;

        private float yawOrientation;

        private readonly EventReceiver<Vector3> moveDirectionEvent = new EventReceiver<Vector3>(PlayerInput.MoveDirectionEventKey);

        private readonly EventReceiver<bool> jumpEvent = new EventReceiver<bool>(PlayerInput.JumpEventKey);

        /// <summary>
        /// Allow for some latency from the user input to make jumping appear more natural
        /// </summary>
        [Display("Jump Time Limit")]
        public float JumpReactionThreshold { get; set; } = 0.3f;

        // When the character falls off a surface, allow for some reaction time
        private float jumpReactionRemaining;

        // Allow some inertia to the movement
        private Vector3 moveDirection = Vector3.Zero;

        /// <summary>
        /// Called when the script is first initialized
        /// </summary>
        public override void Start()
        {
            base.Start();

            jumpReactionRemaining = JumpReactionThreshold;

            // Will search for an CharacterComponent within the same entity as this script
            character = Entity.Get<CharacterComponent>();
            if (character == null) throw new ArgumentException("Please add a CharacterComponent to the entity containing PlayerController!");

            modelChildEntity = Entity.GetChild(0);
        }

        /// <summary>
        /// Called on every frame update
        /// </summary>
        public override void Update()
        {
            // var dt = Game.UpdateTime.Elapsed.Milliseconds * 0.001;
            Move(MaxRunSpeed);

            Jump();
        }

        /// <summary>
        /// Jump makes the character jump and also accounts for the player's reaction time, making jumping feel more natural by
        ///  allowing jumps within some limit of the last time the character was on the ground
        /// </summary>
        private void Jump()
        {
            var dt = this.GetSimulation().FixedTimeStep;

            // Check if conditions allow the character to jump
            if (JumpReactionThreshold <= 0)
            {
                // No reaction threshold. The character can only jump if grounded
                if (!character.IsGrounded)
                {
                    IsGroundedEventKey.Broadcast(false);
                    return;
                }
            }
            else
            {
                // If there is still enough time left for jumping allow the character to jump even when not grounded
                if (jumpReactionRemaining > 0)
                    jumpReactionRemaining -= dt;

                // If the character on the ground reset the jumping reaction time
                if (character.IsGrounded)
                    jumpReactionRemaining = JumpReactionThreshold;

                // If there is no more reaction time left don't allow the character to jump
                if (jumpReactionRemaining <= 0)
                {
                    IsGroundedEventKey.Broadcast(character.IsGrounded);
                    return;
                }
            }

            // If the player didn't press a jump button we don't need to jump
            bool didJump;
            jumpEvent.TryReceive(out didJump);
            if (!didJump)
            {
                IsGroundedEventKey.Broadcast(true);
                return;
            }

            // Jump!!
            jumpReactionRemaining = 0;
            character.Jump();

            // Broadcast that the character is jumping!
            IsGroundedEventKey.Broadcast(false);
        }

        private void Move(float speed)
        {
            // Character speed
            Vector3 newMoveDirection;
            moveDirectionEvent.TryReceive(out newMoveDirection);

            // Allow very simple inertia to the character to make animation transitions more fluid
            moveDirection = moveDirection*0.85f + newMoveDirection *0.15f;

            character.SetVelocity(moveDirection * speed);

            // Broadcast speed as per cent of the max speed
            RunSpeedEventKey.Broadcast(moveDirection.Length());

            // Character orientation
            if (moveDirection.Length() > 0.001)
            {
                yawOrientation = MathUtil.RadiansToDegrees((float) Math.Atan2(-moveDirection.Z, moveDirection.X) + MathUtil.PiOverTwo);
            }
            modelChildEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(yawOrientation), 0, 0);
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
// using FirstPersonShooter.Core; // MODIFIED: Removed ITargetable and this using for now

namespace MySurvivalGame.Game // MODIFIED: Namespace updated
{
    public class PlayerController : SyncScript // MODIFIED: Removed ITargetable
    {
        [Display("Run Speed")]
        public float MaxRunSpeed { get; set; } = 5;

        public static readonly EventKey<float> RunSpeedEventKey = new EventKey<float>(); // This can be used by an animation controller later

        // This component is the physics representation of a controllable character
        private CharacterComponent character;

        // Correctly references PlayerInput from MySurvivalGame.Game namespace
        private readonly EventReceiver<Vector3> moveDirectionEvent = new EventReceiver<Vector3>(PlayerInput.MoveDirectionEventKey);

        /// <summary>
        /// Called when the script is first initialized
        /// </summary>
        public override void Start()
        {
            base.Start();

            // Will search for an CharacterComponent within the same entity as this script
            character = Entity.Get<CharacterComponent>();
            if (character == null) throw new ArgumentException("Please add a CharacterComponent to the entity containing PlayerController!");
        }
        
        /// <summary>
        /// Called on every frame update
        /// </summary>
        public override void Update()
        {
            Move();
        }

        private void Move()
        {
            // Character speed
            Vector3 moveDirection = Vector3.Zero;
            if (moveDirectionEvent.TryReceive(out moveDirection)) // Check if event was received
            {
                // The moveDirection received from PlayerInput is already world-space relative to camera
                character.SetVelocity(moveDirection * MaxRunSpeed);

                // Broadcast normalized speed (magnitude of the input vector, could be > 1 if diagonal on keyboard)
                RunSpeedEventKey.Broadcast(moveDirection.Length());
            }
            else
            {
                // Optional: If no movement input, explicitly set velocity to zero or apply damping
                character.SetVelocity(Vector3.Zero); // Or character.SetVelocity(character.LinearVelocity * dampingFactor);
                RunSpeedEventKey.Broadcast(0f);
            }
        }

        // MODIFIED: Removed ITargetable Implementation
        /*
        #region ITargetable Implementation
        public Vector3 GetTargetPosition()
        {
            // Return a point roughly in the center of the character model
            return Entity.Transform.Position + new Vector3(0, 1.0f, 0); // Assuming player origin is at feet, target is 1m up.
        }

        public Entity GetEntity()
        {
            return this.Entity;
        }
        #endregion
        */
    }
}

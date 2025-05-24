// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Player; // For potential interaction with player components

namespace FirstPersonShooter.Weapons.Melee
{
    public class Hatchet : BaseWeapon
    {
        /// <summary>
        /// The effective range of the hatchet's swing.
        /// </summary>
        public float AttackRange { get; set; } = 1.5f;

        private float cooldownRemaining = 0f;

        public Hatchet()
        {
            // Default values for a hatchet
            Durability = 150f;
            AttackRate = 1.2f; // Swings per second
            Damage = 25f;      // Damage per hit
        }

        public override void Start()
        {
            base.Start(); // Ensure BaseWeapon.Start() is called if it ever has logic
        }
        
        /// <summary>
        /// Called on every frame update to manage cooldown.
        /// </summary>
        public override void Update()
        {
            if (cooldownRemaining > 0)
            {
                cooldownRemaining -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (cooldownRemaining < 0)
                {
                    cooldownRemaining = 0;
                }
            }
        }

        /// <summary>
        /// Performs the primary action of the hatchet (a melee swing).
        /// </summary>
        public override void PrimaryAction()
        {
            if (OwnerEntity == null)
            {
                Log.Warning("Hatchet.PrimaryAction called but OwnerEntity is null. Cannot determine attack origin.");
                return;
            }

            if (cooldownRemaining > 0)
            {
                // Still in cooldown
                // Log.Info("Hatchet on cooldown."); // Optional: for debugging
                return;
            }

            // Set cooldown
            cooldownRemaining = 1.0f / AttackRate;
            // Log.Info($"Hatchet swung! Cooldown set to {cooldownRemaining}s."); // Optional: for debugging

            // Perform melee attack logic
            // The attack should originate from the player/camera.
            // We need a reference to the camera or player's view direction.
            // Assuming OwnerEntity has the PlayerCamera script or similar to get view.
            // For now, let's assume the OwnerEntity's forward direction is the view direction.
            // A more robust solution would be to get the camera from PlayerSetup on OwnerEntity or similar.

            var simulation = this.GetSimulation();
            if (simulation == null)
            {
                Log.Error("Hatchet.PrimaryAction: No physics simulation found!");
                return;
            }

            // Determine raycast origin and direction
            // Option 1: Use OwnerEntity's transform directly (if player model itself rotates with view)
            // var raycastStart = OwnerEntity.Transform.WorldMatrix.TranslationVector;
            // var forward = OwnerEntity.Transform.WorldMatrix.Forward;

            // Option 2: More robustly, find the Camera associated with the player (OwnerEntity)
            // This requires PlayerEquipment or PlayerController to pass camera reference or PlayerCamera to be findable
            // For this example, let's assume PlayerInput on OwnerEntity has a CameraComponent reference.
            // If not, this part will need careful wiring in the actual game setup.
            
            Matrix viewMatrix;
            var playerInput = OwnerEntity.Get<PlayerInput>(); // PlayerInput is usually on the Player entity
            if (playerInput != null && playerInput.Camera != null)
            {
                // Use the camera's position and forward direction
                viewMatrix = playerInput.Camera.ViewMatrix; // This is view matrix, need to invert for world space
                viewMatrix.Invert(); // Now worldMatrix of the camera
            }
            else
            {
                // Fallback to OwnerEntity's transform if no camera found on PlayerInput
                // This might not be aligned with player's actual view if camera has independent rotation (pitch)
                Log.Warning("Hatchet could not find PlayerInput.Camera. Falling back to OwnerEntity's orientation for attack direction.");
                viewMatrix = OwnerEntity.Transform.WorldMatrix;
            }
            
            var raycastStart = viewMatrix.TranslationVector;
            var forward = viewMatrix.Forward;
            var raycastEnd = raycastStart + forward * AttackRange;

            var hitResult = simulation.Raycast(raycastStart, raycastEnd);

            if (hitResult.Succeeded)
            {
                var hitEntity = hitResult.Collider.Entity;
                Log.Info($"Hatchet hit [{hitEntity.Name}] at distance {hitResult.Distance}!");
                // Future: Apply damage, effects, etc.
                // e.g., var damageable = hitEntity.Get<IDamageable>();
                // if (damageable != null) damageable.TakeDamage(Damage, OwnerEntity);
            }
            else
            {
                // Log.Info("Hatchet swing missed."); // Optional: for debugging
            }
        }

        // SecondaryAction and Reload are inherited from BaseWeapon and do nothing by default.
        // No specific override needed for Hatchet unless it has unique secondary or reload actions.
    }
}

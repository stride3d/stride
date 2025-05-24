// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq; // For FirstOrDefault
using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Weapons.Projectiles; // For ArrowProjectile
using FirstPersonShooter.Player; // For PlayerInput to get camera view

namespace FirstPersonShooter.Weapons.Ranged
{
    public abstract class BaseBowWeapon : BaseWeapon
    {
        /// <summary>
        /// Prefab for the arrow to be shot. Assign this in Stride Editor.
        /// The prefab should have ArrowProjectile.cs and a RigidbodyComponent.
        /// </summary>
        public Prefab ArrowPrefab { get; set; }

        /// <summary>
        /// Time in seconds to reach full draw strength.
        /// </summary>
        public float DrawTime { get; set; } = 1.0f;

        /// <summary>
        /// Base launch speed of the arrow at full draw.
        /// </summary>
        public float ArrowLaunchSpeed { get; set; } = 30f;

        protected bool IsDrawing { get; set; } = false;
        protected float CurrentDrawStrength { get; set; } = 0f; // 0.0 to 1.0

        // Cooldown for firing, to prevent immediate re-draw after a shot
        private float shotCooldownRemaining = 0f;


        public override void Update()
        {
            base.Update(); // BaseWeapon.Update() if it has logic

            if (IsDrawing)
            {
                CurrentDrawStrength += (float)Game.UpdateTime.Elapsed.TotalSeconds / DrawTime;
                CurrentDrawStrength = MathUtil.Clamp(CurrentDrawStrength, 0f, 1f);
                // Log.Info($"Drawing bow: {CurrentDrawStrength * 100:F0}%"); // Optional: for debugging
            }

            if (shotCooldownRemaining > 0)
            {
                shotCooldownRemaining -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (shotCooldownRemaining < 0) shotCooldownRemaining = 0;
            }
        }

        public override void PrimaryAction() // Called when shoot button is pressed
        {
            base.PrimaryAction(); // Checks IsBroken
            if (IsBroken) return;

            if (shotCooldownRemaining > 0)
            {
                // Log.Info("Bow is on nocking cooldown."); // Optional
                return;
            }

            if (!IsDrawing)
            {
                IsDrawing = true;
                CurrentDrawStrength = 0f;
                Log.Info($"{Entity?.Name ?? "Bow"}: Started drawing.");
            }
            // If already drawing, PrimaryAction press does nothing further until released.
        }

        public virtual void OnPrimaryActionReleased() // Called when shoot button is released
        {
            if (!IsDrawing) return;

            Log.Info($"{Entity?.Name ?? "Bow"}: Shot released with strength: {CurrentDrawStrength * 100:F0}%");

            if (CurrentDrawStrength < 0.1f) // Threshold for a valid shot
            {
                Log.Info($"{Entity?.Name ?? "Bow"}: Shot too weak.");
                IsDrawing = false;
                CurrentDrawStrength = 0f;
                return;
            }

            if (ArrowPrefab == null)
            {
                Log.Error($"{Entity?.Name ?? "Bow"}: ArrowPrefab is not assigned!");
                IsDrawing = false;
                CurrentDrawStrength = 0f;
                return;
            }

            // Instantiate and configure the arrow
            var arrowInstances = ArrowPrefab.Instantiate();
            if (arrowInstances == null || !arrowInstances.Any())
            {
                Log.Error($"{Entity?.Name ?? "Bow"}: Failed to instantiate ArrowPrefab.");
                IsDrawing = false;
                CurrentDrawStrength = 0f;
                return;
            }
            var arrowEntityInstance = arrowInstances.First();

            var arrowScript = arrowEntityInstance.Get<ArrowProjectile>();
            var actualLaunchSpeed = ArrowLaunchSpeed * CurrentDrawStrength;

            if (arrowScript != null)
            {
                arrowScript.Damage = this.Damage * CurrentDrawStrength; // Scale damage by draw strength
                arrowScript.InitialSpeed = actualLaunchSpeed;
            }
            else
            {
                Log.Warning($"{Entity?.Name ?? "Bow"}: ArrowPrefab does not contain an ArrowProjectile script. Speed/Damage not set on projectile.");
            }

            // Position and orient the arrow
            // This should ideally be from a specific "Muzzle" or "ArrowSpawn" point on the bow model,
            // aligned with player's view. For now, using OwnerEntity's camera/view.
            if (OwnerEntity == null)
            {
                Log.Error($"{Entity?.Name ?? "Bow"}: OwnerEntity is null. Cannot determine arrow spawn transform.");
                IsDrawing = false;
                CurrentDrawStrength = 0f;
                return;
            }

            Matrix spawnTransformMatrix;
            var playerInput = OwnerEntity.Get<PlayerInput>();
            if (playerInput != null && playerInput.Camera != null)
            {
                var cameraMatrix = playerInput.Camera.ViewMatrix; // This is View Matrix
                cameraMatrix.Invert(); // This is now World Matrix of Camera
                spawnTransformMatrix = cameraMatrix;
            }
            else
            {
                Log.Warning($"{Entity?.Name ?? "Bow"}: Could not find PlayerInput.Camera. Using OwnerEntity's world transform for arrow spawn.");
                spawnTransformMatrix = OwnerEntity.Transform.WorldMatrix;
            }
            
            // Apply world transformation to the arrow entity
            arrowEntityInstance.Transform.Position = spawnTransformMatrix.TranslationVector;
            arrowEntityInstance.Transform.Rotation = Quaternion.RotationMatrix(spawnTransformMatrix);
            // Potentially offset position slightly forward from camera to avoid immediate collision
            arrowEntityInstance.Transform.Position += spawnTransformMatrix.Forward * 0.5f;


            // Add arrow to the scene
            if (this.Entity.Scene != null)
            {
                this.Entity.Scene.Entities.Add(arrowEntityInstance);
            }
            else
            {
                Log.Error($"{Entity?.Name ?? "Bow"}: Cannot add arrow to scene, bow's parent scene is null.");
                IsDrawing = false; // Prevent further issues
                CurrentDrawStrength = 0f;
                return;
            }
            
            ReceiveDamage(0.2f); // Apply durability damage

            // Reset drawing state and apply cooldown for nocking next arrow
            IsDrawing = false;
            CurrentDrawStrength = 0f;
            shotCooldownRemaining = 1.0f / AttackRate; // Use AttackRate for time between shots (nocking time)
            Log.Info($"{Entity?.Name ?? "Bow"} shot. Nocking cooldown: {shotCooldownRemaining}s");
        }
    }
}

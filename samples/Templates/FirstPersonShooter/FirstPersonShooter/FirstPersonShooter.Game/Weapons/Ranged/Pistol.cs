// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Core;    // For MaterialType
using FirstPersonShooter.Player;  // For PlayerInput (to get camera)
using FirstPersonShooter.World;   // For IResourceNode, SurfaceMaterial
using FirstPersonShooter.Audio;   // For SoundManager

namespace FirstPersonShooter.Weapons.Ranged
{
    public class Pistol : BaseWeapon
    {
        public int MaxAmmo { get; set; } = 12;
        public int CurrentAmmo { get; private set; }
        public float ReloadTime { get; set; } = 1.5f; // Seconds
        public float HitScanRange { get; set; } = 100f;

        private bool isReloading = false;
        private float attackCooldownRemaining = 0f;
        private float reloadCooldownRemaining = 0f;

        public Pistol()
        {
            CurrentAmmo = MaxAmmo;
            AttackRate = 5f;    // Shots per second
            Damage = 15f;       // Damage per hit
            WeaponMaterial = MaterialType.Metal;
        }

        public override void Update()
        {
            base.Update(); // Call base Update if it ever has logic.

            // Decrement attack cooldown
            if (attackCooldownRemaining > 0)
            {
                attackCooldownRemaining -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (attackCooldownRemaining < 0)
                {
                    attackCooldownRemaining = 0;
                }
            }

            // Handle reload cooldown
            if (isReloading && reloadCooldownRemaining > 0)
            {
                reloadCooldownRemaining -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (reloadCooldownRemaining <= 0)
                {
                    reloadCooldownRemaining = 0;
                    CurrentAmmo = MaxAmmo;
                    isReloading = false;
                    Log.Info($"{Entity?.Name ?? "Pistol"} reloaded. Ammo: {CurrentAmmo}/{MaxAmmo}");
                }
            }
        }

        public override void PrimaryAction()
        {
            base.PrimaryAction(); // Checks IsBroken
            if (IsBroken) return;

            if (isReloading)
            {
                // Log.Info($"{Entity?.Name ?? "Pistol"} cannot fire: currently reloading."); // Optional: for debugging
                return;
            }

            if (CurrentAmmo <= 0)
            {
                // Log.Info($"{Entity?.Name ?? "Pistol"} cannot fire: no ammo. Triggering reload."); // Optional
                this.Reload(); // Attempt to reload
                return;
            }

            if (attackCooldownRemaining > 0)
            {
                // Log.Info($"{Entity?.Name ?? "Pistol"} cannot fire: on cooldown."); // Optional
                return;
            }

            attackCooldownRemaining = 1.0f / AttackRate;
            CurrentAmmo--;

            Log.Info($"{Entity?.Name ?? "Pistol"} fired. Ammo: {CurrentAmmo}/{MaxAmmo}");
            ReceiveDamage(0.1f); // Example: Pistol takes a small amount of durability damage per shot

            // Perform raycast logic
            if (OwnerEntity == null)
            {
                Log.Warning($"{Entity?.Name ?? "Pistol"}.PrimaryAction called but OwnerEntity is null. Cannot determine attack origin.");
                return;
            }

            var simulation = this.GetSimulation();
            if (simulation == null)
            {
                Log.Error($"{Entity?.Name ?? "Pistol"}.PrimaryAction: No physics simulation found!");
                return;
            }

            Matrix viewMatrix;
            var playerInput = OwnerEntity.Get<PlayerInput>();
            if (playerInput != null && playerInput.Camera != null)
            {
                viewMatrix = playerInput.Camera.ViewMatrix;
                viewMatrix.Invert(); // World matrix of the camera
            }
            else
            {
                Log.Warning($"{Entity?.Name ?? "Pistol"} could not find PlayerInput.Camera. Falling back to OwnerEntity's orientation for attack direction.");
                viewMatrix = OwnerEntity.Transform.WorldMatrix;
            }

            var raycastStart = viewMatrix.TranslationVector;
            var forward = viewMatrix.Forward;
            var raycastEnd = raycastStart + forward * HitScanRange;

            var hitResult = simulation.Raycast(raycastStart, raycastEnd);

            if (hitResult.Succeeded)
            {
                var hitEntity = hitResult.Collider.Entity;
                // Log.Info($"{Entity?.Name ?? "Pistol"} hit [{hitEntity?.Name ?? "Unknown Entity"}] at distance {hitResult.Distance}."); // Redundant with below

                MaterialType surfaceMatType = MaterialType.Default;
                var resourceNode = hitEntity?.Get<IResourceNode>();
                if (resourceNode != null)
                {
                    surfaceMatType = resourceNode.HitMaterial;
                    // Potentially interact with resource node if pistol is meant to (e.g., very low damage)
                    // For now, just using it for sound.
                }
                else
                {
                    var surfaceMaterialComponent = hitEntity?.Get<SurfaceMaterial>();
                    if (surfaceMaterialComponent != null)
                    {
                        surfaceMatType = surfaceMaterialComponent.Type;
                    }
                }
                
                SoundManager.PlayImpactSound(hitResult.Point, this.WeaponMaterial, surfaceMatType);
                Log.Info($"{Entity?.Name ?? "Pistol"} hit {hitEntity?.Name ?? "Unknown Entity"}. Material: {surfaceMatType}");
                // Future: Apply damage to hitEntity if it has a HealthComponent or IDamageable.
                // var damageable = hitEntity.Get<IDamageable>();
                // if (damageable != null) damageable.TakeDamage(this.Damage, OwnerEntity);
            }
            // else { Log.Info($"{Entity?.Name ?? "Pistol"} fired and missed."); } // Optional
        }

        public override void Reload()
        {
            base.Reload(); // Call base method, though it does nothing by default.

            if (IsBroken)
            {
                Log.Info($"{Entity?.Name ?? "Pistol"} cannot reload: broken.");
                return;
            }

            if (isReloading)
            {
                // Log.Info($"{Entity?.Name ?? "Pistol"} already reloading."); // Optional: for debugging
                return;
            }

            if (CurrentAmmo == MaxAmmo)
            {
                // Log.Info($"{Entity?.Name ?? "Pistol"} cannot reload: ammo is full."); // Optional
                return;
            }

            isReloading = true;
            reloadCooldownRemaining = ReloadTime;
            Log.Info($"{Entity?.Name ?? "Pistol"} reloading...");
        }
    }
}

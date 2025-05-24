// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq; // For FirstOrDefault
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Core;    // For MaterialType
using FirstPersonShooter.Player;  // For PlayerInput (to get camera)
using FirstPersonShooter.Weapons.Projectiles; // For GrenadeProjectile

namespace FirstPersonShooter.Weapons.Ranged
{
    public class GrenadeWeapon : BaseWeapon
    {
        public Prefab GrenadeProjectilePrefab { get; set; }
        public float ThrowForce { get; set; } = 15f;
        public int MaxGrenades { get; set; } = 3;
        public int CurrentGrenades { get; private set; }

        private float attackCooldownRemaining = 0f;

        public GrenadeWeapon()
        {
            CurrentGrenades = MaxGrenades;
            AttackRate = 1.0f; // Time between throws
            Damage = 0f;       // Weapon itself does no direct damage
            WeaponMaterial = MaterialType.Metal;
        }

        public override void Start()
        {
            base.Start();
            if (GrenadeProjectilePrefab == null)
            {
                Log.Error($"GrenadeWeapon '{Entity?.Name ?? "Unknown"}' has no GrenadeProjectilePrefab assigned!");
            }
        }
        
        public override void Update()
        {
            base.Update(); // BaseWeapon.Update() if it ever has logic

            if (attackCooldownRemaining > 0)
            {
                attackCooldownRemaining -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (attackCooldownRemaining < 0)
                {
                    attackCooldownRemaining = 0;
                }
            }
        }

        public override void PrimaryAction()
        {
            base.PrimaryAction(); // Checks IsBroken
            if (IsBroken) return;

            if (attackCooldownRemaining > 0)
            {
                Log.Info($"{Entity?.Name ?? "GrenadeWeapon"} on cooldown.");
                return;
            }

            if (CurrentGrenades <= 0)
            {
                Log.Info($"{Entity?.Name ?? "GrenadeWeapon"}: No grenades left.");
                return;
            }

            if (GrenadeProjectilePrefab == null)
            {
                Log.Error($"{Entity?.Name ?? "GrenadeWeapon"}: GrenadeProjectilePrefab is not assigned!");
                return;
            }
            
            if (OwnerEntity == null)
            {
                Log.Error($"{Entity?.Name ?? "GrenadeWeapon"}: OwnerEntity is null. Cannot determine throw origin.");
                return;
            }

            var grenadeInstances = GrenadeProjectilePrefab.Instantiate();
            if (grenadeInstances == null || !grenadeInstances.Any())
            {
                 Log.Error($"{Entity?.Name ?? "GrenadeWeapon"}: Failed to instantiate GrenadeProjectilePrefab.");
                 return;
            }
            var grenadeEntity = grenadeInstances.First();

            // Determine spawn transform
            Matrix spawnTransformMatrix;
            Vector3 viewDirection;
            var playerInput = OwnerEntity.Get<PlayerInput>();
            if (playerInput != null && playerInput.Camera != null)
            {
                var cameraMatrix = playerInput.Camera.ViewMatrix; 
                cameraMatrix.Invert(); // World Matrix of Camera
                spawnTransformMatrix = cameraMatrix;
                viewDirection = spawnTransformMatrix.Forward;
            }
            else
            {
                Log.Warning($"{Entity?.Name ?? "GrenadeWeapon"}: Could not find PlayerInput.Camera. Using OwnerEntity's world transform.");
                spawnTransformMatrix = OwnerEntity.Transform.WorldMatrix;
                viewDirection = spawnTransformMatrix.Forward;
            }
            
            grenadeEntity.Transform.Position = spawnTransformMatrix.TranslationVector + viewDirection * 0.5f; // Spawn slightly in front
            grenadeEntity.Transform.Rotation = Quaternion.RotationMatrix(spawnTransformMatrix); // Align with view initially

            if (this.Entity.Scene != null)
            {
                this.Entity.Scene.Entities.Add(grenadeEntity);
            }
            else
            {
                Log.Error($"{Entity?.Name ?? "GrenadeWeapon"}: Cannot add grenade to scene, weapon's parent scene is null.");
                return; // Don't consume grenade or cooldown if not spawned
            }

            var grenadeRigidbody = grenadeEntity.Get<RigidbodyComponent>();
            var grenadeProjectileScript = grenadeEntity.Get<GrenadeProjectile>();

            if (grenadeRigidbody != null && grenadeProjectileScript != null)
            {
                grenadeRigidbody.ApplyImpulse(viewDirection * ThrowForce);
            }
            else
            {
                Log.Error($"{Entity?.Name ?? "GrenadeWeapon"}: GrenadeProjectilePrefab is missing RigidbodyComponent or GrenadeProjectile script.");
                grenadeEntity.Scene = null; // Remove the misconfigured grenade
                return; // Don't consume grenade or cooldown
            }
            
            CurrentGrenades--;
            attackCooldownRemaining = 1.0f / AttackRate;
            ReceiveDamage(0.1f); // Durability damage to launcher/self

            Log.Info($"Threw grenade. {CurrentGrenades} left. Cooldown: {attackCooldownRemaining}s");
        }

        public override void Reload()
        {
            base.Reload(); // Base check for IsBroken
            if (IsBroken) return;

            CurrentGrenades = MaxGrenades;
            Log.Info($"{Entity?.Name ?? "GrenadeWeapon"}: Grenades restocked to {CurrentGrenades}.");
        }
    }
}

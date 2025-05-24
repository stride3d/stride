// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Weapons.Projectiles; // For BasicTurretProjectile
using FirstPersonShooter.Core; // For ITargetable

namespace FirstPersonShooter.Building.Defenses.Strategies
{
    public class ProjectileFireStrategy : ScriptComponent, ITurretFireStrategy
    {
        public Prefab ProjectilePrefab { get; set; }

        private float _fireRate = 2f;
        public float FireRate 
        { 
            get => _fireRate; 
            set => _fireRate = value; 
        }

        private float fireCooldownRemaining = 0f;

        public override void Start()
        {
            if (ProjectilePrefab == null)
            {
                Log.Error($"ProjectileFireStrategy on {Entity.Name}: ProjectilePrefab is not assigned.");
            }
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (fireCooldownRemaining > 0f)
            {
                fireCooldownRemaining -= deltaTime;
                if (fireCooldownRemaining < 0f)
                {
                    fireCooldownRemaining = 0f;
                }
            }
        }

        public bool IsReadyToFire()
        {
            return fireCooldownRemaining <= 0f;
        }

        public bool Fire(Entity ownerTurret, Entity targetEntity, Vector3 muzzlePosition, Quaternion muzzleRotation, float gameDeltaTime)
        {
            if (!IsReadyToFire())
            {
                return false;
            }

            if (ProjectilePrefab == null)
            {
                Log.Error($"ProjectileFireStrategy on {ownerTurret?.Name ?? "Unknown Turret"}: ProjectilePrefab not set!");
                return false; // Cannot fire
            }

            var targetPos = targetEntity.Get<ITargetable>()?.GetTargetPosition() ?? targetEntity.Transform.Position;

            var projectileEntities = ProjectilePrefab.Instantiate();
            if (projectileEntities == null || projectileEntities.Count == 0)
            {
                Log.Error($"ProjectileFireStrategy on {ownerTurret?.Name ?? "Unknown Turret"}: Failed to instantiate ProjectilePrefab or prefab is empty.");
                return false;
            }
            var projectileInstance = projectileEntities[0]; // Assuming the prefab root is the projectile

            if (projectileInstance != null)
            {
                projectileInstance.Transform.Position = muzzlePosition;
                
                // Aiming logic (align projectile's forward with direction to target)
                var directionToTarget = targetPos - muzzlePosition;
                if (directionToTarget.LengthSquared() < float.Epsilon)
                {
                    // If target is at muzzle, use muzzle's current forward direction
                    projectileInstance.Transform.Rotation = muzzleRotation;
                    Log.Warning($"ProjectileFireStrategy on {ownerTurret?.Name ?? "Unknown Turret"}: Target is at the same position as muzzle. Using muzzle's current rotation.");
                }
                else
                {
                    directionToTarget.Normalize();
                    projectileInstance.Transform.Rotation = Quaternion.LookRotation(directionToTarget, Vector3.UnitY);
                }
                
                // Add to scene:
                // Ensure ownerTurret is valid and has a scene context
                if (ownerTurret?.Scene != null)
                {
                    ownerTurret.Scene.Entities.Add(projectileInstance);
                }
                else if (this.Entity?.Scene != null) // Fallback to this strategy's entity scene context
                {
                     Log.Warning($"ProjectileFireStrategy on {Entity.Name}: ownerTurret or its scene is null. Using strategy's entity scene.");
                     this.Entity.Scene.Entities.Add(projectileInstance);
                }
                else
                {
                    Log.Error($"ProjectileFireStrategy on {Entity.Name}: Cannot add projectile to scene, ownerTurret and self have no scene context.");
                    return false; // Failed to add to scene
                }
                
                var projScript = projectileInstance.Get<BasicTurretProjectile>();
                if (projScript != null) 
                { 
                    Log.Info($"ProjectileFireStrategy on {ownerTurret?.Name ?? "Unknown Turret"}: Fired projectile '{projectileInstance.Name}' with Speed {projScript.Speed}.");
                }
                else
                {
                    Log.Warning($"ProjectileFireStrategy on {ownerTurret?.Name ?? "Unknown Turret"}: Projectile '{projectileInstance.Name}' is missing BasicTurretProjectile script.");
                }

                fireCooldownRemaining = 1.0f / FireRate;
                return true;
            }
            else
            {
                Log.Error($"ProjectileFireStrategy on {ownerTurret?.Name ?? "Unknown Turret"}: First entity in instantiated prefab is null.");
                return false;
            }
        }
    }
}

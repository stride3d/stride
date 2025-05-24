// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Core; // For ITargetable

namespace FirstPersonShooter.Building.Defenses
{
    public class TurretWeaponSystem : SyncScript
    {
        public float FireRate { get; set; } = 2f; // Shots per second
        public Entity MuzzlePointEntity { get; set; } // Assign in editor: child entity representing muzzle
        // public Prefab ProjectilePrefab { get; set; } // For future projectile firing

        private float fireCooldownRemaining = 0f;

        public override void Start()
        {
            if (MuzzlePointEntity == null)
            {
                Log.Warning($"TurretWeaponSystem on {Entity.Name}: MuzzlePointEntity is not assigned. Using this entity's transform as fallback.");
            }
        }

        public override void Update()
        {
            if (fireCooldownRemaining > 0f)
            {
                fireCooldownRemaining -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (fireCooldownRemaining < 0f)
                {
                    fireCooldownRemaining = 0f;
                }
            }
        }

        /// <summary>
        /// Attempts to fire at the target entity.
        /// </summary>
        /// <param name="targetEntity">The entity to fire at.</param>
        /// <returns>True if a shot was fired, false otherwise.</returns>
        public bool FireAt(Entity targetEntity)
        {
            if (fireCooldownRemaining > 0f)
            {
                // Log.Info($"TurretWeaponSystem on {Entity.Name}: On cooldown."); // Can be verbose
                return false;
            }

            if (targetEntity == null)
            {
                Log.Warning($"TurretWeaponSystem on {Entity.Name}: FireAt called with null targetEntity.");
                return false;
            }

            Vector3 targetPos = targetEntity.Transform.Position; // Default if not ITargetable
            var targetable = targetEntity.Get<ITargetable>();
            if (targetable != null)
            {
                targetPos = targetable.GetTargetPosition();
            }

            Vector3 spawnPos = MuzzlePointEntity?.Transform.WorldMatrix.TranslationVector ?? Entity.Transform.WorldMatrix.TranslationVector;

            Log.Info($"TurretWeaponSystem on {Entity.Name}: Firing at {targetEntity.Name} (target pos {targetPos}) from weapon system (muzzle pos {spawnPos}).");
            
            // Future: Instantiate ProjectilePrefab, aim it from spawnPos to targetPos.
            // Example:
            // if (ProjectilePrefab != null)
            // {
            //     var projectileInstance = ProjectilePrefab.Instantiate().FirstOrDefault();
            //     if (projectileInstance != null)
            //     {
            //         projectileInstance.Transform.Position = spawnPos;
            //         // Aiming logic:
            //         var direction = Vector3.Normalize(targetPos - spawnPos);
            //         projectileInstance.Transform.Rotation = Quaternion.LookRotation(direction, Vector3.UnitY); 
            //         // Add to scene:
            //         Entity.Scene.Entities.Add(projectileInstance);
            //         // Configure projectile (speed, damage etc.)
            //         // var projScript = projectileInstance.Get<YourProjectileScript>();
            //         // if (projScript != null) { projScript.Velocity = direction * ProjSpeed; }
            //     }
            // }
            // else { Log.Error($"TurretWeaponSystem on {Entity.Name}: ProjectilePrefab not set!"); }


            fireCooldownRemaining = 1.0f / FireRate;
            return true;
        }
    }
}

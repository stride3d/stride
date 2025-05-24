// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Core; // For ITargetable
using FirstPersonShooter.Building.Defenses.Strategies; // For ITurretFireStrategy

namespace FirstPersonShooter.Building.Defenses
{
    public class TurretWeaponSystem : SyncScript
    {
        public Entity MuzzlePointEntity { get; set; } // Assign in editor: child entity representing muzzle
        public ITurretFireStrategy FireStrategy { get; private set; }

        public override void Start()
        {
            if (MuzzlePointEntity == null)
            {
                Log.Warning($"TurretWeaponSystem on {Entity.Name}: MuzzlePointEntity is not assigned. Using this entity's transform as fallback.");
            }

            // Attempt to get the FireStrategy component
            FireStrategy = Entity.Get<ITurretFireStrategy>() ?? Entity.GetComponentInChildren<ITurretFireStrategy>();
            if (FireStrategy == null)
            {
                Log.Error($"TurretWeaponSystem on {Entity.Name}: No ITurretFireStrategy component found on this entity or its children.");
            }
        }

        public override void Update()
        {
            FireStrategy?.UpdateCooldown((float)Game.UpdateTime.Elapsed.TotalSeconds);
        }

        /// <summary>
        /// Attempts to fire at the target entity using the assigned FireStrategy.
        /// </summary>
        /// <param name="targetEntity">The entity to fire at.</param>
        /// <returns>True if a shot was attempted/fired, false otherwise.</returns>
        public bool FireAt(Entity targetEntity)
        {
            if (FireStrategy == null)
            {
                Log.Error($"TurretWeaponSystem on {Entity.Name}: FireStrategy is not assigned or found.");
                return false;
            }

            if (targetEntity == null)
            {
                Log.Warning($"TurretWeaponSystem on {Entity.Name}: FireAt called with null targetEntity.");
                return false;
            }

            // Determine muzzle position and rotation
            Vector3 muzzlePosition = MuzzlePointEntity?.Transform.WorldMatrix.TranslationVector ?? Entity.Transform.WorldMatrix.TranslationVector;
            Quaternion muzzleRotation = MuzzlePointEntity?.Transform.WorldMatrix.RotationQuaternion ?? Entity.Transform.WorldMatrix.RotationQuaternion;
            
            // Log.Info($"TurretWeaponSystem on {Entity.Name}: Attempting to fire at {targetEntity.Name} via strategy.");

            return FireStrategy.Fire(this.Entity, targetEntity, muzzlePosition, muzzleRotation, (float)Game.UpdateTime.Elapsed.TotalSeconds);
        }
    }
}

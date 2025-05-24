// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;

namespace FirstPersonShooter.Building.Defenses.Strategies
{
    public interface ITurretFireStrategy
    {
        /// <summary>
        /// Gets or sets the rate of fire in shots per second.
        /// </summary>
        float FireRate { get; set; }

        /// <summary>
        /// Updates the cooldown timer for firing.
        /// </summary>
        /// <param name="deltaTime">The elapsed time since the last update, in seconds.</param>
        void UpdateCooldown(float deltaTime);

        /// <summary>
        /// Checks if the weapon is ready to fire.
        /// </summary>
        /// <returns>True if ready to fire, false otherwise.</returns>
        bool IsReadyToFire();

        /// <summary>
        /// Attempts to fire the weapon.
        /// </summary>
        /// <param name="ownerTurret">The main turret entity (which owns the TurretWeaponSystem).</param>
        /// <param name="targetEntity">The entity to fire upon.</param>
        /// <param name="muzzlePosition">World space position of the muzzle.</param>
        /// <param name="muzzleRotation">World space rotation of the muzzle.</param>
        /// <param name="gameDeltaTime">Game delta time, can be used by strategy if it needs its own time updates beyond FireRate.</param>
        /// <returns>True if a shot was attempted/fired, false otherwise (e.g., on cooldown or unable to fire).</returns>
        bool Fire(Entity ownerTurret, Entity targetEntity, Vector3 muzzlePosition, Quaternion muzzleRotation, float gameDeltaTime);
    }
}

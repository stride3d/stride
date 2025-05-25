using MySurvivalGame.Game.Weapons; // For BaseWeapon
using Stride.Engine;
using Stride.Core.Mathematics; // For Vector3 if doing raycast here

namespace MySurvivalGame.Game.Weapons.Ranged
{
    public abstract class BaseRangedWeapon : BaseWeapon
    {
        public virtual int ClipSize { get; protected set; } = 10;
        public virtual int CurrentAmmoInClip { get; protected set; } = 10;
        public virtual int ReserveAmmo { get; protected set; } = 50;

        // Common ranged methods like FireBullet(), ApplyRecoil() could go here
        protected virtual void ShootRaycast(Vector3 start, Vector3 direction, float range)
        {
            var simulation = this.GetSimulation();
            if (simulation == null) return;

            var hitResult = simulation.Raycast(start, start + direction * range);
            if (hitResult.Succeeded)
            {
                Log.Info($"{this.Entity?.Name ?? "RangedWeapon"}: Fired. Hit: {hitResult.Collider.Entity.Name} at {hitResult.Point}");
                // Future: Check if hitEntity has IDamageable, apply damage.
            }
            else
            {
                Log.Info($"{this.Entity?.Name ?? "RangedWeapon"}: Fired. Missed.");
            }
        }
    }
}

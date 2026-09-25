// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.BepuPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Rendering.Sprites;

namespace FirstPersonShooter.Player
{
    public struct WeaponFiredResult
    {
        public bool         DidFire;
        public bool         DidHit;
        public HitInfo    HitResult;
    }

    public class WeaponScript : SyncScript
    {
        public static readonly EventKey<WeaponFiredResult> WeaponFired = new EventKey<WeaponFiredResult>();

        public static readonly EventKey<bool> IsReloading = new EventKey<bool>();

        private readonly EventReceiver<bool> shootEvent = new EventReceiver<bool>(PlayerInput.ShootEventKey);

        private readonly EventReceiver<bool> reloadEvent = new EventReceiver<bool>(PlayerInput.ReloadEventKey);

        public float MaxShootDistance { get; set; } = 100f;

        public float ShootImpulse { get; set; } = 5f;

        public float Cooldown { get; set; } = 0.3f;
        private float cooldownRemaining = 0;

        public float ReloadCooldown { get; set; } = 2.0f;

        public SpriteComponent RemainingBullets { get; set; }
        private int remainingBullets = 0;

        private void UpdateBulletsLED()
        {
            var spriteSheet = RemainingBullets?.SpriteProvider as SpriteFromSheet;
            if (spriteSheet != null)
                spriteSheet.CurrentFrame = remainingBullets;
        }

        private void ReloadWeapon()
        {
            IsReloading.Broadcast(true);
            Func<Task> reloadTask = async () =>
            {
                // Countdown
                var secondsCountdown = cooldownRemaining = ReloadCooldown;
                while (secondsCountdown > 0f)
                {
                    await Script.NextFrame();
                    secondsCountdown -= (float) Game.UpdateTime.WarpElapsed.TotalSeconds;
                }

                remainingBullets = 9;
                UpdateBulletsLED();
            };

            Script.AddTask(reloadTask);
        }

        /// <summary>
        /// Called on every frame update
        /// </summary>
        public override void Update()
        {
            bool didShoot;
            shootEvent.TryReceive(out didShoot);

            bool didReload;
            reloadEvent.TryReceive(out didReload);

            cooldownRemaining = (cooldownRemaining > 0) ? (cooldownRemaining - (float)this.Game.UpdateTime.WarpElapsed.TotalSeconds) : 0f;
            if (cooldownRemaining > 0)
                return; // Can't shoot yet

            if ((remainingBullets == 0 && didShoot) || (remainingBullets < 9 && didReload))
            {
                ReloadWeapon();
                return;
            }

            if (!didShoot)
                return;

            remainingBullets--;
            UpdateBulletsLED();

            cooldownRemaining = Cooldown;

            var raycastStart = Entity.Transform.WorldMatrix.TranslationVector;
            var forward = Entity.Transform.WorldMatrix.Forward;

            // The player is on layer 1.
            var didHit = Entity.GetSimulation().RayCast(raycastStart, forward, MaxShootDistance, out var result, ~CollisionMask.Layer1);

            var weaponFired = new WeaponFiredResult {HitResult = result, DidFire = true, DidHit = didHit && result.Collidable is not null };

            if (didHit && result.Collidable is BodyComponent rigidBody)
            {
                // Calculate the lever arm from the center of mass to the hit point
                Vector3 centerOfMass = rigidBody.Entity.Transform.LocalToWorld(rigidBody.CenterOfMass);
                Vector3 hitPoint = result.Point;
                Vector3 leverArm = hitPoint - centerOfMass;

                rigidBody.Awake = true;
                rigidBody.ApplyImpulse(forward * ShootImpulse, leverArm);
            }

            // Broadcast the fire event
            WeaponFired.Broadcast( weaponFired );
        }
    }
}

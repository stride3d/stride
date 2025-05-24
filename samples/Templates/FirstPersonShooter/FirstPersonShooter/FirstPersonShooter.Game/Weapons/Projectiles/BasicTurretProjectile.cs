// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
// using FirstPersonShooter.Core; // For IDamageable, if it exists and is used later

namespace FirstPersonShooter.Weapons.Projectiles
{
    public class BasicTurretProjectile : ScriptComponent
    {
        public float Speed { get; set; } = 50f;
        public float MaxLifetime { get; set; } = 3f; // Seconds
        public float Damage { get; set; } = 10f; // For future use

        private float _currentLifetime;
        private StaticColliderComponent _collider;

        public override void Start()
        {
            _currentLifetime = MaxLifetime;
            _collider = Entity.Get<StaticColliderComponent>();

            if (_collider == null)
            {
                Log.Error($"BasicTurretProjectile on '{Entity.Name}' requires a StaticColliderComponent to function.");
                // Optionally, disable this script or the entity if the collider is missing
                // this.Enabled = false; 
                return;
            }

            // Subscribe to collision events
            _collider.Collisions.CollectionChanged += Collisions_CollectionChanged;
        }

        public override void Update()
        {
            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            // Move the entity forward
            Entity.Transform.Position += Entity.Transform.WorldMatrix.Forward * Speed * deltaTime;

            // Decrement lifetime
            _currentLifetime -= deltaTime;
            if (_currentLifetime <= 0)
            {
                Log.Info($"Projectile '{Entity.Name}' lifetime expired. Removing from scene.");
                Entity.Scene = null; // Remove entity from scene
                return; // Important to return after removing from scene
            }
        }

        private void Collisions_CollectionChanged(object sender, Stride.Core.Collections.TrackingCollectionChangedEventArgs e)
        {
            var collision = (Collision)e.Item;

            // We are interested in new collisions
            if (e.Action == Stride.Core.Collections.NotifyCollectionChangedAction.Add)
            {
                var otherEntity = _collider == collision.ColliderA ? collision.ColliderB.Entity : collision.ColliderA.Entity;

                // Avoid self-collision or collision with other projectiles (assuming they are on a specific layer or have a tag)
                // For now, just log and destroy. A more robust solution would check tags/groups.
                // We also need to ensure it doesn't collide with the turret that fired it immediately.
                // This might involve ignoring collisions for the first few frames, or using collision groups.

                Log.Info($"Projectile '{Entity.Name}' collided with '{otherEntity.Name}'.");

                // Placeholder for IDamageable logic:
                // var damageable = otherEntity.Get<IDamageable>();
                // if (damageable != null)
                // {
                //     damageable.TakeDamage(Damage);
                //     Log.Info($"Dealt {Damage} to {otherEntity.Name}.");
                // }

                // Remove projectile after impact
                if (Entity.Scene != null) // Check if not already removed (e.g. by lifetime)
                {
                    Entity.Scene = null;
                }
            }
        }

        public override void Cancel() // Called when the script is removed or entity is removed
        {
            // Unsubscribe from collision events to prevent issues if the component is reused or if event handlers hold references
            if (_collider != null)
            {
                _collider.Collisions.CollectionChanged -= Collisions_CollectionChanged;
            }
            base.Cancel();
        }
    }
}

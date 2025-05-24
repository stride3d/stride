// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic; // For List<HitResult>
using System.Linq; // For FirstOrDefault
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Audio; // For SoundManager

namespace FirstPersonShooter.Weapons.Projectiles
{
    public class GrenadeProjectile : ScriptComponent
    {
        public float FuseTime { get; set; } = 3.0f; // Seconds
        public float ExplosionRadius { get; set; } = 5.0f;
        public float ExplosionDamage { get; set; } = 75f; // Max damage at center
        public float ExplosionImpulse { get; set; } = 50f; // Max impulse at center
        public Prefab PlaceholderExplosionEffectPrefab { get; set; } // Optional: A simple sphere that scales up

        private float fuseTimer = 0f;
        private bool exploded = false;
        private RigidbodyComponent rigidbody;
        private List<HitResult> hitResultsList = new List<HitResult>(); // For OverlapSphere

        public override void Start()
        {
            rigidbody = Entity.Get<RigidbodyComponent>();
            if (rigidbody == null)
            {
                Log.Error($"GrenadeProjectile on Entity '{Entity.Name}' requires a RigidbodyComponent.");
                DestroyGrenade(); 
                return;
            }
            // Initial velocity will be set by the GrenadeWeapon
            Log.Info($"GrenadeProjectile '{Entity.Name}' armed. Fuse: {FuseTime}s");
        }

        public override void Update()
        {
            if (exploded) return;

            fuseTimer += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (fuseTimer >= FuseTime)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (exploded) return; // Ensure explosion happens only once
            exploded = true;

            var explosionPosition = Entity.Transform.Position;
            Log.Info($"Grenade exploding at {explosionPosition}!");

            // Instantiate placeholder explosion effect
            if (PlaceholderExplosionEffectPrefab != null)
            {
                var effectEntity = PlaceholderExplosionEffectPrefab.Instantiate().FirstOrDefault();
                if (effectEntity != null)
                {
                    effectEntity.Transform.Position = explosionPosition;
                    if (this.Entity.Scene != null)
                    {
                        this.Entity.Scene.Entities.Add(effectEntity);
                        // The effect prefab should have its own script to despawn.
                        Log.Info($"Spawned explosion effect from '{PlaceholderExplosionEffectPrefab.Name}'.");
                    }
                    else
                    {
                        Log.Warning("GrenadeProjectile: Cannot spawn explosion effect, grenade's parent scene is null.");
                    }
                }
                else
                {
                    Log.Warning("GrenadeProjectile: Failed to instantiate PlaceholderExplosionEffectPrefab.");
                }
            }

            // Play explosion sound
            SoundManager.PlayExplosionSound(explosionPosition);

            // Physics Impulse & Damage
            var simulation = this.GetSimulation();
            if (simulation != null)
            {
                hitResultsList.Clear();
                simulation.OverlapSphere(explosionPosition, ExplosionRadius, hitResultsList, CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags.DefaultFilter);

                var uniqueHitEntities = new HashSet<Entity>();

                foreach (var hitResult in hitResultsList)
                {
                    var hitEntity = hitResult.Collider?.Entity;
                    if (hitEntity == null || hitEntity == this.Entity || !uniqueHitEntities.Add(hitEntity))
                    {
                        continue; // Skip self, nulls, or already processed entities
                    }

                    float distance = Vector3.Distance(explosionPosition, hitResult.Collider.WorldMatrix.TranslationVector); // Use collider's position for accuracy
                    float falloff = MathUtil.Clamp(1 - (distance / ExplosionRadius), 0f, 1f); // Linear falloff

                    // Apply Damage (Conceptual)
                    float damage = ExplosionDamage * falloff;
                    Log.Info($"Entity {hitEntity.Name} (distance: {distance:F2}m, falloff: {falloff:F2}) would take {damage:F2} damage.");
                    // Future: hitEntity.Get<IDamageable>()?.TakeDamage(damage, null /* Attacker info */);

                    // Apply Impulse
                    var hitRigidbody = hitEntity.Get<RigidbodyComponent>();
                    if (hitRigidbody != null && hitRigidbody.RigidBodyType != RigidBodyTypes.Kinematic && hitRigidbody.Enabled)
                    {
                        Vector3 direction = Vector3.Normalize(hitResult.Collider.WorldMatrix.TranslationVector - explosionPosition);
                        if (direction.LengthSquared() < 0.001f) // If target is at epicenter, push upwards
                        {
                            direction = Vector3.UnitY;
                        }
                        float impulseMagnitude = ExplosionImpulse * falloff;
                        hitRigidbody.ApplyImpulse(direction * impulseMagnitude);
                        Log.Info($"Applied impulse ({impulseMagnitude:F2}) to {hitEntity.Name}.");
                    }
                }
            }
            else
            {
                Log.Warning("GrenadeProjectile: Physics simulation not found for explosion overlap test.");
            }

            DestroyGrenade();
        }

        private void DestroyGrenade()
        {
            // Unsubscribe or clean up if necessary (e.g. rigidbody.CollisionStarted -= OnCollision if it had one)
            if (this.Entity.Scene != null)
            {
                this.Entity.Scene = null; // Removes entity from scene
                Log.Info($"GrenadeProjectile '{Entity.Name}' despawned.");
            }
        }
    }
}

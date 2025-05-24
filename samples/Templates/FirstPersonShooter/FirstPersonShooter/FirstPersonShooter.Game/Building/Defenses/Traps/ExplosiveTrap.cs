// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Physics;
using Stride.Audio;
using Stride.Particles;
using Stride.Core.Mathematics;
using System.Collections.Generic;
using FirstPersonShooter.Core; // For IDamageable
using FirstPersonShooter.Building.Pieces; // For BaseBuildingPiece
// Potentially add using for PlayerMarkerComponent or CreatureMarkerComponent if specific filtering is desired beyond IDamageable

namespace FirstPersonShooter.Building.Defenses.Traps
{
    public class ExplosiveTrap : BaseBuildingPiece
    {
        // --- BaseBuildingPiece Properties ---
        private float health = 20f;
        private MaterialType structureMaterialType = MaterialType.Metal;
        public override float Health { get => health; set => health = value; }
        public override MaterialType StructureMaterialType { get => structureMaterialType; set => structureMaterialType = value; }

        // --- ExplosiveTrap Specific Properties ---
        public float ExplosionDamage { get; set; } = 100f;
        public float ExplosionRadius { get; set; } = 5f;
        public float TriggerProximityRadius { get; set; } = 1.5f; // For proximity trigger
        public float ArmingTime { get; set; } = 2.0f; // Seconds after placement
        public SoundEffectInstance ArmingSound { get; set; } // Sound played when armed
        public SoundEffectInstance ExplosionSound { get; set; } // Sound for explosion
        public ParticleSystemComponent ExplosionParticlePrefab { get; set; } // Particle effect for explosion

        // --- Private Fields ---
        private bool isArmed = false;
        private float currentArmingTime = 0f;
        private bool exploded = false;
        private List<HitResult> proximityHitResults = new List<HitResult>(); // Re-use list for proximity checks
        private List<HitResult> explosionHitResults = new List<HitResult>(); // Re-use list for explosion damage

        public ExplosiveTrap()
        {
            this.IsGroundPiece = true;
        }

        public override void InitializeSnapPoints()
        {
            SnapPoints.Clear();
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = Vector3.Zero,
                LocalRotation = Quaternion.Identity,
                Type = "FloorTrapBase" 
            });
        }

        public override void Start()
        {
            base.Start(); 

            currentArmingTime = ArmingTime;
            isArmed = false;
            exploded = false;
        }

        public override void Update()
        {
            base.Update(); 

            if (exploded)
            {
                return;
            }

            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            // Arming Logic
            if (!isArmed)
            {
                if (currentArmingTime > 0f)
                {
                    currentArmingTime -= deltaTime;
                    if (currentArmingTime <= 0f)
                    {
                        isArmed = true;
                        ArmingSound?.Play();
                        Log.Info($"ExplosiveTrap '{Entity?.Name ?? "Unnamed"}' is now armed.");
                    }
                }
                return; // If not armed yet (either still counting down or just became armed), don't check for proximity yet
            }

            // Proximity Trigger Logic (if armed)
            var simulation = this.GetSimulation();
            if (simulation == null)
            {
                Log.Warning("ExplosiveTrap: Physics simulation not found.");
                return;
            }

            proximityHitResults.Clear();
            simulation.OverlapSphere(this.Entity.Transform.Position, TriggerProximityRadius, proximityHitResults, CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags.CharacterFilter | CollisionFilterGroupFlags.VehicleFilter); // Example filter: Characters and Vehicles

            foreach (var hit in proximityHitResults)
            {
                var hitEntity = hit.Collider?.Entity;
                if (hitEntity == null || hitEntity == this.Entity) // Skip self or null entities
                    continue;

                // Check if the entity is damageable (basic filter)
                // Could be expanded to check for PlayerMarkerComponent/CreatureMarkerComponent if needed
                if (hitEntity.Get<IDamageable>() != null)
                {
                    Log.Info($"ExplosiveTrap '{Entity?.Name ?? "Unnamed"}' triggered by proximity to '{hitEntity.Name}'. Exploding.");
                    Explode();
                    break; // Only need one trigger
                }
            }
        }

        public void Explode()
        {
            if (exploded)
            {
                return;
            }
            exploded = true;

            Log.Info($"ExplosiveTrap '{Entity?.Name ?? "Unnamed"}' Exploded!");

            ExplosionSound?.Play();

            if (ExplosionParticlePrefab != null)
            {
                var particleEntity = ExplosionParticlePrefab.Entity.Clone(); // Clone the entity holding the particle system
                if (particleEntity != null)
                {
                    particleEntity.Transform.Position = this.Entity.Transform.Position;
                    Entity.Scene.Entities.Add(particleEntity);
                    // Ensure particle system plays. Most particle systems are set to play on enable/spawn.
                    // If manual start is needed: particleEntity.Get<ParticleSystemComponent>()?.ParticleSystem.Play();
                }
                else
                {
                    Log.Warning($"ExplosiveTrap '{Entity?.Name ?? "Unnamed"}': Failed to clone ExplosionParticlePrefab's entity.");
                }
            }

            var simulation = this.GetSimulation();
            if (simulation != null)
            {
                explosionHitResults.Clear();
                simulation.OverlapSphere(this.Entity.Transform.Position, ExplosionRadius, explosionHitResults, CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags.All); // Hit all groups for damage

                foreach (var hit in explosionHitResults)
                {
                    var hitEntity = hit.Collider?.Entity;
                    if (hitEntity == null || hitEntity == this.Entity)
                        continue;

                    var damageable = hitEntity.Get<IDamageable>();
                    if (damageable != null)
                    {
                        Log.Info($"ExplosiveTrap '{Entity?.Name ?? "Unnamed"}' dealing {ExplosionDamage} damage to '{hitEntity.Name}'.");
                        damageable.TakeDamage(ExplosionDamage, this.Entity);
                    }
                }
            }
            else
            {
                Log.Warning("ExplosiveTrap: Physics simulation not found for explosion damage dealing.");
            }
            
            // This will handle the removal of the trap entity and trigger BaseBuildingPiece destruction effects (if any remain relevant)
            this.Debug_ForceDestroy();
        }

        public override void OnPieceDestroyed()
        {
            // This method is called when Health <= 0 or when Debug_ForceDestroy is called.
            // We want to ensure that if the trap is destroyed by external damage, it still explodes.
            // If Explode() was already called (e.g. by proximity), `exploded` will be true.
            if (!exploded)
            {
                Log.Info($"ExplosiveTrap '{Entity?.Name ?? "Unnamed"}' was destroyed by damage, triggering explosion.");
                Explode(); // This will set exploded = true and then call Debug_ForceDestroy().
                           // Debug_ForceDestroy() will in turn call OnPieceDestroyed() again, but `exploded` will be true,
                           // so it won't loop infinitely.
            }
            
            // The original BaseBuildingPiece.OnPieceDestroyed() handles structural integrity updates and visual/sound effects.
            // Since Explode() calls Debug_ForceDestroy(), which itself calls OnPieceDestroyed(),
            // we must be careful. The `exploded` flag prevents re-explosion.
            // If BaseBuildingPiece.OnPieceDestroyed has effects we want *after* our explosion logic (which is unlikely
            // as our explosion is the primary "destruction" effect), we might call it.
            // However, `Debug_ForceDestroy` in `BaseBuildingPiece` already calls `OnPieceDestroyed`.
            // So, if `Explode` calls `Debug_ForceDestroy`, `BaseBuildingPiece.OnPieceDestroyed` will be invoked.
            // We don't need to call `base.OnPieceDestroyed()` here again.
            // The check `if (!exploded)` ensures `Explode()` is only called once.
        }
    }
}

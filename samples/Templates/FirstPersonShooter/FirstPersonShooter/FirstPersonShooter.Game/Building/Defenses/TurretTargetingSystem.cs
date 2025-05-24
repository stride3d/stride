// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Core; // For ITargetable

namespace FirstPersonShooter.Building.Defenses
{
    public class TurretTargetingSystem : SyncScript
    {
        public float TargetingRange { get; set; } = 15f;
        public float ScanInterval { get; set; } = 0.5f; // Seconds
        public Entity CurrentTarget { get; private set; }

        private float scanTimer = 0f;
        private List<HitResult> hitResults = new List<HitResult>(); // Re-use list to avoid allocations

        public override void Start()
        {
            scanTimer = ScanInterval; // Initial scan delay or immediate scan
        }

        public override void Update()
        {
            scanTimer -= (float)Game.UpdateTime.Elapsed.TotalSeconds;

            if (scanTimer <= 0f)
            {
                scanTimer = ScanInterval;
                ScanForTargets();
            }
        }

        private void ScanForTargets()
        {
            CurrentTarget = null; // Reset before scan
            var simulation = this.GetSimulation();

            if (simulation == null)
            {
                Log.Warning("TurretTargetingSystem: Physics simulation not found.");
                return;
            }

            hitResults.Clear(); // Clear previous results
            // Using DefaultFilter for broad phase, CharacterFilter for specific interaction.
            // Actual group setup is done in Stride GameSettings > Physics.
            simulation.OverlapSphere(Entity.Transform.Position, TargetingRange, hitResults, CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags.CharacterFilter);

            float closestDistanceSquared = TargetingRange * TargetingRange; // Use squared distance for comparison efficiency

            foreach (var hit in hitResults)
            {
                var hitEntity = hit.Collider?.Entity;
                if (hitEntity == null || hitEntity == this.Entity) // Skip self or null entities
                    continue;

                var targetable = hitEntity.Get<ITargetable>();
                if (targetable != null)
                {
                    // Additional checks: Line of sight (raycast), team affiliation (if applicable)
                    // For now, just take the closest ITargetable
                    float distanceSq = Vector3.DistanceSquared(Entity.Transform.Position, hitEntity.Transform.Position);
                    if (distanceSq < closestDistanceSquared)
                    {
                        // Basic Line of Sight Check (optional, but good for realism)
                        var losHitResult = simulation.Raycast(Entity.Transform.Position, targetable.GetTargetPosition(), CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags.DefaultFilter & ~CollisionFilterGroupFlags.CharacterFilter); // Hit anything *but* characters
                        if (!losHitResult.Succeeded || losHitResult.Collider.Entity == hitEntity) // If nothing hit, or hit the target itself
                        {
                            closestDistanceSquared = distanceSq;
                            CurrentTarget = hitEntity;
                        }
                        else
                        {
                             // Log.Info($"TurretTargetingSystem: Target {hitEntity.Name} found by sphere but LOS blocked by {losHitResult.Collider.Entity.Name}");
                        }
                    }
                }
            }

            if (CurrentTarget != null)
            {
                // Log.Info($"TurretTargetingSystem on {Entity.Name}: Acquired target {CurrentTarget.Name}."); // Can be verbose
            }
            else
            {
                // Log.Info($"TurretTargetingSystem on {Entity.Name}: No targets in range or line of sight."); // Can be verbose
            }
        }
    }
}

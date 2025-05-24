// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Physics;
using Stride.Audio;
using Stride.Core.Mathematics;
using System.Collections.Generic;
using FirstPersonShooter.Core; // For IDamageable
using FirstPersonShooter.Building.Pieces; // For BaseBuildingPiece

namespace FirstPersonShooter.Building.Defenses.Traps
{
    public class SpikeTrap : BaseBuildingPiece
    {
        // --- BaseBuildingPiece Properties ---
        private float health = 100f;
        private MaterialType structureMaterialType = MaterialType.Metal; // Or Wood, depending on trap type
        public override float Health { get => health; set => health = value; }
        public override MaterialType StructureMaterialType { get => structureMaterialType; set => structureMaterialType = value; }

        // --- SpikeTrap Specific Properties ---
        public float DamagePerTrigger { get; set; } = 25f;
        public float TriggerCooldown { get; set; } = 1.0f; // Cooldown before this trap can damage *any* entity again.
        public float RearmTimePerEntity { get; set; } = 2.0f; // Cooldown before the *same* entity can be damaged again by this trap.
        public bool IsSingleUse { get; set; } = false;
        public SoundEffectInstance TrapTriggerSound { get; set; } // Sound to play on trigger

        // --- Private Fields ---
        private float currentCooldown = 0f;
        private Dictionary<Entity, float> entityRearmTimers = new Dictionary<Entity, float>();
        private StaticColliderComponent triggerCollider;

        public SpikeTrap()
        {
            this.IsGroundPiece = true; // Typically placed on the ground
        }

        public override void InitializeSnapPoints()
        {
            SnapPoints.Clear();
            // A simple floor trap might snap to a "FloorCenter" or similar.
            // For now, let's assume it needs a flat base connection.
            // If it's freely placed, SnapPoints could remain empty or have a type that doesn't auto-connect.
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = Vector3.Zero, // Snaps from its own origin (base center)
                LocalRotation = Quaternion.Identity,
                Type = "FloorTrapBase" // Compatible with "FoundationTopCenter", "CeilingBottomCenter" (if applicable)
            });
        }

        public override void Start()
        {
            base.Start(); // Calls InitializeSnapPoints

            triggerCollider = Entity.Get<StaticColliderComponent>();
            if (triggerCollider == null)
            {
                // Attempt to find it in children if not on the root
                triggerCollider = Entity.GetComponentInChildren<StaticColliderComponent>();
            }

            if (triggerCollider == null)
            {
                Log.Error($"SpikeTrap '{Entity?.Name ?? "Unnamed"}' requires a StaticColliderComponent to function as a trigger. None found on entity or children.");
                this.Enabled = false; // Disable script if no collider
                return;
            }

            if (!triggerCollider.IsTrigger)
            {
                Log.Warning($"SpikeTrap '{Entity?.Name ?? "Unnamed"}' has a StaticColliderComponent that is not set to 'IsTrigger = true'. Attempting to set it. Please verify prefab setup.");
                triggerCollider.IsTrigger = true;
            }

            // Subscribe to collision events
            triggerCollider.Collisions.CollectionChanged += Collisions_CollectionChanged;
        }

        public override void Update()
        {
            base.Update(); // Handle any base class update logic

            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            if (currentCooldown > 0f)
            {
                currentCooldown -= deltaTime;
                if (currentCooldown < 0f) currentCooldown = 0f;
            }

            // Update rearm timers for entities
            var keysToRemove = new List<Entity>();
            var entityKeys = new List<Entity>(entityRearmTimers.Keys); // Avoid modification during iteration issues

            foreach (var entityKey in entityKeys)
            {
                if (entityRearmTimers.TryGetValue(entityKey, out float remainingTime))
                {
                    remainingTime -= deltaTime;
                    if (remainingTime <= 0f)
                    {
                        keysToRemove.Add(entityKey);
                    }
                    else
                    {
                        entityRearmTimers[entityKey] = remainingTime;
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                entityRearmTimers.Remove(key);
            }
        }

        private void Collisions_CollectionChanged(object sender, Stride.Core.Collections.TrackingCollectionChangedEventArgs e)
        {
            var collision = (Collision)e.Item;
            Entity otherEntity = (triggerCollider == collision.ColliderA) ? collision.ColliderB.Entity : collision.ColliderA.Entity;

            // We are interested in new collisions (entity entering the trigger)
            if (e.Action == Stride.Core.Collections.NotifyCollectionChangedAction.Add)
            {
                ProcessSpikeTrapCollision(otherEntity);
            }
            // Optional: Handle CollisionEnded if needed (e.g. for continuous damage traps, not this one)
        }

        private void ProcessSpikeTrapCollision(Entity otherEntity)
        {
            if (otherEntity == null || otherEntity == this.Entity)
                return;

            if (IsSingleUse && !this.Enabled) // If single use and already triggered (disabled)
                return;

            if (currentCooldown > 0f)
            {
                // Log.Info($"SpikeTrap '{Entity.Name}' on global cooldown. Cannot trigger.");
                return; // Trap is on global cooldown
            }

            if (entityRearmTimers.ContainsKey(otherEntity))
            {
                // Log.Info($"SpikeTrap '{Entity.Name}' on rearm cooldown for entity '{otherEntity.Name}'.");
                return; // This specific entity is on rearm cooldown for this trap
            }

            var damageable = otherEntity.Get<IDamageable>();
            if (damageable != null)
            {
                Log.Info($"SpikeTrap '{Entity.Name}' triggered by '{otherEntity.Name}'. Dealing {DamagePerTrigger} damage.");
                damageable.TakeDamage(DamagePerTrigger, this.Entity);

                TrapTriggerSound?.Play();

                currentCooldown = TriggerCooldown;
                entityRearmTimers[otherEntity] = RearmTimePerEntity;

                if (IsSingleUse)
                {
                    Log.Info($"SpikeTrap '{Entity.Name}' is single use and has been triggered. Destroying.");
                    // We disable the component to prevent further interactions before destruction.
                    // The actual destruction might be delayed or handled by BaseBuildingPiece.
                    this.Enabled = false; 
                    // Consider calling a method that handles piece destruction properly,
                    // like the one in BaseBuildingPiece, if it handles particles/sound.
                    // For now, using Debug_ForceDestroy as per spec.
                    this.Debug_ForceDestroy(); 
                }
            }
        }
        
        public override void Cancel()
        {
            base.Cancel();
            if (triggerCollider != null)
            {
                triggerCollider.Collisions.CollectionChanged -= Collisions_CollectionChanged;
            }
        }
    }
}

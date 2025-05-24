// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Physics;
using Stride.Audio;
using Stride.Core.Mathematics;
using System; // For Math.Min
using System.Collections.Generic; // For lists if needed for other types of collision management
using FirstPersonShooter.Core; // For IDamageable (though not directly implemented, used by projectiles)
using FirstPersonShooter.Building.Pieces; // For BaseBuildingPiece
using FirstPersonShooter.Weapons.Projectiles; // For BasicTurretProjectile

namespace FirstPersonShooter.Building.Defenses
{
    public class ForcefieldGenerator : BaseBuildingPiece
    {
        // --- BaseBuildingPiece Properties ---
        private float health = 150f;
        private MaterialType structureMaterialType = MaterialType.Metal;
        public override float Health { get => health; set => health = value; }
        public override MaterialType StructureMaterialType { get => structureMaterialType; set => structureMaterialType = value; }

        // --- ForcefieldGenerator Specific Properties ---
        public float ShieldRadius { get; set; } = 10f;
        public float ShieldHealth { get; set; } = 500f;
        public float ShieldRegenRate { get; set; } = 25f; // Points per second
        public float ShieldRegenDelay { get; set; } = 5.0f; // Seconds after taking damage
        
        public bool IsActive { get; private set; } = false;
        
        public Entity ShieldVisualEntity { get; set; } // Assign in editor: Child entity for visual shield sphere
        public SoundEffectInstance ShieldActivateSound { get; set; }
        public SoundEffectInstance ShieldDeactivateSound { get; set; }
        public SoundEffectInstance ShieldImpactSound { get; set; }

        public bool IsPowered { get; set; } = true; // For now, defaults to true
        public float PowerConsumptionRate { get; set; } = 10f; // Per second, for future use

        // --- Private Fields ---
        private float currentShieldHealth;
        private float currentRegenDelayTimer;
        private StaticColliderComponent shieldTriggerCollider;

        public ForcefieldGenerator()
        {
            this.IsGroundPiece = true; 
        }

        public override void InitializeSnapPoints()
        {
            SnapPoints.Clear();
            // Example: A single snap point at its base
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = Vector3.Zero,
                LocalRotation = Quaternion.Identity,
                Type = "GeneratorBase" 
            });
        }

        public override void Start()
        {
            base.Start(); 

            currentShieldHealth = ShieldHealth;
            currentRegenDelayTimer = 0f;

            if (ShieldVisualEntity == null)
            {
                Log.Error($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' has no ShieldVisualEntity assigned.");
            }
            else
            {
                // Assuming ShieldVisualEntity is a unit sphere, scale it to ShieldRadius (diameter)
                // If it's a plane or custom model, this might need adjustment.
                ShieldVisualEntity.Transform.Scale = new Vector3(ShieldRadius * 2.0f); 
                ShieldVisualEntity.Enabled = false; // Initially inactive
            }

            shieldTriggerCollider = Entity.Get<StaticColliderComponent>();
            if (shieldTriggerCollider == null)
            {
                // Attempt to find it in children if not on the root
                shieldTriggerCollider = Entity.GetComponentInChildren<StaticColliderComponent>();
            }

            if (shieldTriggerCollider == null)
            {
                Log.Error($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' requires a StaticColliderComponent for the shield trigger. None found.");
                this.Enabled = false; 
                return;
            }

            if (!shieldTriggerCollider.IsTrigger)
            {
                Log.Warning($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' has a StaticColliderComponent that is not set to 'IsTrigger = true'. Attempting to set it. Please verify prefab setup.");
                shieldTriggerCollider.IsTrigger = true;
            }
            
            // Adjust collider shape radius
            if (shieldTriggerCollider.ColliderShapes.Count > 0 && shieldTriggerCollider.ColliderShapes[0] is SphereColliderShape sphereShape)
            {
                sphereShape.Radius = ShieldRadius;
                // If the shape was added in the editor, its description needs to be updated for the new radius to persist if properties are reset
                // This is a runtime adjustment. For persistent change, adjust SphereColliderShapeDesc.Radius in editor or code.
            }
            else if (shieldTriggerCollider.ColliderShapes.Count == 0)
            {
                Log.Warning($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}': Shield trigger collider has no shapes. Adding a SphereColliderShape with ShieldRadius.");
                var shapeDesc = new SphereColliderShapeDesc() { Radius = ShieldRadius };
                var newSphereShape = new SphereColliderShape(shapeDesc);
                shieldTriggerCollider.ColliderShapes.Add(newSphereShape);
            }
            else
            {
                 Log.Warning($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}': Shield trigger collider's first shape is not a SphereColliderShape. Radius not automatically adjusted. Ensure it matches ShieldRadius.");
            }

            shieldTriggerCollider.Collisions.CollectionChanged += Collisions_CollectionChanged;

            TryActivateShield();
        }

        public override void Update()
        {
            base.Update();
            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            if (!IsPowered && IsActive)
            {
                DeactivateShield();
                return;
            }

            if (!IsPowered)
            {
                // Future: Could have a very slow decay of shield health if unpowered but was active.
                return;
            }

            // If powered, not active, and has health, try to activate.
            if (!IsActive && currentShieldHealth > 0f)
            {
                TryActivateShield();
            }

            if (IsActive)
            {
                if (currentRegenDelayTimer > 0f)
                {
                    currentRegenDelayTimer -= deltaTime;
                    if (currentRegenDelayTimer < 0f) currentRegenDelayTimer = 0f;
                }
                else if (currentShieldHealth < ShieldHealth)
                {
                    currentShieldHealth = Math.Min(ShieldHealth, currentShieldHealth + ShieldRegenRate * deltaTime);
                }
                // Future: ConsumePower(PowerConsumptionRate * deltaTime);
            }
        }

        public void TryActivateShield()
        {
            if (IsActive || !IsPowered || currentShieldHealth <= 0f)
            {
                return;
            }

            IsActive = true;
            if (ShieldVisualEntity != null)
            {
                ShieldVisualEntity.Enabled = true;
            }
            ShieldActivateSound?.Play();
            Log.Info($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' shield activated. Health: {currentShieldHealth}/{ShieldHealth}");
        }

        public void DeactivateShield()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            if (ShieldVisualEntity != null)
            {
                ShieldVisualEntity.Enabled = false;
            }
            ShieldDeactivateSound?.Play();
            Log.Info($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' shield deactivated.");
        }

        public bool TakeShieldDamage(float amount, Entity projectileEntity)
        {
            if (!IsActive || currentShieldHealth <= 0f)
            {
                return false; // Shield not active or already broken, cannot take damage
            }

            currentShieldHealth -= amount;
            currentRegenDelayTimer = ShieldRegenDelay;
            ShieldImpactSound?.Play();
            Log.Info($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' shield took {amount} damage. Current health: {currentShieldHealth}/{ShieldHealth}");

            if (projectileEntity != null && projectileEntity.Scene != null)
            {
                // Consider a small delay or particle effect before removing projectile
                projectileEntity.Scene = null; // Destroy the projectile
                Log.Info($"Projectile '{projectileEntity.Name}' destroyed by shield.");
            }

            if (currentShieldHealth <= 0f)
            {
                currentShieldHealth = 0f;
                DeactivateShield();
                Log.Warning($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' shield broken!");
                // Optionally, trigger an event or specific sound for shield break
            }
            return true; // Damage was processed by the shield
        }

        private void Collisions_CollectionChanged(object sender, Stride.Core.Collections.TrackingCollectionChangedEventArgs e)
        {
            if (!IsActive) return;

            var collision = (Collision)e.Item;
            Entity otherEntity = (shieldTriggerCollider == collision.ColliderA) ? collision.ColliderB.Entity : collision.ColliderA.Entity;

            if (e.Action == Stride.Core.Collections.NotifyCollectionChangedAction.Add) // Projectile entered trigger
            {
                if (otherEntity == null || otherEntity == this.Entity)
                    return;

                var projectileScript = otherEntity.Get<BasicTurretProjectile>();
                if (projectileScript != null)
                {
                    // Projectile specific damage. Could also have a generic "IProjectile" interface.
                    Log.Info($"ForcefieldGenerator '{Entity?.Name ?? "Unnamed"}' detected projectile '{otherEntity.Name}'.");
                    TakeShieldDamage(projectileScript.Damage, otherEntity);
                }
                // Else: Could handle other types of entities if needed, e.g. melee attackers, vehicles.
            }
        }

        public override void OnPieceDestroyed()
        {
            DeactivateShield(); // Ensure shield is down if generator is destroyed
            base.OnPieceDestroyed(); // Call base class logic for destruction
        }

        public override void Cancel()
        {
            base.Cancel();
            if (shieldTriggerCollider != null)
            {
                shieldTriggerCollider.Collisions.CollectionChanged -= Collisions_CollectionChanged;
            }
        }
    }
}

using Stride.Engine;
using Stride.Core.Mathematics; // For Vector3, Matrix
using Stride.Physics;         // For Simulation, HitResult, SphereColliderShape
using MySurvivalGame.Game.Core; // For IDamageable, DamageType
using MySurvivalGame.Game.Player; // For PlayerEquipment, PlayerInput
using MySurvivalGame.Game.Items;  // For WeaponToolData
using System.Collections.Generic; // For List<HitResult>
using MySurvivalGame.Game.Audio; // ADDED for GameSoundManager

namespace MySurvivalGame.Game.Weapons.Melee
{
    public class Hatchet : BaseMeleeWeapon
    {
        public override void PrimaryAction()
        {
            var thisEntityName = this.Entity?.Name ?? "Hatchet";
            Log.Info($"{thisEntityName}: Hatchet Swung.");
            GameSoundManager.PlaySound("Hatchet_Swing", this.Entity.Transform.WorldMatrix.TranslationVector);

            // Assuming Hatchet script could be on Player entity or a child (weapon model) entity.
            var playerEquipment = this.Entity?.Get<PlayerEquipment>() ?? this.Entity?.GetParent()?.Get<PlayerEquipment>();
            
            if (playerEquipment == null)
            {
                Log.Error($"{thisEntityName}: Could not retrieve PlayerEquipment component.");
                return;
            }

            var weaponData = playerEquipment.currentlyEquippedItemData; // This is already WeaponToolData
            if (weaponData == null)
            {
                Log.Error($"{thisEntityName}: Could not retrieve WeaponToolData from PlayerEquipment.");
                return;
            }

            // PlayerInput component should be on the same entity as PlayerEquipment
            var camera = playerEquipment.Entity?.Get<PlayerInput>()?.Camera;
            if (camera == null)
            {
                Log.Error($"{thisEntityName}: Could not retrieve camera from PlayerInput on Player entity.");
                return;
            }

            var simulation = this.GetSimulation();
            if (simulation == null)
            {
                Log.Error($"{thisEntityName}: Physics simulation not found.");
                return;
            }

            float meleeRange = weaponData.Range > 0 ? weaponData.Range : 1.5f; 
            float meleeRadius = 0.3f; 

            Matrix cameraWorldMatrix = camera.Entity.Transform.WorldMatrix;
            Vector3 castStart = cameraWorldMatrix.TranslationVector; 
            Vector3 castEnd = castStart + cameraWorldMatrix.Forward * meleeRange;

            var sphereShape = new SphereColliderShape(false, meleeRadius);
            
            // Stride's ShapeSweep returns only the closest hit.
            HitResult closestHit = simulation.ShapeSweep(sphereShape, Matrix.Translation(castStart), Matrix.Translation(castEnd), 
                                                            filterGroup: CollisionFilterGroups.DefaultFilter, 
                                                            filterFlags: CollisionFilterGroupFlags.DefaultFilter); // Consider specific filter for only enemies/damageables

            if (closestHit.Succeeded && closestHit.Collider != null)
            {
                var hitEntity = closestHit.Collider.Entity;
                Log.Info($"{thisEntityName}: Hit '{hitEntity.Name}'.");

                var damageable = hitEntity.Get<IDamageable>();
                if (damageable != null)
                {
                    Log.Info($"{thisEntityName}: Applying {weaponData.Damage} Melee damage to '{hitEntity.Name}'.");
                    damageable.TakeDamage(weaponData.Damage, DamageType.Melee);
                }
                else
                {
                    Log.Info($"{thisEntityName}: Hit entity '{hitEntity.Name}' is not IDamageable.");
                }
            }
            else
            {
                Log.Info($"{thisEntityName}: Swing missed.");
            }
            // Future: Play swing animation, sound
        }

        public override void SecondaryAction()
        {
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet Secondary Action (e.g., block or stronger swing - TBD).");
        }

        public override void Reload()
        {
            // Typically no reload for a hatchet
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet has no reload action.");
        }

        public override void OnEquip(Entity owner)
        {
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet equipped by {owner?.Name}.");
            // Future: Change player animations, parent model to hand
        }

        public override void OnUnequip(Entity owner)
        {
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet unequipped by {owner?.Name}.");
            // Future: Revert player animations
        }
    }
}

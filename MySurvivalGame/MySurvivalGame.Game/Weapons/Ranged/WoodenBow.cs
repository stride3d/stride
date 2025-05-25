using Stride.Engine;
using Stride.Core.Mathematics;
using MySurvivalGame.Game.Player;       // For PlayerInventoryComponent
using MySurvivalGame.Game.Items;        // For WeaponToolData
using MySurvivalGame.Game.Core;         // For DamageType
using MySurvivalGame.Game.Audio;        // For GameSoundManager

namespace MySurvivalGame.Game.Weapons.Ranged
{
    public class WoodenBow : BaseBowWeapon
    {
        // WeaponToolData (from PlayerEquipment.currentlyEquippedItemData, cast to WeaponToolData) will hold stats like Damage, Range, RequiredAmmoName
        public WeaponToolData BowData { get; private set; }


        public override void OnEquip(Entity owner)
        {
            base.OnEquip(owner); // Call base if it has logic
            var playerEquipment = owner?.Get<PlayerEquipment>();
            if (playerEquipment != null && playerEquipment.currentlyEquippedItemData is WeaponToolData wtd)
            {
                this.BowData = wtd;
                Log.Info($"{Entity.Name}: Wooden Bow equipped. Ammo type: {this.BowData.RequiredAmmoName}");
            }
        }
        
        protected override void ReleaseArrow(float chargeTime)
        {
            if (BowData == null)
            {
                Log.Error("WoodenBow: BowData not set. Cannot fire.");
                return;
            }

            var playerEntity = this.Entity?.GetParent(); // Or however player entity is found
            var playerInventory = playerEntity?.Get<PlayerInventoryComponent>();

            if (playerInventory == null)
            {
                Log.Error("WoodenBow: PlayerInventoryComponent not found.");
                return;
            }

            if (playerInventory.ConsumeItemByName(BowData.RequiredAmmoName, 1))
            {
                Log.Info($"{Entity.Name}: Arrow fired with charge {chargeTime:F2}s. (Conceptual Raycast)");
                GameSoundManager.PlaySound("Bow_Shoot", Entity.Transform.WorldMatrix.TranslationVector);

                var camera = GetCamera(); // Assumes GetCamera() helper exists as in Pistol.cs
                if (camera != null)
                {
                    Matrix cameraWorldMatrix = camera.Entity.Transform.WorldMatrix;
                    Vector3 raycastStart = cameraWorldMatrix.TranslationVector;
                    Vector3 raycastDirection = cameraWorldMatrix.Forward;
                    // Range and Damage could be affected by chargeTime in a more complex system
                    float range = BowData.Range; 
                    float damage = BowData.Damage;

                    var simulation = this.GetSimulation();
                    var hitResult = simulation.Raycast(raycastStart, raycastStart + raycastDirection * range);
                    if (hitResult.Succeeded && hitResult.Collider?.Entity.Get<IDamageable>() != null)
                    {
                        Log.Info($"WoodenBow: Hit {hitResult.Collider.Entity.Name}. Applying {damage} damage.");
                        hitResult.Collider.Entity.Get<IDamageable>().TakeDamage(damage, DamageType.Ranged);
                    } else if (hitResult.Succeeded) {
                        Log.Info($"WoodenBow: Hit {hitResult.Collider.Entity.Name}, but it's not Damageable.");
                    } else {
                        Log.Info($"WoodenBow: Arrow missed.");
                    }
                }
            }
            else
            {
                Log.Info($"{Entity.Name}: No '{BowData.RequiredAmmoName}' in inventory!");
                GameSoundManager.PlaySound("Bow_Empty", Entity.Transform.WorldMatrix.TranslationVector);
            }
        }

        // Helper to get camera, similar to Pistol.cs
        private CameraComponent GetCamera()
        {
            var playerScriptOwner = this.Entity?.Get<PlayerEquipment>() != null ? this.Entity : this.Entity?.GetParent();
            return playerScriptOwner?.Get<PlayerInput>()?.Camera;
        }

        // SecondaryAction for bows could be nocking a different arrow type, or aiming down sights if applicable
        public override void SecondaryAction() { Log.Info($"{Entity.Name}: Wooden Bow Secondary Action (e.g. Aim TBD)."); }
        public override void Reload() { Log.Info($"{Entity.Name}: Wooden Bow does not reload in the traditional sense."); }

    }
}

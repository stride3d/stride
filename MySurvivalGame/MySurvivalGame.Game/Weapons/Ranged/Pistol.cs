using Stride.Engine;
using Stride.Core.Mathematics; // For Matrix, Vector3 for camera access
using MySurvivalGame.Game.Player; 
using MySurvivalGame.Game.Items;  
using MySurvivalGame.Game.Audio; // ADDED for GameSoundManager

namespace MySurvivalGame.Game.Weapons.Ranged
{
    public class Pistol : BaseRangedWeapon
    {
        // WeaponToolData will hold the authoritative stats from inventory.
        // These can be initialized when the weapon script is equipped.
        public override int ClipSize { get; protected set; } = 7;
        public int ActualCurrentAmmoInClip { get; set; } // This will be managed by the script instance
        public int ActualReserveAmmo { get; set; }     // This will be managed by the script instance


        public override void PrimaryAction()
        {
            if (ActualCurrentAmmoInClip > 0)
            {
                ActualCurrentAmmoInClip--;
                Log.Info($"{this.Entity.Name}: Pistol Fired. Ammo: {ActualCurrentAmmoInClip}/{ClipSize}. Reserve: {ActualReserveAmmo}");
                
                // Get camera for raycast origin and direction
                var camera = GetCamera(); // Helper method to get camera
                if (camera != null)
                {
                    Matrix cameraWorldMatrix = camera.Entity.Transform.WorldMatrix;
                    Vector3 raycastStart = cameraWorldMatrix.TranslationVector;
                    Vector3 raycastDirection = cameraWorldMatrix.Forward;
                    float range = (this.Entity.Get<PlayerEquipment>()?.currentlyEquippedItemData as WeaponToolData)?.Range ?? 25f; // Get range from data or default
                    
                    ShootRaycast(raycastStart, raycastDirection, range);
                }
                GameSoundManager.PlaySound("Pistol_Shoot", this.Entity.Transform.WorldMatrix.TranslationVector);
                // Future: muzzle flash
            }
            else
            {
                Log.Info($"{this.Entity.Name}: Pistol click (empty).");
                GameSoundManager.PlaySound("Pistol_EmptyClick", this.Entity.Transform.WorldMatrix.TranslationVector);
            }
        }

        public override void Reload()
        {
            if (ActualCurrentAmmoInClip >= ClipSize)
            {
                Log.Info($"{this.Entity.Name}: Pistol clip already full.");
                return;
            }

            if (ActualReserveAmmo > 0)
            {
                int ammoToReload = ClipSize - ActualCurrentAmmoInClip;
                int ammoToTakeFromReserve = System.Math.Min(ammoToReload, ActualReserveAmmo);

                ActualCurrentAmmoInClip += ammoToTakeFromReserve;
                ActualReserveAmmo -= ammoToTakeFromReserve;
                Log.Info($"{this.Entity.Name}: Pistol Reloaded. Ammo: {ActualCurrentAmmoInClip}/{ClipSize}. Reserve: {ActualReserveAmmo}");
                GameSoundManager.PlaySound("Pistol_Reload", this.Entity.Transform.WorldMatrix.TranslationVector);
                // Future: Play animation
            }
            else
            {
                Log.Info($"{this.Entity.Name}: Pistol - No reserve ammo to reload.");
            }
        }
        
        public override void OnEquip(Entity owner)
        {
            Log.Info($"{this.Entity.Name}: Pistol equipped by {owner?.Name}.");
            // Future: Initialize ActualCurrentAmmoInClip and ActualReserveAmmo from WeaponToolData.
            // For example, if WeaponToolData stores these directly or has fields to map.
            // This will be handled in the PlayerEquipment.EquipItem step more explicitly.
            var playerEquipment = owner?.Get<PlayerEquipment>(); // Correctly get PlayerEquipment from owner
            if(playerEquipment?.currentlyEquippedItemData is WeaponToolData weaponData) // Check if it's WeaponToolData
            {
                // Example: this.ClipSize = weaponData.ClipSize; // If data-driven
                ActualCurrentAmmoInClip = ClipSize; 
                ActualReserveAmmo = 50; // Default placeholder
            }
        }

        public override void OnUnequip(Entity owner)
        {
            Log.Info($"{this.Entity.Name}: Pistol unequipped by {owner?.Name}.");
            // Future: Potentially save current ammo state back to WeaponToolData if it's instance specific.
        }

        private CameraComponent GetCamera()
        {
            // Assuming PlayerEquipment is on Player, and PlayerInput is also on Player and has Camera linked.
            var playerEntity = this.Entity?.GetParent(); // If weapon is child of PlayerEquipment entity. Or just Entity if weapon script is on Player.
                                                        // This needs to be robust based on where PlayerEquipment puts weapon entities.
                                                        // For now, assume PlayerEquipment is on Player, and PlayerInput is also on Player.
            if (this.Entity?.Get<PlayerEquipment>() != null) // If this script is on the same entity as PlayerEquipment
            {
               return this.Entity.Get<PlayerInput>()?.Camera;
            }
            // If this script is on a weapon entity that's a child of the Player entity:
            return this.Entity?.GetParent()?.Get<PlayerInput>()?.Camera;
        }
    }
}

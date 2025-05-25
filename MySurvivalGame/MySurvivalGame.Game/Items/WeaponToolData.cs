using Stride.Graphics; // For Texture, if base class needs it directly
using MySurvivalGame.Game.Items; // For MockInventoryItem and EquipmentType

namespace MySurvivalGame.Game.Items
{
    public enum SpecialBonusType // Could be expanded (e.g., specific enemy types, etc.)
    {
        None,
        Mining,
        Woodcutting,
        Combat // General combat bonus, or could be more specific
    }

    public class WeaponToolData : MockInventoryItem
    {
        public float Damage { get; set; }
        public float FireRate { get; set; } // Shots or swings per second
        public float Range { get; set; } // Effective range in game units (e.g., meters)
        public SpecialBonusType BonusType { get; set; }
        public float DurabilityPoints { get; set; }
        public float MaxDurabilityPoints { get; set; }
        public bool IsBroken { get; private set; } 

        // Ammo properties - relevant if CurrentEquipmentType is a ranged weapon
        public int ClipSize { get; set; } = 0;
        public int CurrentAmmoInClip { get; set; } 
        public int ReserveAmmo { get; set; }     

        // Constructor
        public WeaponToolData(
            string name, 
            string itemType, // General type like "Axe", "Pistol"
            string description, 
            EquipmentType equipmentType, // Specific like Weapon, Tool
            float damage,
            float fireRate,
            float range,
            float maxDurability,
            SpecialBonusType bonusType = SpecialBonusType.None,
            Texture icon = null, 
            int quantity = 1, 
            int maxStackSize = 1, 
            float? initialDurability = null,
            // Ammo related parameters - only relevant for certain types
            int clipSize = 0, 
            int currentAmmoInClip = 0, 
            int reserveAmmo = 0
        ) : base(name, itemType, description, icon, quantity, (initialDurability ?? maxDurability) / maxDurability, maxStackSize, equipmentType) 
        {
            Damage = damage;
            FireRate = fireRate;
            Range = range;
            BonusType = bonusType;
            MaxDurabilityPoints = maxDurability;
            DurabilityPoints = initialDurability ?? maxDurability; 
            
            if (equipmentType == EquipmentType.Weapon) // Could be more specific like RangedWeapon if enum expands
            {
                this.ClipSize = clipSize;
                this.CurrentAmmoInClip = currentAmmoInClip;
                this.ReserveAmmo = reserveAmmo;
            }
            
            UpdateBaseDurability();
            IsBroken = DurabilityPoints <= 0; 
        }

        // Method to update durability and IsBroken status, and sync base.Durability
        public void UpdateDurability(float newDurabilityPoints)
        {
            DurabilityPoints = newDurabilityPoints;
            if (DurabilityPoints < 0) DurabilityPoints = 0;
            if (DurabilityPoints > MaxDurabilityPoints) DurabilityPoints = MaxDurabilityPoints;

            IsBroken = DurabilityPoints <= 0;
            
            UpdateBaseDurability();
        }

        private void UpdateBaseDurability()
        {
            // Sync with base.Durability for UI
            if (MaxDurabilityPoints > 0)
                base.Durability = DurabilityPoints / MaxDurabilityPoints;
            else
                base.Durability = null; // Or 1.0f if it should appear full but non-applicable
        }
    }
}

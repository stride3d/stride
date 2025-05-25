using Stride.Engine;
using Stride.Core; // For [DataMember]
using MySurvivalGame.Game.Items; // For MockInventoryItem, WeaponToolData, SpecialBonusType, EquipmentType
using MySurvivalGame.Game.Player; // For PlayerInventoryComponent
using MySurvivalGame.Game.Audio; // ADDED for GameSoundManager
// Potentially System.Linq if FirstOrDefault or similar is used later.

namespace MySurvivalGame.Game.World
{
    public enum ResourceNodeType 
    {
        Wood,
        Stone,
        MetalOre,
        Generic 
    }

    public enum RequiredToolCategory 
    {
        None, 
        Axe,
        Pickaxe,
        Drill,
        Hand 
    }

    public class ResourceNodeComponent : SyncScript 
    {
        [DataMember] 
        public ResourceNodeType NodeType { get; set; } = ResourceNodeType.Wood;

        [DataMember]
        public int TotalResources { get; set; } = 100;

        [DataMember]
        public int HarvestAmountPerHit { get; set; } = 10;

        [DataMember]
        public RequiredToolCategory ToolCategory { get; set; } = RequiredToolCategory.Axe;
        
        // Optional: could add a field for the specific item name to drop, e.g. "OakWoodLog" vs "PineWoodLog"
        // string lootItemName = null; 

        public MockInventoryItem HitNode(WeaponToolData hittingTool, PlayerInventoryComponent playerInventory)
        {
            if (TotalResources <= 0)
            {
                Log.Info($"Node '{this.Entity.Name}' is depleted.");
                return null;
            }

            bool toolCompatible = false;
            if (ToolCategory == RequiredToolCategory.None) 
            {
                toolCompatible = true;
            }
            else if (hittingTool != null)
            {
                // Map WeaponToolData.SpecialBonusType to RequiredToolCategory
                switch (ToolCategory)
                {
                    case RequiredToolCategory.Axe:
                        toolCompatible = hittingTool.BonusType == SpecialBonusType.Woodcutting;
                        break;
                    case RequiredToolCategory.Pickaxe:
                        toolCompatible = hittingTool.BonusType == SpecialBonusType.Mining; 
                        break;
                    case RequiredToolCategory.Drill:
                        toolCompatible = hittingTool.BonusType == SpecialBonusType.Mining; 
                        break;
                    // case RequiredToolCategory.Hand: // Placeholder for future
                    //    toolCompatible = (hittingTool == null || hittingTool.BonusType == SpecialBonusType.None); 
                    //    break;
                }
            } else if (ToolCategory == RequiredToolCategory.Hand) {
                // If no tool is equipped (hittingTool is null), and node is harvestable by hand
                toolCompatible = true;
            }


            if (!toolCompatible)
            {
                Log.Info($"Ineffective hit on '{this.Entity.Name}'. Required tool: {ToolCategory}, Used tool bonus: {hittingTool?.BonusType.ToString() ?? "None"}.");
                return null;
            }

            int actualHarvestAmount = System.Math.Min(HarvestAmountPerHit, TotalResources);
            TotalResources -= actualHarvestAmount;

            // Determine item to give based on NodeType
            string itemName = "Harvested Item";
            string itemDesc = "A harvested resource.";
            MySurvivalGame.Game.Items.EquipmentType itemEquipmentType = MySurvivalGame.Game.Items.EquipmentType.None; // Usually resources are None
            int itemMaxStack = 64;

            switch (NodeType)
            {
                case ResourceNodeType.Wood:
                    itemName = "Wood Log";
                    itemDesc = "A log of wood.";
                    break;
                case ResourceNodeType.Stone:
                    itemName = "Stone Chunk";
                    itemDesc = "A chunk of stone.";
                    break;
                case ResourceNodeType.MetalOre:
                    itemName = "Metal Ore";
                    itemDesc = "Raw metal ore.";
                    itemMaxStack = 32; 
                    break;
                case ResourceNodeType.Generic:
                    itemName = "Generic Resource";
                    itemDesc = "A generic resource.";
                    break;
            }
            
            MockInventoryItem harvestedItemData = new MockInventoryItem(itemName, "Resource", itemDesc, null, actualHarvestAmount, null, itemMaxStack, itemEquipmentType);
            
            // We need a new instance to add to inventory, as playerInventory.AddItem might modify quantity of the passed item.
            MockInventoryItem itemInstanceToAdd = new MockInventoryItem(harvestedItemData.Name, harvestedItemData.ItemType, harvestedItemData.Description, 
                                                                  harvestedItemData.Icon, harvestedItemData.Quantity, harvestedItemData.Durability, 
                                                                  harvestedItemData.MaxStackSize, harvestedItemData.CurrentEquipmentType);

            if (playerInventory.AddItem(itemInstanceToAdd))
            {
                Log.Info($"Harvested {actualHarvestAmount} '{itemName}' from '{this.Entity.Name}'. Remaining: {TotalResources}");
                // ADDED: Play sound on successful harvest
                string hitSoundName = "Hit_Generic";
                switch (NodeType)
                {
                    case ResourceNodeType.Wood: hitSoundName = "Hit_Wood"; break;
                    case ResourceNodeType.Stone: hitSoundName = "Hit_Stone"; break;
                    case ResourceNodeType.MetalOre: hitSoundName = "Hit_MetalOre"; break;
                }
                GameSoundManager.PlaySound(hitSoundName, this.Entity.Transform.WorldMatrix.TranslationVector);
            }
            else
            {
                Log.Warning($"Inventory full. Could not add '{itemName}' from '{this.Entity.Name}'. Resource NOT consumed from node this time.");
                TotalResources += actualHarvestAmount; // Rollback consumption
                return null;
            }

            if (TotalResources <= 0)
            {
                Log.Info($"Node '{this.Entity.Name}' depleted.");
                GameSoundManager.PlaySound("Node_Depleted", this.Entity.Transform.WorldMatrix.TranslationVector); // ADDED
                // Optional: Deactivate or remove the node entity
                this.Entity.Enabled = false; 
            }
            // Return a copy of what was added to inventory (or the original if AddItem modified it and returned a reference to it)
            // For safety, returning the instance that was successfully processed by AddItem (if it handles stacking internally and returns a reference)
            // or a new instance representing what was added.
            // Since AddItem might stack, the 'harvestedItemData' with 'actualHarvestAmount' is what was attempted to be added.
            return harvestedItemData; 
        }
    }
}

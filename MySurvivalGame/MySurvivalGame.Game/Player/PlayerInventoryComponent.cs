// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.

using System.Collections.Generic;
using System.Linq; // For FirstOrDefault
using MySurvivalGame.Game.Items; // For MockInventoryItem
using Stride.Core; // For DataMemberIgnore attribute if needed by ScriptComponent
using Stride.Engine;

namespace MySurvivalGame.Game.Player
{
    public class PlayerInventoryComponent : ScriptComponent
    {
        public List<MockInventoryItem> AllPlayerItems { get; private set; } = new List<MockInventoryItem>();

        // This Start method is for testing purposes, to populate initial inventory
        public override void Start()
        {
            base.Start();
            if (AllPlayerItems.Count == 0) // Only add if inventory is empty
            {
                // Existing items
                AddItem(new MockInventoryItem("Wood", "Resource", "A sturdy piece of wood.", null, 30, null, 64, EquipmentType.None));
                AddItem(new MockInventoryItem("Stone", "Resource", "A common grey stone.", null, 90, null, 64, EquipmentType.None));
                
                // Updated Iron Axe to have SpecialBonusType.None
                AddItem(new WeaponToolData("Iron Axe", "Tool", "A basic axe.", EquipmentType.Tool, 
                                           damage: 15f, fireRate: 1.0f, range: 1.5f, maxDurability: 120f, 
                                           bonusType: SpecialBonusType.None, // Explicitly set to None
                                           initialDurability: 120f,
                                           clipSize: 0, currentAmmoInClipPersisted: 0, reserveAmmoPersisted: 0)); // Non-ammo weapon

                AddItem(new MockInventoryItem("Health Potion", "Consumable", "Restores health.", null, 5, null, 10, EquipmentType.Consumable));
                
                // Updated "Old Pistol" with ammo data
                AddItem(new WeaponToolData("Old Pistol", "Weapon", "An old pistol.", EquipmentType.Weapon,
                                           damage: 20f, fireRate: 2.0f, range: 25f, maxDurability: 80f,
                                           bonusType: SpecialBonusType.Combat, 
                                           clipSize: 7, 
                                           currentAmmoInClipPersisted: 7, 
                                           reserveAmmoPersisted: 21));

                // New Test Tool Items
                AddItem(new WeaponToolData("Logging Axe", "Tool", "An axe specialized for felling trees.", EquipmentType.Tool, 
                                           damage: 12f, fireRate: 0.8f, range: 1.7f, maxDurability: 100f, 
                                           bonusType: SpecialBonusType.Woodcutting, 
                                           initialDurability: 100f,
                                           clipSize: 0, currentAmmoInClipPersisted: 0, reserveAmmoPersisted: 0)); // Non-ammo weapon
                                           
                AddItem(new WeaponToolData("Stone Pickaxe", "Tool", "A pickaxe for mining stone and ore.", EquipmentType.Tool, 
                                           damage: 10f, fireRate: 0.7f, range: 1.8f, maxDurability: 150f, 
                                           bonusType: SpecialBonusType.Mining, 
                                           initialDurability: 150f,
                                           clipSize: 0, currentAmmoInClipPersisted: 0, reserveAmmoPersisted: 0)); // Non-ammo weapon
            }
        }

        public bool AddItem(MockInventoryItem itemToAdd)
        {
            if (itemToAdd == null || itemToAdd.Quantity <= 0) return false;

            // Attempt to stack with existing items
            if (itemToAdd.IsStackable)
            {
                foreach (var existingItem in AllPlayerItems)
                {
                    if (existingItem.Name == itemToAdd.Name && // Simple name check for stackability for now
                        existingItem.IsStackable &&
                        existingItem.Quantity < existingItem.MaxStackSize)
                    {
                        int canAdd = existingItem.MaxStackSize - existingItem.Quantity;
                        int willAdd = System.Math.Min(itemToAdd.Quantity, canAdd);

                        existingItem.Quantity += willAdd;
                        itemToAdd.Quantity -= willAdd;
                        Log.Info($"PlayerInventory: Stacked {willAdd} of {itemToAdd.Name}. Remaining to add: {itemToAdd.Quantity}");
                        if (itemToAdd.Quantity <= 0) return true; // Fully stacked
                    }
                }
            }

            // If item still has quantity, add as new stack or new item
            if (itemToAdd.Quantity > 0)
            {
                // Optional: Check for inventory capacity if you have a max slot limit
                // For now, just add.
                // Create a new instance to avoid modifying the original itemToAdd if it's used elsewhere
                var newItemInstance = new MockInventoryItem(
                    itemToAdd.Name, 
                    itemToAdd.ItemType, 
                    itemToAdd.Description, 
                    itemToAdd.Icon, 
                    itemToAdd.Quantity, // This is the remaining quantity
                    itemToAdd.Durability, 
                    itemToAdd.MaxStackSize, 
                    itemToAdd.CurrentEquipmentType
                );
                AllPlayerItems.Add(newItemInstance);
                Log.Info($"PlayerInventory: Added new stack of {newItemInstance.Quantity} of {newItemInstance.Name}.");
                return true;
            }
            // This return false would only be hit if itemToAdd.Quantity was initially > 0,
            // then fully stacked, then itemToAdd.Quantity became 0, then somehow the code
            // didn't return true inside the loop. This path should ideally not be hit
            // if logic is correct. If it was not stackable or no stacking occurred,
            // the second if (itemToAdd.Quantity > 0) handles it.
            return false; 
        }

        public void RemoveItem(MockInventoryItem itemToRemove)
        {
            if (itemToRemove == null) return;
            // For non-stackable items or removing a whole stack by instance
            var itemInstance = AllPlayerItems.FirstOrDefault(i => i.UniqueId == itemToRemove.UniqueId);
            if (itemInstance != null) 
            {
                bool removed = AllPlayerItems.Remove(itemInstance);
                if(removed) Log.Info($"PlayerInventory: Removed item {itemToRemove.Name} (ID: {itemToRemove.UniqueId}).");
            }
        }

        public bool TryConsumeQuantity(System.Guid itemId, int quantityToConsume)
        {
            var itemInstance = AllPlayerItems.FirstOrDefault(i => i.UniqueId == itemId);
            if (itemInstance != null)
            {
                if (itemInstance.Quantity >= quantityToConsume)
                {
                    itemInstance.Quantity -= quantityToConsume;
                    Log.Info($"PlayerInventory: Consumed {quantityToConsume} of {itemInstance.Name}. Remaining: {itemInstance.Quantity}.");
                    if (itemInstance.Quantity <= 0)
                    {
                        AllPlayerItems.Remove(itemInstance);
                        Log.Info($"PlayerInventory: Removed empty stack of {itemInstance.Name} after consumption.");
                    }
                    return true;
                }
                else
                {
                    Log.Warning($"PlayerInventory: Not enough quantity to consume {quantityToConsume} of {itemInstance.Name}. Has: {itemInstance.Quantity}.");
                }
            }
            else
            {
                 Log.Warning($"PlayerInventory: Item with ID {itemId} not found for consumption.");
            }
            return false;
        }
    }
}

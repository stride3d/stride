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

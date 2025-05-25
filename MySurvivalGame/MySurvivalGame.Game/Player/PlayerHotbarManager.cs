// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.

using Stride.Engine;
using Stride.Engine.Events; 
using MySurvivalGame.Game.Items; 
using MySurvivalGame.Game.UI.Scripts; // ADDED: For InventoryPanelScript
using System.Linq; // ADDED: For FirstOrDefault

namespace MySurvivalGame.Game.Player
{
    public class PlayerHotbarManager : ScriptComponent
    {
        /// <summary>
        /// Represents the items currently in the player's hotbar.
        /// Assumes a fixed size of 8 slots.
        /// </summary>
        public MockInventoryItem[] HotbarItems { get; private set; } = new MockInventoryItem[8];
        private EventReceiver<int> hotbarSlotSelectedReceiver; 
        private InventoryPanelScript inventoryPanelScript; // ADDED

        /// <summary>
        /// Updates a specific slot in the hotbar with a new item.
        /// This method is typically called by the inventory UI system after a drag-and-drop operation
        /// or when items are moved to/from the hotbar.
        /// </summary>
        /// <param name="slotIndex">The index of the hotbar slot to update (0-7).</param>
        /// <param name="item">The item to place in the slot. Can be null if the slot is being cleared.</param>
        public void UpdateHotbarSlot(int slotIndex, MockInventoryItem item)
        {
            if (slotIndex < 0 || slotIndex >= HotbarItems.Length)
            {
                Log.Error($"PlayerHotbarManager: Invalid slot index {slotIndex}. Must be between 0 and {HotbarItems.Length - 1}.");
                return;
            }

            HotbarItems[slotIndex] = item;
            Log.Info($"Hotbar slot {slotIndex} updated with item: {item?.Name ?? "Empty"}");

            // Future: Could broadcast an event here if other systems need to know about hotbar changes
            // e.g., PlayerHotbarUpdatedEventKey.Broadcast(slotIndex, item);
        }

        public override void Start()
        {
            // Initialize all hotbar slots to null (empty) if not already.
            for (int i = 0; i < HotbarItems.Length; i++)
            {
                if (HotbarItems[i] != null) // If items were somehow pre-assigned via editor (not typical for this setup)
                {
                     Log.Info($"Hotbar slot {i} pre-initialized with item: {HotbarItems[i].Name}");
                }
                else
                {
                    HotbarItems[i] = null;
                }
            }
            Log.Info("PlayerHotbarManager started and hotbar initialized.");

            // Initialize event receiver
            hotbarSlotSelectedReceiver = new EventReceiver<int>(MySurvivalGame.Game.PlayerInput.HotbarSlotSelectedEventKey);

            // ADDED: Find InventoryPanelScript
            var inventoryUIEntity = Entity.Scene?.RootEntities.FirstOrDefault(e => e.Name == "InventoryUI");
            if (inventoryUIEntity != null)
            {
                inventoryPanelScript = inventoryUIEntity.Get<InventoryPanelScript>();
            }
            if (inventoryPanelScript == null)
            {
                Log.Warning("PlayerHotbarManager: Could not find InventoryPanelScript in the scene.");
            }
        }

        public override void Update() 
        {
            // Check if a hotbar slot selection event was received
            int selectedSlotIndex;
            if (hotbarSlotSelectedReceiver.TryReceive(out selectedSlotIndex))
            {
                // Validate index (PlayerInput should send valid 0-7 based on keys 1-8)
                if (selectedSlotIndex >= 0 && selectedSlotIndex < HotbarItems.Length)
                {
                    MockInventoryItem itemInSlot = HotbarItems[selectedSlotIndex];
                    var playerEquipment = Entity.Get<PlayerEquipment>();
                    var playerInventory = Entity.Get<PlayerInventoryComponent>();

                    if (itemInSlot != null)
                    {
                        Log.Info($"PlayerHotbarManager: Slot {selectedSlotIndex + 1} selected. Item: '{itemInSlot.Name}', Type: '{itemInSlot.ItemType}', EquipmentType: '{itemInSlot.CurrentEquipmentType}'.");

                        if (itemInSlot.CurrentEquipmentType == MySurvivalGame.Game.Items.EquipmentType.Weapon || 
                            itemInSlot.CurrentEquipmentType == MySurvivalGame.Game.Items.EquipmentType.Tool)
                        {
                            playerEquipment?.EquipItem(itemInSlot);
                        }
                        else if (itemInSlot.CurrentEquipmentType == MySurvivalGame.Game.Items.EquipmentType.Consumable) // ADDED: Consumable logic
                        {
                            Log.Info($"PlayerHotbarManager: Attempting to use Consumable: '{itemInSlot.Name}'.");
                            if (playerInventory != null)
                            {
                                bool consumed = playerInventory.TryConsumeQuantity(itemInSlot.UniqueId, 1);
                                if (consumed)
                                {
                                    Log.Info($"PlayerHotbarManager: Consumed '{itemInSlot.Name}'."); 
                                    
                                    var itemInInventory = playerInventory.AllPlayerItems.FirstOrDefault(i => i.UniqueId == itemInSlot.UniqueId);
                                    if (itemInInventory == null || itemInInventory.Quantity == 0)
                                    {
                                        HotbarItems[selectedSlotIndex] = null; 
                                        Log.Info($"PlayerHotbarManager: Item '{itemInSlot.Name}' depleted from inventory and removed from hotbar slot {selectedSlotIndex + 1}.");
                                    } 
                                    else if (itemInInventory != null) 
                                    {
                                        HotbarItems[selectedSlotIndex].Quantity = itemInInventory.Quantity; 
                                        if(HotbarItems[selectedSlotIndex].Quantity <= 0) HotbarItems[selectedSlotIndex] = null;
                                    }
                                    inventoryPanelScript?.RefreshInventoryDisplay(); 
                                }
                                else
                                {
                                    Log.Warning($"PlayerHotbarManager: Failed to consume '{itemInSlot.Name}'.");
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.Info($"PlayerHotbarManager: Slot {selectedSlotIndex + 1} selected. Slot is empty.");
                        playerEquipment?.EquipItem(null); 
                    }
                }
                else
                {
                    Log.Warning($"PlayerHotbarManager: Received invalid slot index {selectedSlotIndex} from HotbarSlotSelectedEventKey.");
                }
            }
        }

        // In a full game, you might have methods here like:
        // - GetSelectedItem(): Returns the item in the currently selected hotbar slot.
        // - UseItem(int slotIndex): Logic to use/consume the item in the specified slot.
        // - SelectSlot(int slotIndex): Logic to handle visual feedback for selected slot.
    }
}

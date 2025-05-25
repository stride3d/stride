// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics; // For Texture
using System; // For Guid

namespace MySurvivalGame.Game.Items // MODIFIED: Namespace updated
{
    public enum EquipmentType
    {
        None,
        Weapon,
        Tool,
        Armor, 
        Consumable,
        Deployable 
    }

    public class MockInventoryItem
    {
        // Existing Properties
        public string Name { get; set; }
        public string ItemType { get; set; } 
        public string Description { get; set; }
        public Texture Icon { get; set; } 
        public int Quantity { get; set; }
        public float? Durability { get; set; }

        // New Properties
        public Guid UniqueId { get; private set; }
        public int MaxStackSize { get; set; }
        public EquipmentType CurrentEquipmentType { get; set; }
        public bool IsStackable => MaxStackSize > 1;

        // Constructor updated for new properties
        public MockInventoryItem(string name, 
                                 string itemType, 
                                 string description, 
                                 Texture icon = null, 
                                 int quantity = 1, 
                                 float? durability = null, 
                                 int maxStackSize = 1, 
                                 EquipmentType equipmentType = EquipmentType.None)
        {
            Name = name;
            ItemType = itemType;
            Description = description;
            Icon = icon;
            Quantity = quantity;
            Durability = durability;

            // Initialize new properties
            UniqueId = Guid.NewGuid();
            MaxStackSize = maxStackSize;
            CurrentEquipmentType = equipmentType;
        }
    }
}

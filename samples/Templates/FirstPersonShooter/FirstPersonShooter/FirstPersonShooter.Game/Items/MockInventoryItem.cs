// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics; // For Texture

namespace FirstPersonShooter.Items
{
    public class MockInventoryItem
    {
        public string Name { get; set; }
        public string ItemType { get; set; } // Renamed from Type to avoid keyword clash
        public string Description { get; set; }
        public Texture Icon { get; set; } // For ItemSlotScript.SetItemData
        public int Quantity { get; set; }
        public float? Durability { get; set; }
        // Add other stats if needed, e.g., Dictionary<string, string> Stats for "Damage: 10", "Armor: 5"

        public MockInventoryItem(string name, string itemType, string description, Texture icon = null, int quantity = 1, float? durability = null)
        {
            Name = name;
            ItemType = itemType;
            Description = description;
            Icon = icon;
            Quantity = quantity;
            Durability = durability;
        }
    }
}

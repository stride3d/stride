// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Graphics; // For Texture

namespace FirstPersonShooter.Items.Crafting
{
    public class CraftingRecipe
    {
        public string RecipeID { get; set; } // Unique ID for the recipe
        public string ItemIDToCraft { get; set; } // ID of the item this recipe crafts
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public Texture Icon { get; set; } // Icon of the item to craft
        public List<RequiredResource> RequiredResources { get; set; } = new List<RequiredResource>();
        public float CraftingTime { get; set; } = 1.0f; // Seconds
        public int OutputQuantity { get; set; } = 1; // How many items are crafted
        // Future: Category, IsUnlockedByDefault, EngramRequirement, etc.
    }
}

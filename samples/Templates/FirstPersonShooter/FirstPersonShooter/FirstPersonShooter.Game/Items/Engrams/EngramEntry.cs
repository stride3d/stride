// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Graphics; // For Texture, changed from Stride.Engine
using Stride.Core.Mathematics; // For Vector2

namespace FirstPersonShooter.Items.Engrams 
{
    public enum EngramStatus { Locked, Unlockable, Unlocked }

    public class EngramEntry
    {
        public string EngramID { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public Texture Icon { get; set; }
        public int EngramPointCost { get; set; }
        public int RequiredPlayerLevel { get; set; }
        public List<string> PrerequisiteEngramIDs { get; set; } = new List<string>();
        public List<string> UnlocksRecipeIDs { get; set; } = new List<string>(); // CraftingRecipe IDs
        public EngramStatus Status { get; set; } = EngramStatus.Locked;
        public Vector2 UIPosition { get; set; } // For layout on the EngramTreeCanvas
        // Future: Category string
    }
}

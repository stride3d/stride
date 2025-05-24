// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics; // For Texture

namespace FirstPersonShooter.Items.Crafting
{
    public class RequiredResource
    {
        public string ItemID { get; set; } // ID of the resource item
        public string DisplayName { get; set; } // Name for display
        public int Quantity { get; set; }
        public Texture Icon { get; set; } // Optional: Icon for the resource itself
    }
}

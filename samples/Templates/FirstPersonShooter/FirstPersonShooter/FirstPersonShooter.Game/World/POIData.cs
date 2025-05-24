// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics; // For Texture
using Stride.Core.Mathematics; // For Vector3

namespace FirstPersonShooter.World
{
    public enum POIType { Landmark, ResourceNode, Quest, PlayerBase, CustomMarker }

    public class POIData
    {
        public string ID { get; set; } // Unique ID
        public string Name { get; set; }
        public POIType Type { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Texture IconTexture { get; set; } // Texture for the icon
        public bool IsDiscovered { get; set; } = true; // Default to discovered for now
        // Future: string Description, bool ShowOnMinimap, bool ShowOnFullMap
    }
}

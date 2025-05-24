// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.Building.Pieces
{
    public class FoundationPiece : ScriptComponent
    {
        /// <summary>
        /// The health of this foundation piece.
        /// </summary>
        public float Health { get; set; } = 500f;

        /// <summary>
        /// The material type of this structure, for impact sounds or damage calculations.
        /// </summary>
        public MaterialType StructureMaterialType { get; set; } = MaterialType.Wood;

        public override void Start()
        {
            Log.Info($"{Entity?.Name ?? "FoundationPiece"} initialized with Health: {Health}, Material: {StructureMaterialType}");

            // Future: Could add this to a list of player's buildings,
            // or register with a destruction system, etc.
        }

        // Future methods:
        // public void TakeDamage(float amount) { ... }
        // public void Repair(float amount) { ... }
        // public void OnDestroyed() { ... }
    }
}

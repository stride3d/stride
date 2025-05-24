// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.Weapons.Ranged
{
    public class WoodenBow : BaseBowWeapon
    {
        public WoodenBow()
        {
            // Default values for a Wooden Bow
            this.Damage = 20f;              // Base damage, will be scaled by draw strength in BaseBowWeapon
            this.DrawTime = 1.2f;           // Seconds for full draw
            this.ArrowLaunchSpeed = 25f;    // Base speed, will be scaled
            this.WeaponMaterial = MaterialType.Wood;
            this.Durability = 75f;
            this.AttackRate = 0.8f;         // Relates to nocking time/cooldown after shot
        }

        public override void Start()
        {
            base.Start(); // From ScriptComponent, and potentially BaseWeapon/BaseBowWeapon if they have Start()

            if (ArrowPrefab == null)
            {
                Log.Warning($"{Entity?.Name ?? "WoodenBow"} has no ArrowPrefab assigned in the editor. It will not be able to fire arrows.");
            }
        }

        // The core firing logic (PrimaryAction, OnPrimaryActionReleased) is handled by BaseBowWeapon.
        // This class primarily defines the specific stats for a Wooden Bow.
        // Additional unique behaviors for the WoodenBow could be added here if needed.
    }
}

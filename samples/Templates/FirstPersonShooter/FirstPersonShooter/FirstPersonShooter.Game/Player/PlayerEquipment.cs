// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Weapons; // For BaseWeapon

namespace FirstPersonShooter.Player
{
    /// <summary>
    /// Manages the equipment of a player, specifically their current weapon.
    /// </summary>
    public class PlayerEquipment : ScriptComponent
    {
        /// <summary>
        /// Gets the currently equipped weapon.
        /// </summary>
        public BaseWeapon CurrentWeapon { get; private set; }

        /// <summary>
        /// Equips a new weapon. If a weapon is already equipped, it will be unequipped first.
        /// </summary>
        /// <param name="newWeapon">The new weapon to equip. Can be null to unequip.</param>
        public void EquipWeapon(BaseWeapon newWeapon)
        {
            // Unequip the current weapon if one exists
            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnUnequip(this.Entity);
                // Optional: Unparent CurrentWeapon.GetEntity() from the player's hand/attachment point.
                // Log.Info($"Unequipped {CurrentWeapon.GetEntity()?.Name}");
            }

            CurrentWeapon = newWeapon;

            // Equip the new weapon if it's not null
            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnEquip(this.Entity);
                // Optional: Parent CurrentWeapon.GetEntity() to a specific hand/attachment point on this.Entity.
                // For example, find a child entity named "WeaponSlot" and parent the weapon's entity to it.
                // var weaponSlot = this.Entity.FindChild("WeaponSlot");
                // if (weaponSlot != null && CurrentWeapon.GetEntity() != null)
                // {
                //     CurrentWeapon.GetEntity().Transform.Parent = weaponSlot.Transform;
                //     CurrentWeapon.GetEntity().Transform.Position = Vector3.Zero; // Reset local position
                //     CurrentWeapon.GetEntity().Transform.Rotation = Quaternion.Identity; // Reset local rotation
                // }
                // Log.Info($"Equipped {CurrentWeapon.GetEntity()?.Name}");
            }
        }

        // Example of how this might be used (e.g., driven by PlayerInput or an inventory system):
        // public override void Update()
        // {
        //     // Example: Press '1' to equip a hypothetical weapon (this would need a weapon instance)
        //     if (Input.IsKeyPressed(Keys.D1) && someWeaponInstance != null)
        //     {
        //         EquipWeapon(someWeaponInstance); 
        //     }
        //     // Example: Press 'Q' to unequip
        //     if (Input.IsKeyPressed(Keys.Q))
        //     {
        //         EquipWeapon(null);
        //     }
        //
        //     // Example: Use the equipped weapon
        //     if (CurrentWeapon != null && Input.IsMouseButtonDown(MouseButton.Left)) // Assuming ShootEventKey is true
        //     {
        //          // This would ideally be driven by an event from PlayerInput, similar to ShootEventKey
        //          // And the cooldown based on AttackRate would be handled within the weapon itself or here.
        //         CurrentWeapon.PrimaryAction();
        //     }
        // }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Events; // For EventReceiver
using FirstPersonShooter.Weapons; // For BaseWeapon
// Assuming PlayerInput is in FirstPersonShooter.Player namespace
// using FirstPersonShooter.Player; 

namespace FirstPersonShooter.Player
{
    /// <summary>
    /// Manages the equipment of a player, specifically their current weapon.
    /// Also handles relaying input actions to the equipped weapon.
    /// </summary>
    public class PlayerEquipment : ScriptComponent
    {
        /// <summary>
        /// Gets the currently equipped weapon.
        /// </summary>
        public BaseWeapon CurrentWeapon { get; private set; }

        private EventReceiver<bool> shootEventReceiver;
        private EventReceiver<bool> reloadEventReceiver;

        public override void Start()
        {
            base.Start(); // Good practice

            // Initialize event receivers. PlayerInput must exist for these static keys.
            shootEventReceiver = new EventReceiver<bool>(PlayerInput.ShootEventKey);
            reloadEventReceiver = new EventReceiver<bool>(PlayerInput.ReloadEventKey);
        }

        public override void Update()
        {
            // Check for shoot action
            if (shootEventReceiver.TryReceive(out bool shootPressed) && shootPressed)
            {
                TriggerCurrentWeaponPrimary();
            }

            // Check for reload action
            if (reloadEventReceiver.TryReceive(out bool reloadPressed) && reloadPressed)
            {
                if (CurrentWeapon != null && !CurrentWeapon.IsBroken) // Check if weapon exists and isn't broken
                {
                    CurrentWeapon.Reload();
                }
                else if (CurrentWeapon != null && CurrentWeapon.IsBroken)
                {
                     Log.Info($"PlayerEquipment: Cannot reload, {CurrentWeapon.GetEntity()?.Name ?? "Current weapon"} is broken.");
                }
            }
        }

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

        /// <summary>
        /// Triggers the primary action of the currently equipped weapon.
        /// </summary>
        public void TriggerCurrentWeaponPrimary()
        {
            if (CurrentWeapon == null)
            {
                // Log.Warning("PlayerEquipment: No weapon equipped to trigger primary action."); // Optional: for debugging if no weapon is normal
                return;
            }

            if (CurrentWeapon.IsBroken)
            {
                Log.Info($"PlayerEquipment: Cannot use primary action, {CurrentWeapon.GetEntity()?.Name ?? "Current weapon"} is broken.");
                // Potentially broadcast an event here too, e.g. "AttemptedToUseBrokenWeapon"
                return;
            }

            CurrentWeapon.PrimaryAction();
        }

        /// <summary>
        /// Triggers the secondary action of the currently equipped weapon.
        /// </summary>
        public void TriggerCurrentWeaponSecondary()
        {
            if (CurrentWeapon == null)
            {
                // Log.Warning("PlayerEquipment: No weapon equipped to trigger secondary action.");
                return;
            }

            if (CurrentWeapon.IsBroken)
            {
                Log.Info($"PlayerEquipment: Cannot use secondary action, {CurrentWeapon.GetEntity()?.Name ?? "Current weapon"} is broken.");
                return;
            }

            CurrentWeapon.SecondaryAction();
        }
    }
}

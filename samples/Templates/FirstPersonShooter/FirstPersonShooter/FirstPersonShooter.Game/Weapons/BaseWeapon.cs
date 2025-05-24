// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Events; // For EventKey
using FirstPersonShooter.Items; // For IEquippable

namespace FirstPersonShooter.Weapons
{
    public abstract class BaseWeapon : ScriptComponent, IEquippable
    {
        /// <summary>
        /// Event broadcast when a weapon breaks.
        /// </summary>
        public static readonly EventKey WeaponBrokeEventKey = new EventKey();

        /// <summary>
        /// The durability of the weapon.
        /// </summary>
        public float Durability { get; set; } = 100.0f;

        /// <summary>
        /// Indicates if the weapon is broken and unusable.
        /// </summary>
        public bool IsBroken { get; private set; } = false;

        /// <summary>
        /// The rate at which the weapon can perform its primary action, in attacks per second.
        /// </summary>
        public float AttackRate { get; set; } = 1.0f;

        /// <summary>
        /// The damage dealt by the weapon's primary action.
        /// </summary>
        public float Damage { get; set; } = 10.0f;

using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.Weapons
{
    public abstract class BaseWeapon : ScriptComponent, IEquippable
    {
        /// <summary>
        /// Event broadcast when a weapon breaks.
        /// </summary>
        public static readonly EventKey WeaponBrokeEventKey = new EventKey();

        /// <summary>
        /// The durability of the weapon.
        /// </summary>
        public float Durability { get; set; } = 100.0f;

        /// <summary>
        /// Indicates if the weapon is broken and unusable.
        /// </summary>
        public bool IsBroken { get; private set; } = false;

        /// <summary>
        /// The rate at which the weapon can perform its primary action, in attacks per second.
        /// </summary>
        public float AttackRate { get; set; } = 1.0f;

        /// <summary>
        /// The damage dealt by the weapon's primary action.
        /// </summary>
        public float Damage { get; set; } = 10.0f;

        /// <summary>
        /// The material type of this weapon, used for impact sound determination.
        /// </summary>
        public MaterialType WeaponMaterial { get; set; } = MaterialType.Metal;

        /// <summary>
        /// The entity that has equipped this weapon.
        /// </summary>
        public Entity OwnerEntity { get; protected set; }


        /// <summary>
        /// Reduces the weapon's durability by the specified amount.
        /// If durability reaches zero, the weapon breaks.
        /// </summary>
        /// <param name="amount">The amount of damage to inflict on the weapon's durability.</param>
        public virtual void ReceiveDamage(float amount)
        {
            if (IsBroken)
            {
                return;
            }

            Durability -= amount;

            if (Durability <= 0)
            {
                Durability = 0;
                IsBroken = true;
                WeaponBrokeEventKey.Broadcast(); // Consider passing 'this' or 'Entity' if needed by listeners
                Log.Info($"{Entity?.Name ?? "Weapon"} has broken!");
            }
        }

        /// <summary>
        /// Performs the primary action of the weapon (e.g., shoot, swing).
        /// </summary>
        public virtual void PrimaryAction() // Made virtual to allow override if base check is not desired
        {
            if (IsBroken)
            {
                Log.Info($"{Entity?.Name ?? "Weapon"} is broken and cannot perform primary action.");
                return;
            }
            // To be implemented by derived classes
        }

        /// <summary>
        /// Performs the secondary action of the weapon (e.g., aim, block).
        /// </summary>
        public virtual void SecondaryAction()
        {
            if (IsBroken)
            {
                Log.Info($"{Entity?.Name ?? "Weapon"} is broken and cannot perform secondary action.");
                return;
            }
            // Default implementation does nothing further.
        }

        /// <summary>
        /// Reloads the weapon, if applicable.
        /// </summary>
        public virtual void Reload()
        {
            // Default implementation does nothing.
        }

        #region IEquippable Implementation

        /// <summary>
        /// Called when the item is equipped by an owner entity.
        /// Stores the owner and potentially handles parenting or visual changes.
        /// </summary>
        /// <param name="owner">The entity that equipped this item.</param>
        public virtual void OnEquip(Entity owner)
        {
            OwnerEntity = owner;
            // Potential future logic:
            // - Parent this.Entity to a specific "hand" or "weapon slot" entity on the owner.
            // - Enable weapon model, disable other models, etc.
            // Log.Info($"Weapon {this.Entity.Name} equipped by {owner.Name}");
        }

        /// <summary>
        /// Called when the item is unequipped by the owner entity.
        /// Clears the owner and potentially handles unparenting or visual changes.
        /// </summary>
        /// <param name="owner">The entity that unequipped this item.</param>
        public virtual void OnUnequip(Entity owner)
        {
            // Potential future logic:
            // - Unparent this.Entity from the owner.
            // - Disable weapon model.
            // Log.Info($"Weapon {this.Entity.Name} unequipped by {owner.Name}");
            OwnerEntity = null;
        }

        /// <summary>
        /// Gets the entity associated with this equippable item.
        /// This is the entity this script component is attached to.
        /// </summary>
        /// <returns>The entity instance.</returns>
        public Entity GetEntity()
        {
            return this.Entity;
        }

        #endregion
    }
}

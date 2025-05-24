// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace FirstPersonShooter.Items
{
    /// <summary>
    /// Interface for items that can be equipped by an entity.
    /// </summary>
    public interface IEquippable
    {
        /// <summary>
        /// Called when the item is equipped by an owner entity.
        /// </summary>
        /// <param name="owner">The entity that equipped this item.</param>
        void OnEquip(Entity owner);

        /// <summary>
        /// Called when the item is unequipped by the owner entity.
        /// </summary>
        /// <param name="owner">The entity that unequipped this item.</param>
        void OnUnequip(Entity owner);

        /// <summary>
        /// Gets the entity associated with this equippable item.
        /// This is typically the entity the IEquippable script/component is attached to.
        /// </summary>
        /// <returns>The entity instance.</returns>
        Entity GetEntity();
    }
}

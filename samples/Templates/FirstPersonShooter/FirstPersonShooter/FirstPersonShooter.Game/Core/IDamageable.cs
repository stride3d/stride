// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace FirstPersonShooter.Core
{
    /// <summary>
    /// Interface for entities that can take damage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies damage to the entity.
        /// </summary>
        /// <param name="amount">The amount of damage to take.</param>
        /// <param name="source">The entity that is the source of the damage (can be null).</param>
        void TakeDamage(float amount, Entity source);
    }
}

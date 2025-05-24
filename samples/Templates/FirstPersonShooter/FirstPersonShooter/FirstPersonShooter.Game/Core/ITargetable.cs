// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;

namespace FirstPersonShooter.Core
{
    /// <summary>
    /// Interface for entities that can be targeted by other systems (e.g., turrets).
    /// </summary>
    public interface ITargetable
    {
        /// <summary>
        /// Gets the world position that should be aimed at.
        /// </summary>
        /// <returns>The target position in world space.</returns>
        Vector3 GetTargetPosition();

        /// <summary>
        /// Gets the entity associated with this targetable object.
        /// </summary>
        /// <returns>The entity instance.</returns>
        Entity GetEntity();
    }
}

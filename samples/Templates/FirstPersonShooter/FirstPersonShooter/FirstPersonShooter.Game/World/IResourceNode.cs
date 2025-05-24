// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace FirstPersonShooter.World
{
    /// <summary>
    /// Interface for entities that can be harvested for resources.
    /// </summary>
    public interface IResourceNode
    {
        /// <summary>
        /// Gets the type of resource this node provides (e.g., "Wood", "Stone").
        /// </summary>
        /// <returns>The resource type string.</returns>
        string GetResourceType();

        /// <summary>
        /// Attempts to harvest resources from this node.
        /// </summary>
        /// <param name="gatherAmount">The amount of resource to attempt to gather (e.g., damage dealt by the tool).</param>
        /// <param name="harvester">The entity performing the harvest.</param>
        /// <returns>True if resources were successfully harvested, false otherwise (e.g., depleted, wrong tool).</returns>
        bool Harvest(float gatherAmount, Entity harvester);

        /// <summary>
        /// Gets a value indicating whether this resource node is depleted.
        /// </summary>
        bool IsDepleted { get; }

        /// <summary>
        /// Gets the entity associated with this resource node.
        /// </summary>
        /// <returns>The entity instance.</returns>
        Entity GetEntity();

        /// <summary>
        /// Gets the material type of this resource node for impact effects.
        /// </summary>
        MaterialType HitMaterial { get; }
    }
}
